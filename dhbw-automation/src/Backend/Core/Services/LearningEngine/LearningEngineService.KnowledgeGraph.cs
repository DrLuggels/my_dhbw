using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.LearningEngine;

/// <summary>
/// Knowledge graph operations for the Learning Engine.
/// Handles graph queries, visualization data, and entity management.
/// </summary>
public partial class LearningEngineService
{
    /// <summary>
    /// Gets the knowledge graph for a document (entities + relationships).
    /// </summary>
    public async Task<KnowledgeGraphDto> GetDocumentKnowledgeGraphAsync(int documentId, int userId)
    {
        var result = new KnowledgeGraphDto();

        // Get all entities for this document
        var entities = await _context.KgEntities
            .Where(e => e.DocumentId == documentId && e.UserId == userId && e.IsActive)
            .OrderByDescending(e => e.ImportanceScore)
            .ToListAsync();

        var entityIds = entities.Select(e => e.Id).ToHashSet();

        // Get all relationships between these entities
        var relationships = await _context.KgRelationships
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .Where(r => r.IsActive &&
                (entityIds.Contains(r.SourceEntityId) || entityIds.Contains(r.TargetEntityId)))
            .ToListAsync();

        // Get user performance data for entities
        var performances = await _context.UserEntityPerformances
            .Where(p => p.UserId == userId && entityIds.Contains(p.EntityId))
            .ToDictionaryAsync(p => p.EntityId);

        // Map to DTOs
        result.Entities = entities.Select(e =>
        {
            performances.TryGetValue(e.Id, out var perf);
            return MapToDto(e, perf);
        }).ToList();

        result.Relationships = relationships.Select(r => MapToDto(r)).ToList();

        // Calculate stats
        result.Stats = new KnowledgeGraphStats
        {
            TotalEntities = entities.Count,
            TotalRelationships = relationships.Count,
            EntitiesByType = entities.GroupBy(e => e.EntityType)
                .ToDictionary(g => g.Key, g => g.Count()),
            RelationshipsByType = relationships.GroupBy(r => r.RelationshipType)
                .ToDictionary(g => g.Key, g => g.Count()),
            DocumentsCovered = 1,
            ChunksCovered = entities.Select(e => e.ChunkId).Distinct().Count()
        };

        return result;
    }

    /// <summary>
    /// Gets the full knowledge graph for a user (across all documents).
    /// </summary>
    public async Task<KnowledgeGraphDto> GetUserKnowledgeGraphAsync(int userId, KnowledgeGraphOptions? options = null)
    {
        options ??= new KnowledgeGraphOptions();
        var result = new KnowledgeGraphDto();

        // Build entity query
        var entityQuery = _context.KgEntities
            .Where(e => e.UserId == userId && e.IsActive);

        // Apply filters
        if (options.DocumentIds?.Any() == true)
        {
            entityQuery = entityQuery.Where(e => e.DocumentId.HasValue && options.DocumentIds.Contains(e.DocumentId.Value));
        }

        if (!string.IsNullOrEmpty(options.Subject))
        {
            entityQuery = entityQuery.Where(e => e.Subject == options.Subject);
        }

        if (!string.IsNullOrEmpty(options.Topic))
        {
            entityQuery = entityQuery.Where(e => e.Topic != null && e.Topic.Contains(options.Topic));
        }

        if (options.EntityTypes?.Any() == true)
        {
            entityQuery = entityQuery.Where(e => options.EntityTypes.Contains(e.EntityType));
        }

        if (options.MinImportance.HasValue)
        {
            entityQuery = entityQuery.Where(e => e.ImportanceScore >= options.MinImportance.Value);
        }

        // Order by importance and apply limit
        entityQuery = entityQuery.OrderByDescending(e => e.ImportanceScore);

        if (options.MaxEntities.HasValue)
        {
            entityQuery = entityQuery.Take(options.MaxEntities.Value);
        }

        var entities = await entityQuery.ToListAsync();
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        // Get relationships
        var relationships = await _context.KgRelationships
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .Where(r => r.IsActive && r.UserId == userId &&
                entityIds.Contains(r.SourceEntityId) && entityIds.Contains(r.TargetEntityId))
            .ToListAsync();

        // Get user performance data
        var performances = await _context.UserEntityPerformances
            .Where(p => p.UserId == userId && entityIds.Contains(p.EntityId))
            .ToDictionaryAsync(p => p.EntityId);

        // Map to DTOs
        result.Entities = entities.Select(e =>
        {
            performances.TryGetValue(e.Id, out var perf);
            var dto = MapToDto(e, perf);
            // Add document name if available
            if (e.DocumentId.HasValue)
            {
                var doc = _context.Documents.Find(e.DocumentId.Value);
                dto.DocumentName = doc?.FileName;
            }
            return dto;
        }).ToList();

        result.Relationships = relationships.Select(r => MapToDto(r)).ToList();

        // Calculate stats
        result.Stats = new KnowledgeGraphStats
        {
            TotalEntities = entities.Count,
            TotalRelationships = relationships.Count,
            EntitiesByType = entities.GroupBy(e => e.EntityType)
                .ToDictionary(g => g.Key, g => g.Count()),
            RelationshipsByType = relationships.GroupBy(r => r.RelationshipType)
                .ToDictionary(g => g.Key, g => g.Count()),
            DocumentsCovered = entities.Where(e => e.DocumentId.HasValue)
                .Select(e => e.DocumentId!.Value).Distinct().Count(),
            ChunksCovered = entities.Where(e => e.ChunkId.HasValue)
                .Select(e => e.ChunkId!.Value).Distinct().Count()
        };

        return result;
    }

