using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DHBWAutomation.Backend.Core.Services.Embedding;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for deadline-aware learning priority calculation.
/// Priority Score = 0.35×Urgency + 0.25×Relevance + 0.25×MasteryGap + 0.15×Decay
/// </summary>
public class DeadlinePriorityService : IDeadlinePriorityService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DeadlinePriorityService> _logger;

    // Priority weights (must sum to 1.0)
    private const double WeightUrgency = 0.35;
    private const double WeightRelevance = 0.25;
    private const double WeightMasteryGap = 0.25;
    private const double WeightDecay = 0.15;

    // Urgency calculation constants
    private const int MaxUrgencyDays = 30; // Days before deadline where urgency starts
    private const double MinSimilarityThreshold = 0.5; // Minimum semantic similarity for linking

    public DeadlinePriorityService(
        AppDbContext dbContext,
        IEmbeddingService embeddingService,
        ILogger<DeadlinePriorityService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<LearningPriority>> CalculatePrioritiesAsync(int userId)
    {
        var priorities = new List<LearningPriority>();

        // Get all user's knowledge nodes
        var nodes = await _dbContext.UserKnowledgeNodes
            .Where(n => n.UserId == userId)
            .ToListAsync();

        // Get upcoming assignments with deadlines
        var assignments = await _dbContext.MoodleAssignments
            .Where(a => a.UserId == userId && !a.IsSubmitted && a.DueDate.HasValue && a.DueDate > DateTime.UtcNow)
            .ToListAsync();

        // Get existing priorities for update
        var existingPriorities = await _dbContext.LearningPriorities
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.UserKnowledgeNodeId ?? 0);

        foreach (var node in nodes)
        {
            // Find related assignments via semantic matching or subject matching
            var relatedAssignment = await FindRelatedAssignmentAsync(node, assignments);
            var deadline = relatedAssignment?.DueDate;

            // Calculate component scores (0-100)
            var urgencyScore = CalculateUrgencyScore(deadline);
            var relevanceScore = await CalculateRelevanceScoreAsync(node, relatedAssignment);
            var masteryGapScore = (1 - node.MasteryLevel) * 100;
            var decayScore = (1 - node.EffectiveStrength) * 100;

            // Calculate composite score
            var compositeScore = WeightUrgency * urgencyScore +
                                 WeightRelevance * relevanceScore +
                                 WeightMasteryGap * masteryGapScore +
                                 WeightDecay * decayScore;

            // Update or create priority
            if (existingPriorities.TryGetValue(node.Id, out var existing))
            {
                existing.DeadlineUrgency = urgencyScore;
                existing.TopicRelevance = relevanceScore;
                existing.MasteryGap = masteryGapScore;
                existing.DecayAmount = decayScore;
                existing.CompositeScore = compositeScore;
                existing.Deadline = deadline;
                existing.MoodleAssignmentId = relatedAssignment?.Id;
                existing.CalculatedAt = DateTime.UtcNow;
                priorities.Add(existing);
            }
            else
            {
                var newPriority = new LearningPriority
                {
                    UserId = userId,
                    UserKnowledgeNodeId = node.Id,
                    MoodleAssignmentId = relatedAssignment?.Id,
                    DeadlineUrgency = urgencyScore,
                    TopicRelevance = relevanceScore,
                    MasteryGap = masteryGapScore,
                    DecayAmount = decayScore,
                    CompositeScore = compositeScore,
                    Deadline = deadline
                };
                _dbContext.LearningPriorities.Add(newPriority);
                priorities.Add(newPriority);
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Calculated {Count} priorities for user {UserId}", priorities.Count, userId);
        return priorities.OrderByDescending(p => p.CompositeScore).ToList();
    }

    /// <inheritdoc />
    public async Task<int> LinkTopicsToAssignmentsAsync(int userId)
    {
        var linkedCount = 0;

        var assignments = await _dbContext.MoodleAssignments
            .Where(a => a.UserId == userId && !a.IsSubmitted && a.DueDate.HasValue)
            .ToListAsync();

        var nodes = await _dbContext.UserKnowledgeNodes
            .Where(n => n.UserId == userId)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            var assignmentText = $"{assignment.CourseName}: {assignment.Title}";
            if (!string.IsNullOrEmpty(assignment.Description))
            {
                assignmentText += $" - {assignment.Description}";
            }

            // Generate embedding for assignment
            var assignmentEmbedding = await _embeddingService.GenerateEmbeddingAsync(assignmentText, userId);
            if (assignmentEmbedding == null)
            {
                _logger.LogWarning("Could not generate embedding for assignment {AssignmentId}", assignment.Id);
                continue;
            }

            // Find matching nodes
            foreach (var node in nodes)
            {
                var nodeText = $"{node.Subject}: {node.Topic}";
                if (!string.IsNullOrEmpty(node.Subtopic))
                {
                    nodeText += $" - {node.Subtopic}";
                }

                var nodeEmbedding = await _embeddingService.GenerateEmbeddingAsync(nodeText, userId);
                if (nodeEmbedding == null) continue;

                var similarity = CalculateCosineSimilarity(assignmentEmbedding, nodeEmbedding);

                if (similarity >= MinSimilarityThreshold)
                {
                    // Update or create priority link
                    var priority = await _dbContext.LearningPriorities
                        .FirstOrDefaultAsync(p => p.UserId == userId && p.UserKnowledgeNodeId == node.Id);

                    if (priority == null)
                    {
                        priority = new LearningPriority
                        {
                            UserId = userId,
                            UserKnowledgeNodeId = node.Id
                        };
                        _dbContext.LearningPriorities.Add(priority);
                    }

                    priority.MoodleAssignmentId = assignment.Id;
                    priority.TopicRelevance = similarity * 100;
                    priority.Deadline = assignment.DueDate;
                    priority.CalculatedAt = DateTime.UtcNow;
                    linkedCount++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Linked {Count} topic-assignment pairs for user {UserId}", linkedCount, userId);
        return linkedCount;
    }

    /// <inheritdoc />
    public async Task<List<LearningRecommendation>> GetRecommendedTopicsAsync(int userId, int topN = 5)
    {
        var recommendations = new List<LearningRecommendation>();

        // Get priorities with nodes
        var priorities = await _dbContext.LearningPriorities
            .Include(p => p.KnowledgeNode)
            .Include(p => p.MoodleAssignment)
            .Where(p => p.UserId == userId && p.KnowledgeNode != null)
            .OrderByDescending(p => p.CompositeScore)
            .Take(topN)
            .ToListAsync();

        foreach (var priority in priorities)
        {
            var node = priority.KnowledgeNode!;
            var recommendation = new LearningRecommendation
            {
                NodeId = node.Id,
                Subject = node.Subject,
                Topic = node.Topic,
                Subtopic = node.Subtopic,
                PriorityScore = priority.CompositeScore,
                MasteryLevel = node.MasteryLevel,
                EffectiveStrength = node.EffectiveStrength,
                NearestDeadline = priority.Deadline,
                RelatedAssignment = priority.MoodleAssignment?.Title,
                UrgencyScore = priority.DeadlineUrgency,
                RelevanceScore = priority.TopicRelevance,
                MasteryGapScore = priority.MasteryGap,
                DecayScore = priority.DecayAmount,
                RecommendationReason = GenerateRecommendationReason(priority, node)
            };
            recommendations.Add(recommendation);
        }

        // If not enough priorities, add nodes without priorities
        if (recommendations.Count < topN)
        {
            var existingNodeIds = recommendations.Select(r => r.NodeId).ToHashSet();
            var additionalNodes = await _dbContext.UserKnowledgeNodes
                .Where(n => n.UserId == userId && !existingNodeIds.Contains(n.Id))
                .OrderBy(n => n.MasteryLevel)
                .Take(topN - recommendations.Count)
                .ToListAsync();

            foreach (var node in additionalNodes)
            {
                recommendations.Add(new LearningRecommendation
                {
                    NodeId = node.Id,
                    Subject = node.Subject,
                    Topic = node.Topic,
                    Subtopic = node.Subtopic,
                    PriorityScore = (1 - node.MasteryLevel) * 50, // Basic priority based on mastery gap
                    MasteryLevel = node.MasteryLevel,
                    EffectiveStrength = node.EffectiveStrength,
                    MasteryGapScore = (1 - node.MasteryLevel) * 100,
                    DecayScore = (1 - node.EffectiveStrength) * 100,
                    RecommendationReason = $"Low mastery ({node.MasteryLevel:P0}) - needs practice"
                });
            }
        }

        return recommendations;
    }

    /// <inheritdoc />
    public double CalculateUrgencyScore(DateTime? deadline)
    {
        if (!deadline.HasValue)
            return 0.0;

        var daysUntilDeadline = (deadline.Value - DateTime.UtcNow).TotalDays;

        if (daysUntilDeadline <= 0)
            return 100.0; // Overdue - maximum urgency

        if (daysUntilDeadline >= MaxUrgencyDays)
            return 0.0; // Far away - no urgency

        // Linear decay from 100 (deadline) to 0 (30 days out)
        return Math.Max(0, 100 * (1 - daysUntilDeadline / MaxUrgencyDays));
    }

    /// <inheritdoc />
    public async Task<List<DeadlineWithTopics>> GetUpcomingDeadlinesAsync(int userId, int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(days);

        var assignments = await _dbContext.MoodleAssignments
            .Where(a => a.UserId == userId &&
                        a.DueDate.HasValue &&
                        a.DueDate <= cutoffDate &&
                        a.DueDate > DateTime.UtcNow)
            .OrderBy(a => a.DueDate)
            .ToListAsync();

        var result = new List<DeadlineWithTopics>();

        foreach (var assignment in assignments)
        {
            var linkedPriorities = await _dbContext.LearningPriorities
                .Include(p => p.KnowledgeNode)
                .Where(p => p.UserId == userId && p.MoodleAssignmentId == assignment.Id)
                .ToListAsync();

            var deadline = new DeadlineWithTopics
            {
                AssignmentId = assignment.Id,
                AssignmentTitle = assignment.Title,
                CourseName = assignment.CourseName,
                Deadline = assignment.DueDate!.Value,
                DaysUntilDeadline = (int)(assignment.DueDate.Value - DateTime.UtcNow).TotalDays,
                IsSubmitted = assignment.IsSubmitted,
                UrgencyScore = CalculateUrgencyScore(assignment.DueDate),
                LinkedTopics = linkedPriorities
                    .Where(p => p.KnowledgeNode != null)
                    .Select(p => new LinkedTopic
                    {
                        NodeId = p.KnowledgeNode!.Id,
                        Subject = p.KnowledgeNode.Subject,
                        Topic = p.KnowledgeNode.Topic,
                        SemanticSimilarity = p.TopicRelevance / 100.0,
                        MasteryLevel = p.KnowledgeNode.MasteryLevel,
                        NeedsAttention = p.KnowledgeNode.MasteryLevel < 0.6
                    })
                    .ToList()
            };
            result.Add(deadline);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task RefreshPrioritiesAsync(int userId)
    {
        // First, link topics to assignments
        await LinkTopicsToAssignmentsAsync(userId);

        // Then recalculate all priorities
        await CalculatePrioritiesAsync(userId);

        _logger.LogInformation("Refreshed all priorities for user {UserId}", userId);
    }

    /// <inheritdoc />
    public async Task<LearningPriority?> GetNodePriorityAsync(int userId, int nodeId)
    {
        return await _dbContext.LearningPriorities
            .Include(p => p.KnowledgeNode)
            .Include(p => p.MoodleAssignment)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.UserKnowledgeNodeId == nodeId);
    }

    #region Private Helper Methods

    /// <summary>
    /// Finds the most related assignment for a knowledge node.
    /// </summary>
    private async Task<MoodleAssignment?> FindRelatedAssignmentAsync(
        UserKnowledgeNode node,
        List<MoodleAssignment> assignments)
    {
        // First try exact subject match
        var subjectMatch = assignments
            .FirstOrDefault(a => a.CourseName?.Contains(node.Subject, StringComparison.OrdinalIgnoreCase) == true);

        if (subjectMatch != null)
            return subjectMatch;

        // Check if there's a linked priority
        var linkedPriority = await _dbContext.LearningPriorities
            .FirstOrDefaultAsync(p => p.UserKnowledgeNodeId == node.Id && p.MoodleAssignmentId.HasValue);

        if (linkedPriority?.MoodleAssignmentId != null)
        {
            return assignments.FirstOrDefault(a => a.Id == linkedPriority.MoodleAssignmentId);
        }

        // Return earliest deadline if no match found (conservative approach)
        return assignments.OrderBy(a => a.DueDate).FirstOrDefault();
    }

    /// <summary>
    /// Calculates relevance score between a node and assignment.
    /// </summary>
    private async Task<double> CalculateRelevanceScoreAsync(
        UserKnowledgeNode node,
        MoodleAssignment? assignment)
    {
        if (assignment == null)
            return 0.0;

        // Check for existing priority with relevance score
        var priority = await _dbContext.LearningPriorities
            .FirstOrDefaultAsync(p => p.UserKnowledgeNodeId == node.Id && p.MoodleAssignmentId == assignment.Id);

        if (priority != null && priority.TopicRelevance > 0)
            return priority.TopicRelevance;

        // Simple heuristic: check if topic appears in assignment
        var text = $"{assignment.Title} {assignment.Description}".ToLower();
        if (text.Contains(node.Topic.ToLower()))
            return 80.0;
        if (text.Contains(node.Subject.ToLower()))
            return 50.0;

        return 20.0; // Base relevance for same course
    }

    /// <summary>
    /// Calculates cosine similarity between two embeddings.
    /// </summary>
    private double CalculateCosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    /// <summary>
    /// Generates a human-readable recommendation reason.
    /// </summary>
    private string GenerateRecommendationReason(LearningPriority priority, UserKnowledgeNode node)
    {
        var reasons = new List<string>();

        if (priority.DeadlineUrgency >= 70)
        {
            var days = priority.Deadline.HasValue
                ? (int)(priority.Deadline.Value - DateTime.UtcNow).TotalDays
                : 0;
            reasons.Add($"Deadline in {days} days");
        }

        if (priority.MasteryGap >= 60)
        {
            reasons.Add($"Low mastery ({node.MasteryLevel:P0})");
        }

        if (priority.DecayAmount >= 50)
        {
            reasons.Add($"Knowledge fading ({node.EffectiveStrength:P0} strength)");
        }

        if (priority.TopicRelevance >= 70)
        {
            reasons.Add("Highly relevant to assignment");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Regular practice recommended");
        }

        return string.Join(" | ", reasons);
    }

    #endregion
}
