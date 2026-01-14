using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.UnifiedLearning;

/// <summary>
/// Knowledge Graph Operations - graph retrieval, relationships, and statistics
/// </summary>
public partial class UnifiedLearningService
{
    /// <inheritdoc />
    public async Task<UnifiedKnowledgeGraphDto> GetKnowledgeGraphAsync(
        int userId,
        UnifiedGraphOptions? options = null)
    {
        var query = _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive);

        // Apply filters
        if (options != null)
        {
            if (options.DocumentIds?.Any() == true)
                query = query.Where(e => e.SourceDocumentId.HasValue &&
                    options.DocumentIds.Contains(e.SourceDocumentId.Value));

            if (!string.IsNullOrEmpty(options.Subject))
                query = query.Where(e => e.Subject == options.Subject);

            if (!string.IsNullOrEmpty(options.Topic))
                query = query.Where(e => e.Topic.Contains(options.Topic));

            if (options.EntityTypes?.Any() == true)
                query = query.Where(e => options.EntityTypes.Contains(e.EntityType));

            if (options.MinMastery.HasValue)
                query = query.Where(e => e.MasteryScore >= options.MinMastery.Value);

            if (options.MinImportance.HasValue)
                query = query.Where(e => e.ImportanceScore >= options.MinImportance.Value);

            if (options.MaxEntities.HasValue)
                query = query.Take(options.MaxEntities.Value);
        }

        var entities = await query.ToListAsync();
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        // Get relationships
        var relationships = new List<UnifiedKnowledgeRelationship>();
        if (options?.IncludeRelationships != false)
        {
            relationships = await _context.Set<UnifiedKnowledgeRelationship>()
                .Include(r => r.SourceEntity)
                .Include(r => r.TargetEntity)
                .Where(r => r.UserId == userId && r.IsActive &&
                    entityIds.Contains(r.SourceEntityId) &&
                    entityIds.Contains(r.TargetEntityId))
                .ToListAsync();
        }

        // Calculate stats
        var stats = await GetGraphStatsAsync(userId);

        return new UnifiedKnowledgeGraphDto
        {
            Entities = entities.Select(MapToDto).ToList(),
            Relationships = relationships.Select(MapToDto).ToList(),
            Stats = stats
        };
    }

    /// <inheritdoc />
    public async Task<List<UnifiedEntityDto>> GetRelatedEntitiesAsync(
        int entityId,
        int userId,
        int depth = 1)
    {
        var relatedIds = new HashSet<int>();
        var currentIds = new HashSet<int> { entityId };

        for (int d = 0; d < depth; d++)
        {
            var relationships = await _context.Set<UnifiedKnowledgeRelationship>()
                .Where(r => r.UserId == userId && r.IsActive &&
                    (currentIds.Contains(r.SourceEntityId) || currentIds.Contains(r.TargetEntityId)))
                .ToListAsync();

            var newIds = new HashSet<int>();
            foreach (var rel in relationships)
            {
                if (currentIds.Contains(rel.SourceEntityId) && !relatedIds.Contains(rel.TargetEntityId))
                    newIds.Add(rel.TargetEntityId);
                if (currentIds.Contains(rel.TargetEntityId) && !relatedIds.Contains(rel.SourceEntityId))
                    newIds.Add(rel.SourceEntityId);
            }

            relatedIds.UnionWith(newIds);
            currentIds = newIds;

            if (!currentIds.Any()) break;
        }

        relatedIds.Remove(entityId);

        var entities = await _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => relatedIds.Contains(e.Id) && e.IsActive)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<UnifiedEntityDto>> SearchEntitiesAsync(
        int userId,
        string query,
        string? entityType = null,
        int limit = 20)
    {
        var normalizedQuery = NormalizeName(query);

        var dbQuery = _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive &&
                (e.NormalizedName!.Contains(normalizedQuery) ||
                 e.Name.Contains(query) ||
                 (e.Description != null && e.Description.Contains(query))));

        if (!string.IsNullOrEmpty(entityType))
            dbQuery = dbQuery.Where(e => e.EntityType == entityType);

        var entities = await dbQuery
            .OrderByDescending(e => e.ImportanceScore)
            .ThenByDescending(e => e.MasteryScore)
            .Take(limit)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<UnifiedKnowledgeRelationship> CreateOrUpdateRelationshipAsync(
        int userId,
        int sourceEntityId,
        int targetEntityId,
        string relationshipType,
        double? strength = null)
    {
        var existing = await _context.Set<UnifiedKnowledgeRelationship>()
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                r.SourceEntityId == sourceEntityId &&
                r.TargetEntityId == targetEntityId &&
                r.RelationshipType == relationshipType &&
                r.IsActive);

        if (existing != null)
        {
            existing.Reinforce(0.1);
            if (strength.HasValue)
                existing.InitialStrength = strength.Value;
            await _context.SaveChangesAsync();
            return existing;
        }

        var relationship = new UnifiedKnowledgeRelationship
        {
            UserId = userId,
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            RelationshipType = relationshipType,
            InitialStrength = strength ?? 1.0,
            CreatedAt = DateTime.UtcNow,
            LastReinforced = DateTime.UtcNow
        };

        _context.Set<UnifiedKnowledgeRelationship>().Add(relationship);
        await _context.SaveChangesAsync();

        _logger.LogDebug(
            "Created relationship: {Source} --[{Type}]--> {Target}",
            sourceEntityId, relationshipType, targetEntityId);

        return relationship;
    }

    /// <inheritdoc />
    public async Task<UnifiedGraphStatsDto> GetGraphStatsAsync(int userId)
    {
        var entities = await _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive)
            .ToListAsync();

        var relationships = await _context.Set<UnifiedKnowledgeRelationship>()
            .Where(r => r.UserId == userId && r.IsActive)
            .ToListAsync();

        var masteredCount = entities.Count(e => e.MasteryScore >= 0.8);
        var learningCount = entities.Count(e => e.MasteryScore >= 0.3 && e.MasteryScore < 0.8);
        var newCount = entities.Count(e => e.MasteryScore < 0.3);

        return new UnifiedGraphStatsDto
        {
            TotalEntities = entities.Count,
            TotalRelationships = relationships.Count,
            TotalPrerequisites = relationships.Count(r => r.IsPrerequisite),
            EntitiesByType = entities.GroupBy(e => e.EntityType)
                .ToDictionary(g => g.Key, g => g.Count()),
            RelationshipsByType = relationships.GroupBy(r => r.RelationshipType)
                .ToDictionary(g => g.Key, g => g.Count()),
            DocumentsCovered = entities
                .Where(e => e.SourceDocumentId.HasValue)
                .Select(e => e.SourceDocumentId!.Value)
                .Distinct()
                .Count(),
            ChunksCovered = entities
                .Where(e => e.SourceChunkId.HasValue)
                .Select(e => e.SourceChunkId!.Value)
                .Distinct()
                .Count(),
            AverageMastery = entities.Any() ? entities.Average(e => e.MasteryScore) : 0,
            MasteredEntities = masteredCount,
            LearningEntities = learningCount,
            NewEntities = newCount
        };
    }
}