    /// <summary>
    /// Gets entities related to a specific entity.
    /// </summary>
    public async Task<List<KgEntityDto>> GetRelatedEntitiesAsync(int entityId, int userId, int depth = 1)
    {
        var visited = new HashSet<int> { entityId };
        var result = new List<KgEntity>();

        await CollectRelatedEntitiesAsync(entityId, userId, depth, visited, result);

        // Get performance data
        var entityIds = result.Select(e => e.Id).ToList();
        var performances = await _context.UserEntityPerformances
            .Where(p => p.UserId == userId && entityIds.Contains(p.EntityId))
            .ToDictionaryAsync(p => p.EntityId);

        return result.Select(e =>
        {
            performances.TryGetValue(e.Id, out var perf);
            return MapToDto(e, perf);
        }).ToList();
    }

    private async Task CollectRelatedEntitiesAsync(
        int entityId,
        int userId,
        int depth,
        HashSet<int> visited,
        List<KgEntity> result)
    {
        if (depth <= 0) return;

        // Get directly related entities
        var relatedIds = await _context.KgRelationships
            .Where(r => r.IsActive && r.UserId == userId &&
                (r.SourceEntityId == entityId || r.TargetEntityId == entityId))
            .Select(r => r.SourceEntityId == entityId ? r.TargetEntityId : r.SourceEntityId)
            .ToListAsync();

        foreach (var relatedId in relatedIds)
        {
            if (visited.Contains(relatedId)) continue;
            visited.Add(relatedId);

            var entity = await _context.KgEntities
                .FirstOrDefaultAsync(e => e.Id == relatedId && e.IsActive);

            if (entity != null)
            {
                result.Add(entity);

                // Recurse for deeper connections
                if (depth > 1)
                {
                    await CollectRelatedEntitiesAsync(relatedId, userId, depth - 1, visited, result);
                }
            }
        }
    }

    /// <summary>
    /// Searches entities by name or description.
    /// </summary>
    public async Task<List<KgEntityDto>> SearchEntitiesAsync(
        int userId,
        string query,
        string? entityType = null,
        int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<KgEntityDto>();

        var normalizedQuery = NormalizeName(query);

        var entitiesQuery = _context.KgEntities
            .Where(e => e.UserId == userId && e.IsActive &&
                (e.NormalizedName!.Contains(normalizedQuery) ||
                 (e.Description != null && e.Description.ToLower().Contains(query.ToLower()))));

        if (!string.IsNullOrEmpty(entityType))
        {
            entitiesQuery = entitiesQuery.Where(e => e.EntityType == entityType);
        }

        var entities = await entitiesQuery
            .OrderByDescending(e => e.ImportanceScore)
            .Take(limit)
            .ToListAsync();

        // Get performance data
        var entityIds = entities.Select(e => e.Id).ToList();
        var performances = await _context.UserEntityPerformances
            .Where(p => p.UserId == userId && entityIds.Contains(p.EntityId))
            .ToDictionaryAsync(p => p.EntityId);

        return entities.Select(e =>
        {
            performances.TryGetValue(e.Id, out var perf);
            return MapToDto(e, perf);
        }).ToList();
    }

    /// <summary>
    /// Merges duplicate entities.
    /// </summary>
    public async Task<bool> MergeEntitiesAsync(
        int primaryEntityId,
        IEnumerable<int> duplicateEntityIds,
        int userId)
    {
        try
        {
            var primaryEntity = await _context.KgEntities
                .FirstOrDefaultAsync(e => e.Id == primaryEntityId && e.UserId == userId && e.IsActive);

            if (primaryEntity == null)
            {
                _logger.LogWarning("Primary entity {EntityId} not found", primaryEntityId);
                return false;
            }

            foreach (var duplicateId in duplicateEntityIds)
            {
                if (duplicateId == primaryEntityId) continue;

                var duplicate = await _context.KgEntities
                    .FirstOrDefaultAsync(e => e.Id == duplicateId && e.UserId == userId && e.IsActive);

                if (duplicate == null) continue;

                // Update occurrence count
                primaryEntity.OccurrenceCount += duplicate.OccurrenceCount;

                // Update description if duplicate has better one
                if (!string.IsNullOrEmpty(duplicate.Description) &&
                    (string.IsNullOrEmpty(primaryEntity.Description) ||
                     duplicate.Description.Length > primaryEntity.Description.Length))
                {
                    primaryEntity.Description = duplicate.Description;
                }

                // Redirect relationships
                var outgoingRels = await _context.KgRelationships
                    .Where(r => r.SourceEntityId == duplicateId && r.IsActive)
                    .ToListAsync();

                foreach (var rel in outgoingRels)
                {
                    rel.SourceEntityId = primaryEntityId;
                    rel.UpdatedAt = DateTime.UtcNow;
                }

                var incomingRels = await _context.KgRelationships
                    .Where(r => r.TargetEntityId == duplicateId && r.IsActive)
                    .ToListAsync();

                foreach (var rel in incomingRels)
                {
                    rel.TargetEntityId = primaryEntityId;
                    rel.UpdatedAt = DateTime.UtcNow;
                }

                // Soft delete duplicate
                duplicate.IsActive = false;
                duplicate.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Merged entity {DuplicateId} into {PrimaryId}",
                    duplicateId, primaryEntityId);
            }

            primaryEntity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Recalculate importance
            await UpdateImportanceScoresAsync(primaryEntity.DocumentId ?? 0);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging entities");
            return false;
        }
    }
}
