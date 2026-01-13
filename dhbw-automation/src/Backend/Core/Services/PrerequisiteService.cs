using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for managing prerequisite chains between knowledge topics.
/// Ensures users learn topics in the correct order.
/// </summary>
public class PrerequisiteService : IPrerequisiteService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<PrerequisiteService> _logger;

    public PrerequisiteService(
        AppDbContext dbContext,
        IEmbeddingService embeddingService,
        ILogger<PrerequisiteService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PrerequisiteCheckResult> CheckPrerequisitesAsync(int userId, int nodeId)
    {
        var result = new PrerequisiteCheckResult();

        // Get all prerequisites for this node
        var prerequisites = await _dbContext.PrerequisiteChains
            .Include(p => p.PrerequisiteNode)
            .Where(p => p.DependentNodeId == nodeId)
            .ToListAsync();

        result.TotalPrerequisites = prerequisites.Count;

        if (prerequisites.Count == 0)
        {
            result.CanProceed = true;
            return result;
        }

        // Get user's node data
        var userNodes = await _dbContext.UserKnowledgeNodes
            .Where(n => n.UserId == userId)
            .ToDictionaryAsync(n => n.Id);

        foreach (var prereq in prerequisites)
        {
            // Check if user has the prerequisite node
            if (!userNodes.TryGetValue(prereq.PrerequisiteNodeId, out var prereqNode))
            {
                // Node doesn't exist for user
                result.MissingPrerequisites.Add(new MissingPrerequisite
                {
                    NodeId = prereq.PrerequisiteNodeId,
                    Subject = prereq.PrerequisiteNode.Subject,
                    Topic = prereq.PrerequisiteNode.Topic,
                    CurrentMastery = 0,
                    RequiredMastery = prereq.RequiredMasteryLevel,
                    IsStrict = prereq.IsStrict
                });
                continue;
            }

            // Check mastery level
            if (prereqNode.MasteryLevel < prereq.RequiredMasteryLevel)
            {
                result.MissingPrerequisites.Add(new MissingPrerequisite
                {
                    NodeId = prereq.PrerequisiteNodeId,
                    Subject = prereqNode.Subject,
                    Topic = prereqNode.Topic,
                    CurrentMastery = prereqNode.MasteryLevel,
                    RequiredMastery = prereq.RequiredMasteryLevel,
                    IsStrict = prereq.IsStrict
                });
            }
            else
            {
                result.MetPrerequisites++;
            }
        }

        // Check if strict prerequisites are missing
        var strictMissing = result.MissingPrerequisites.Where(m => m.IsStrict).ToList();
        result.CanProceed = strictMissing.Count == 0;

        if (!result.CanProceed)
        {
            var topMissing = strictMissing.OrderByDescending(m => m.Gap).First();
            result.BlockReason = $"Voraussetzung nicht erfüllt: {topMissing.Subject} - {topMissing.Topic} " +
                                 $"(aktuell: {topMissing.CurrentMastery:P0}, benötigt: {topMissing.RequiredMastery:P0})";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<MissingPrerequisite>> GetMissingPrerequisitesAsync(int userId, int nodeId)
    {
        var result = await CheckPrerequisitesAsync(userId, nodeId);
        return result.MissingPrerequisites;
    }

    /// <inheritdoc />
    public async Task<PrerequisiteChain> CreatePrerequisiteAsync(
        int prerequisiteNodeId,
        int dependentNodeId,
        double requiredMasteryLevel = 0.6,
        bool isStrict = true,
        string? description = null)
    {
        // Check if chain already exists
        var existing = await _dbContext.PrerequisiteChains
            .FirstOrDefaultAsync(p => p.PrerequisiteNodeId == prerequisiteNodeId &&
                                      p.DependentNodeId == dependentNodeId);

        if (existing != null)
        {
            // Update existing
            existing.RequiredMasteryLevel = requiredMasteryLevel;
            existing.IsStrict = isStrict;
            existing.Description = description;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        // Check for circular dependencies
        if (await WouldCreateCycleAsync(prerequisiteNodeId, dependentNodeId))
        {
            throw new InvalidOperationException("Diese Voraussetzung würde einen Zyklus erstellen");
        }

        var chain = new PrerequisiteChain
        {
            PrerequisiteNodeId = prerequisiteNodeId,
            DependentNodeId = dependentNodeId,
            RequiredMasteryLevel = requiredMasteryLevel,
            IsStrict = isStrict,
            Description = description
        };

        _dbContext.PrerequisiteChains.Add(chain);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created prerequisite chain: {PrereqId} -> {DepId}", prerequisiteNodeId, dependentNodeId);
        return chain;
    }

    /// <inheritdoc />
    public async Task<bool> RemovePrerequisiteAsync(int chainId)
    {
        var chain = await _dbContext.PrerequisiteChains.FindAsync(chainId);
        if (chain == null)
            return false;

        _dbContext.PrerequisiteChains.Remove(chain);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<List<PrerequisiteChain>> GetPrerequisitesForNodeAsync(int nodeId)
    {
        return await _dbContext.PrerequisiteChains
            .Include(p => p.PrerequisiteNode)
            .Where(p => p.DependentNodeId == nodeId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<PrerequisiteChain>> GetDependentsForNodeAsync(int nodeId)
    {
        return await _dbContext.PrerequisiteChains
            .Include(p => p.DependentNode)
            .Where(p => p.PrerequisiteNodeId == nodeId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GeneratePrerequisiteChainsAsync(int userId)
    {
        var createdCount = 0;

        var nodes = await _dbContext.UserKnowledgeNodes
            .Where(n => n.UserId == userId)
            .ToListAsync();

        // Group by subject
        var subjectGroups = nodes.GroupBy(n => n.Subject);

        foreach (var subjectGroup in subjectGroups)
        {
            var subjectNodes = subjectGroup.ToList();

            // Simple heuristic: order by topic name complexity (longer names might be more advanced)
            // In a real system, you'd use semantic analysis or curriculum structure
            var orderedNodes = subjectNodes
                .OrderBy(n => n.Topic.Length)
                .ThenBy(n => string.IsNullOrEmpty(n.Subtopic) ? 0 : 1)
                .ToList();

            for (int i = 1; i < orderedNodes.Count; i++)
            {
                var prereqNode = orderedNodes[i - 1];
                var dependentNode = orderedNodes[i];

                // Check if chain already exists
                var exists = await _dbContext.PrerequisiteChains
                    .AnyAsync(p => p.PrerequisiteNodeId == prereqNode.Id &&
                                   p.DependentNodeId == dependentNode.Id);

                if (!exists)
                {
                    var chain = new PrerequisiteChain
                    {
                        PrerequisiteNodeId = prereqNode.Id,
                        DependentNodeId = dependentNode.Id,
                        RequiredMasteryLevel = 0.5, // Default 50% for auto-generated
                        IsStrict = false, // Non-strict for auto-generated
                        Description = $"Auto-generiert: {prereqNode.Topic} -> {dependentNode.Topic}"
                    };
                    _dbContext.PrerequisiteChains.Add(chain);
                    createdCount++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Generated {Count} prerequisite chains for user {UserId}", createdCount, userId);
        return createdCount;
    }

    /// <inheritdoc />
    public async Task<UserKnowledgeNode?> GetNextUnlockedTopicAsync(int userId, string? subject = null)
    {
        var unlockedTopics = await GetUnlockedTopicsAsync(userId, subject);

        // Return the topic with lowest mastery among unlocked
        return unlockedTopics
            .OrderBy(n => n.MasteryLevel)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<List<UserKnowledgeNode>> GetUnlockedTopicsAsync(int userId, string? subject = null)
    {
        var query = _dbContext.UserKnowledgeNodes.Where(n => n.UserId == userId);

        if (!string.IsNullOrEmpty(subject))
        {
            query = query.Where(n => n.Subject == subject);
        }

        var nodes = await query.ToListAsync();
        var unlockedNodes = new List<UserKnowledgeNode>();

        foreach (var node in nodes)
        {
            var check = await CheckPrerequisitesAsync(userId, node.Id);
            if (check.CanProceed)
            {
                unlockedNodes.Add(node);
            }
        }

        return unlockedNodes;
    }

    #region Private Helper Methods

    /// <summary>
    /// Checks if creating a chain would create a cycle.
    /// </summary>
    private async Task<bool> WouldCreateCycleAsync(int prereqNodeId, int dependentNodeId)
    {
        if (prereqNodeId == dependentNodeId)
            return true;

        // Check if dependent is already a prerequisite of prereq (direct or transitive)
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(dependentNodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current))
                continue;

            visited.Add(current);

            var dependents = await _dbContext.PrerequisiteChains
                .Where(p => p.PrerequisiteNodeId == current)
                .Select(p => p.DependentNodeId)
                .ToListAsync();

            foreach (var dep in dependents)
            {
                if (dep == prereqNodeId)
                    return true;

                queue.Enqueue(dep);
            }
        }

        return false;
    }

    #endregion
}
