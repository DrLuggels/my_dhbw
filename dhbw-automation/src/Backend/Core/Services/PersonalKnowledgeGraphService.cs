using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for managing the user's personal knowledge graph with time-decay connections.
/// </summary>
public class PersonalKnowledgeGraphService : IPersonalKnowledgeGraphService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PersonalKnowledgeGraphService> _logger;

    // Default decay rates
    private const double DefaultNodeDecayRate = 0.05;  // 5% per day
    private const double DefaultEdgeDecayRate = 0.03;  // 3% per day

    // Reinforcement/Weakening amounts
    private const double NodeReinforcementAmount = 0.1;
    private const double NodeWeakeningAmount = 0.05;
    private const double EdgeReinforcementAmount = 0.15;
    private const double EdgeWeakeningAmount = 0.1;

    // Mastery thresholds
    private const double WeakNodeThreshold = 0.4;
    private const double FadingStrengthThreshold = 0.5;

    public PersonalKnowledgeGraphService(
        AppDbContext context,
        ILogger<PersonalKnowledgeGraphService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Node Management

    public async Task<UserKnowledgeNode> GetOrCreateNodeAsync(int userId, string subject, string topic, string? subtopic = null)
    {
        var existingNode = await _context.UserKnowledgeNodes
            .FirstOrDefaultAsync(n =>
                n.UserId == userId &&
                n.Subject == subject &&
                n.Topic == topic &&
                n.Subtopic == subtopic);

        if (existingNode != null)
        {
            return existingNode;
        }

        // Get personalized decay rate if available
        var decayRate = await GetPersonalDecayRateAsync(userId, subject);

        var newNode = new UserKnowledgeNode
        {
            UserId = userId,
            Subject = subject,
            Topic = topic,
            Subtopic = subtopic,
            MasteryLevel = 0.0,
            DecayRate = decayRate,
            BaseStrength = 0.5, // Start at 50% strength
            LastInteraction = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserKnowledgeNodes.Add(newNode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new knowledge node: {Subject}/{Topic} for user {UserId}",
            subject, topic, userId);

        return newNode;
    }

    public async Task<UserKnowledgeNode?> GetNodeAsync(int nodeId)
    {
        return await _context.UserKnowledgeNodes.FindAsync(nodeId);
    }

    public async Task<List<UserKnowledgeNode>> GetUserNodesAsync(int userId, string? subject = null)
    {
        var query = _context.UserKnowledgeNodes
            .Where(n => n.UserId == userId);

        if (!string.IsNullOrEmpty(subject))
        {
            query = query.Where(n => n.Subject == subject);
        }

        return await query
            .OrderByDescending(n => n.MasteryLevel)
            .ThenBy(n => n.Subject)
            .ThenBy(n => n.Topic)
            .ToListAsync();
    }

    public async Task<UserKnowledgeNode> UpdateMasteryAsync(int nodeId, bool isCorrect, string difficulty, double? responseTimeSeconds = null)
    {
        var node = await _context.UserKnowledgeNodes.FindAsync(nodeId);
        if (node == null)
        {
            throw new ArgumentException($"Node {nodeId} not found");
        }

        // Update exercise statistics
        node.TotalExercises++;
        if (isCorrect) node.CorrectExercises++;

        // Update difficulty-specific statistics
        switch (difficulty.ToLower())
        {
            case "easy":
                node.EasyTotal++;
                if (isCorrect) node.EasyCorrect++;
                break;
            case "medium":
                node.MediumTotal++;
                if (isCorrect) node.MediumCorrect++;
                break;
            case "hard":
                node.HardTotal++;
                if (isCorrect) node.HardCorrect++;
                break;
        }

        // Update average response time
        if (responseTimeSeconds.HasValue)
        {
            var totalTime = node.AverageResponseTimeSeconds * (node.TotalExercises - 1);
            node.AverageResponseTimeSeconds = (totalTime + responseTimeSeconds.Value) / node.TotalExercises;
        }

        // Calculate mastery adjustment with difficulty multiplier
        var difficultyMultiplier = difficulty.ToLower() switch
        {
            "easy" => 0.7,
            "medium" => 1.0,
            "hard" => 1.3,
            _ => 1.0
        };

        // Apply mastery change (weighted average with recency bias)
        const double alpha = 0.3; // 30% weight on new result
        var exerciseScore = isCorrect ? 1.0 * difficultyMultiplier : 0.0;
        node.MasteryLevel = Math.Min(1.0, Math.Max(0.0,
            alpha * exerciseScore + (1 - alpha) * node.MasteryLevel));

        // Reinforce or weaken base strength
        if (isCorrect)
        {
            node.BaseStrength = Math.Min(1.0, node.BaseStrength + NodeReinforcementAmount);
        }
        else
        {
            node.BaseStrength = Math.Max(0.1, node.BaseStrength - NodeWeakeningAmount);
        }

        // Update interaction timestamp
        node.LastInteraction = DateTime.UtcNow;
        node.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated mastery for node {NodeId}: {Mastery:P0}, Strength: {Strength:P0}",
            nodeId, node.MasteryLevel, node.BaseStrength);

        return node;
    }

    public async Task<List<UserKnowledgeNode>> GetWeakNodesAsync(int userId, double threshold = WeakNodeThreshold)
    {
        return await _context.UserKnowledgeNodes
            .Where(n => n.UserId == userId && n.MasteryLevel < threshold)
            .OrderBy(n => n.MasteryLevel)
            .ToListAsync();
    }

    public async Task<List<UserKnowledgeNode>> GetFadingNodesAsync(int userId, double threshold = FadingStrengthThreshold)
    {
        var nodes = await _context.UserKnowledgeNodes
            .Where(n => n.UserId == userId)
            .ToListAsync();

        return nodes
            .Where(n => n.EffectiveStrength < threshold)
            .OrderBy(n => n.EffectiveStrength)
            .ToList();
    }

    #endregion

    #region Edge Management

    public async Task<UserKnowledgeEdge> CreateOrReinforceEdgeAsync(int userId, int sourceNodeId, int targetNodeId, string edgeType = "related")
    {
        var existingEdge = await _context.UserKnowledgeEdges
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                ((e.SourceNodeId == sourceNodeId && e.TargetNodeId == targetNodeId) ||
                 (e.IsBidirectional && e.SourceNodeId == targetNodeId && e.TargetNodeId == sourceNodeId)));

        if (existingEdge != null)
        {
            // Reinforce existing edge
            existingEdge.InitialStrength = Math.Min(1.0, existingEdge.InitialStrength + EdgeReinforcementAmount);
            existingEdge.LastReinforced = DateTime.UtcNow;
            existingEdge.ReinforcementCount++;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Reinforced edge {EdgeId}: Strength {Strength:P0}",
                existingEdge.Id, existingEdge.InitialStrength);

            return existingEdge;
        }

        // Create new edge
        var newEdge = new UserKnowledgeEdge
        {
            UserId = userId,
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            EdgeType = edgeType,
            InitialStrength = 0.5, // Start at 50%
            DecayRate = DefaultEdgeDecayRate,
            LastReinforced = DateTime.UtcNow,
            IsBidirectional = edgeType != EdgeTypes.Prerequisite, // Prerequisites are directional
            CreatedAt = DateTime.UtcNow
        };

        _context.UserKnowledgeEdges.Add(newEdge);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new edge {SourceId} -> {TargetId} ({Type})",
            sourceNodeId, targetNodeId, edgeType);

        return newEdge;
    }

    public async Task WeakenEdgeAsync(int edgeId, double amount = EdgeWeakeningAmount)
    {
        var edge = await _context.UserKnowledgeEdges.FindAsync(edgeId);
        if (edge == null) return;

        edge.InitialStrength = Math.Max(0.1, edge.InitialStrength - amount);
        edge.WeakeningCount++;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Weakened edge {EdgeId}: Strength {Strength:P0}",
            edgeId, edge.InitialStrength);
    }

    public async Task<List<UserKnowledgeEdge>> GetNodeEdgesAsync(int nodeId)
    {
        return await _context.UserKnowledgeEdges
            .Include(e => e.SourceNode)
            .Include(e => e.TargetNode)
            .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
            .ToListAsync();
    }

    public async Task<List<UserKnowledgeEdge>> GetFadingEdgesAsync(int userId, double threshold = 0.3)
    {
        var edges = await _context.UserKnowledgeEdges
            .Where(e => e.UserId == userId)
            .ToListAsync();

        return edges
            .Where(e => e.CurrentStrength < threshold)
            .OrderBy(e => e.CurrentStrength)
            .ToList();
    }

    #endregion

    #region Decay Operations

    public async Task ApplyTimeDecayAsync(int userId)
    {
        _logger.LogInformation("Applying time decay for user {UserId}", userId);

        // Note: EffectiveStrength and CurrentStrength are computed properties,
        // so we don't need to update them. The decay is automatically applied
        // when these properties are accessed.

        // However, we can identify and log fading nodes/edges
        var fadingNodes = await GetFadingNodesAsync(userId);
        var fadingEdges = await GetFadingEdgesAsync(userId);

        _logger.LogInformation("User {UserId} has {FadingNodes} fading nodes and {FadingEdges} fading edges",
            userId, fadingNodes.Count, fadingEdges.Count);

        // Could trigger notifications or learning recommendations here
    }

    public double CalculateEffectiveStrength(double baseStrength, double decayRate, DateTime lastInteraction)
    {
        var daysSinceInteraction = (DateTime.UtcNow - lastInteraction).TotalDays;
        return baseStrength * Math.Exp(-decayRate * daysSinceInteraction);
    }

    public async Task<double> GetPersonalDecayRateAsync(int userId, string subject)
    {
        var profile = await _context.UserDecayProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Subject == subject);

        if (profile == null || profile.DataPoints < 10)
        {
            // Not enough data for personalized rate
            return DefaultNodeDecayRate;
        }

        return profile.LearnedDecayRate;
    }

    #endregion

    #region Graph Operations

    public async Task<PersonalKnowledgeGraphDto> GetUserGraphAsync(int userId)
    {
        var nodes = await _context.UserKnowledgeNodes
            .Where(n => n.UserId == userId)
            .ToListAsync();

        var edges = await _context.UserKnowledgeEdges
            .Where(e => e.UserId == userId)
            .ToListAsync();

        var nodeIds = nodes.Select(n => n.Id).ToHashSet();

        // Filter edges to only include those connecting existing nodes
        edges = edges.Where(e => nodeIds.Contains(e.SourceNodeId) && nodeIds.Contains(e.TargetNodeId)).ToList();

        var graph = new PersonalKnowledgeGraphDto
        {
            UserId = userId,
            Nodes = nodes.Select(n => new KnowledgeNodeDto
            {
                Id = n.Id,
                Subject = n.Subject,
                Topic = n.Topic,
                Subtopic = n.Subtopic,
                MasteryLevel = n.MasteryLevel,
                EffectiveStrength = n.EffectiveStrength,
                LastInteraction = n.LastInteraction,
                TotalExercises = n.TotalExercises,
                CorrectExercises = n.CorrectExercises,
                IsWeak = n.MasteryLevel < WeakNodeThreshold,
                IsFading = n.EffectiveStrength < FadingStrengthThreshold
            }).ToList(),
            Edges = edges.Select(e => new KnowledgeEdgeDto
            {
                Id = e.Id,
                SourceNodeId = e.SourceNodeId,
                TargetNodeId = e.TargetNodeId,
                EdgeType = e.EdgeType,
                CurrentStrength = e.CurrentStrength,
                IsFading = e.CurrentStrength < 0.3
            }).ToList(),
            Statistics = new GraphStatistics
            {
                TotalNodes = nodes.Count,
                TotalEdges = edges.Count,
                AverageMastery = nodes.Count > 0 ? nodes.Average(n => n.MasteryLevel) : 0,
                AverageStrength = nodes.Count > 0 ? nodes.Average(n => n.EffectiveStrength) : 0,
                WeakNodes = nodes.Count(n => n.MasteryLevel < WeakNodeThreshold),
                FadingEdges = edges.Count(e => e.CurrentStrength < 0.3),
                SubjectsCount = nodes.Select(n => n.Subject).Distinct().Count()
            }
        };

        return graph;
    }

    public async Task<int> GenerateSemanticEdgesAsync(int userId, double similarityThreshold = 0.7)
    {
        // This would integrate with the EmbeddingService to find semantically similar nodes
        // For now, we create edges between nodes in the same subject
        var nodes = await _context.UserKnowledgeNodes
            .Where(n => n.UserId == userId)
            .ToListAsync();

        var edgesCreated = 0;

        // Group by subject and create edges within subjects
        var subjectGroups = nodes.GroupBy(n => n.Subject);

        foreach (var group in subjectGroups)
        {
            var subjectNodes = group.ToList();
            for (int i = 0; i < subjectNodes.Count; i++)
            {
                for (int j = i + 1; j < subjectNodes.Count; j++)
                {
                    // Check if edge already exists
                    var existingEdge = await _context.UserKnowledgeEdges
                        .FirstOrDefaultAsync(e =>
                            e.UserId == userId &&
                            ((e.SourceNodeId == subjectNodes[i].Id && e.TargetNodeId == subjectNodes[j].Id) ||
                             (e.SourceNodeId == subjectNodes[j].Id && e.TargetNodeId == subjectNodes[i].Id)));

                    if (existingEdge == null)
                    {
                        await CreateOrReinforceEdgeAsync(userId, subjectNodes[i].Id, subjectNodes[j].Id, EdgeTypes.Related);
                        edgesCreated++;
                    }
                }
            }
        }

        _logger.LogInformation("Generated {Count} semantic edges for user {UserId}", edgesCreated, userId);
        return edgesCreated;
    }

    public async Task<ExerciseImpact> RecordExerciseResultAsync(int userId, int nodeId, bool isCorrect, string difficulty, double? responseTimeSeconds = null)
    {
        var node = await GetNodeAsync(nodeId);
        if (node == null || node.UserId != userId)
        {
            throw new ArgumentException($"Node {nodeId} not found or doesn't belong to user {userId}");
        }

        // Store previous values
        var previousMastery = node.MasteryLevel;
        var previousStrength = node.EffectiveStrength;

        // Get streak multiplier
        var streak = await _context.LearningStreaks.FirstOrDefaultAsync(s => s.UserId == userId);
        var streakMultiplier = streak?.StreakMultiplier ?? 1.0;

        // Update the node with streak-boosted reinforcement
        if (isCorrect)
        {
            node.BaseStrength = Math.Min(1.0, node.BaseStrength + NodeReinforcementAmount * streakMultiplier);
        }

        // Update mastery (this also updates other statistics)
        await UpdateMasteryAsync(nodeId, isCorrect, difficulty, responseTimeSeconds);

        // Handle connected edges
        var edges = await GetNodeEdgesAsync(nodeId);
        var edgesReinforced = 0;
        var edgesWeakened = 0;

        foreach (var edge in edges)
        {
            if (isCorrect)
            {
                // Reinforce connected edges
                edge.InitialStrength = Math.Min(1.0, edge.InitialStrength + EdgeReinforcementAmount * 0.5 * streakMultiplier);
                edge.LastReinforced = DateTime.UtcNow;
                edgesReinforced++;
            }
            else
            {
                // Slightly weaken edges on incorrect answer
                edge.InitialStrength = Math.Max(0.1, edge.InitialStrength - EdgeWeakeningAmount * 0.25);
                edgesWeakened++;
            }
        }

        await _context.SaveChangesAsync();

        // Update streak
        await UpdateStreakAsync(userId);

        // Reload node to get updated values
        node = await GetNodeAsync(nodeId);

        var impact = new ExerciseImpact
        {
            NodeId = nodeId,
            PreviousMastery = previousMastery,
            NewMastery = node!.MasteryLevel,
            MasteryChange = node.MasteryLevel - previousMastery,
            PreviousStrength = previousStrength,
            NewStrength = node.EffectiveStrength,
            StrengthChange = node.EffectiveStrength - previousStrength,
            EdgesReinforced = edgesReinforced,
            EdgesWeakened = edgesWeakened,
            StreakMultiplier = streakMultiplier,
            Message = GenerateImpactMessage(isCorrect, node.MasteryLevel - previousMastery, streakMultiplier)
        };

        _logger.LogInformation("Recorded exercise result for node {NodeId}: {Impact}",
            nodeId, isCorrect ? "Correct" : "Incorrect");

        return impact;
    }

    private async Task UpdateStreakAsync(int userId)
    {
        var streak = await _context.LearningStreaks.FirstOrDefaultAsync(s => s.UserId == userId);

        if (streak == null)
        {
            streak = new LearningStreak
            {
                UserId = userId,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastActivityDate = DateTime.UtcNow,
                TotalExercisesCompleted = 1,
                TotalActiveDays = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.LearningStreaks.Add(streak);
        }
        else
        {
            var today = DateTime.UtcNow.Date;
            var lastDay = streak.LastActivityDate.Date;

            streak.TotalExercisesCompleted++;

            if (lastDay == today)
            {
                // Already active today
            }
            else if (lastDay == today.AddDays(-1))
            {
                // Streak continues!
                streak.CurrentStreak++;
                streak.TotalActiveDays++;
                if (streak.CurrentStreak > streak.LongestStreak)
                {
                    streak.LongestStreak = streak.CurrentStreak;
                }
            }
            else if (lastDay < today.AddDays(-1))
            {
                // Streak broken - check for freeze
                if (streak.StreakFreezes > 0 && lastDay >= today.AddDays(-2))
                {
                    streak.StreakFreezes--;
                    streak.LastFreezeUsed = DateTime.UtcNow;
                    streak.TotalActiveDays++;
                }
                else
                {
                    streak.CurrentStreak = 1;
                    streak.TotalActiveDays++;
                }
            }

            streak.LastActivityDate = DateTime.UtcNow;
            streak.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private string GenerateImpactMessage(bool isCorrect, double masteryChange, double streakMultiplier)
    {
        if (isCorrect)
        {
            var baseMessage = masteryChange > 0.05 ? "Großer Fortschritt!" :
                              masteryChange > 0.02 ? "Gut gemacht!" :
                              "Richtig!";

            if (streakMultiplier > 1.1)
            {
                return $"{baseMessage} (Streak-Bonus: {streakMultiplier:F2}x)";
            }
            return baseMessage;
        }
        else
        {
            return masteryChange < -0.05 ? "Das Thema braucht mehr Übung." :
                   "Nicht ganz richtig. Versuch es nochmal!";
        }
    }

    #endregion
}
