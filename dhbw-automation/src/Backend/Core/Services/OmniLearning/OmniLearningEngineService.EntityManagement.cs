using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.OmniLearning;

public partial class OmniLearningEngineService
{
    #region Entity Management

    /// <summary>
    /// Erstellt eine neue Wissens-Entität
    /// </summary>
    public async Task<UnifiedKnowledgeEntity> CreateEntityAsync(int userId, CreateEntityDto dto)
    {
        var normalizedName = NormalizeName(dto.Name);

        // Prüfe ob Entität bereits existiert
        var existing = await _context.UnifiedKnowledgeEntities
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.NormalizedName == normalizedName &&
                e.Subject == dto.Subject &&
                e.Topic == dto.Topic &&
                e.IsActive);

        if (existing != null)
        {
            // Erhöhe Occurrence Count und aktualisiere
            existing.OccurrenceCount++;
            existing.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(dto.Description) && string.IsNullOrEmpty(existing.Description))
                existing.Description = dto.Description;
            await _context.SaveChangesAsync();
            return existing;
        }

        // Erstelle neue Entität
        var entity = new UnifiedKnowledgeEntity
        {
            UserId = userId,
            EntityType = dto.EntityType,
            Name = dto.Name,
            NormalizedName = normalizedName,
            Description = dto.Description,
            Subject = dto.Subject,
            Topic = dto.Topic,
            Subtopic = dto.Subtopic,
            SourceDocumentId = dto.SourceDocumentId,
            SourceChunkId = dto.SourceChunkId,
            ConfidenceScore = 1.0,
            ImportanceScore = 0.5,
            OccurrenceCount = 1,
            CreatedAt = DateTime.UtcNow,
            LastInteraction = DateTime.UtcNow
        };

        _context.UnifiedKnowledgeEntities.Add(entity);
        await _context.SaveChangesAsync();

        // Generiere Embedding falls möglich
        try
        {
            var textForEmbedding = $"{entity.Name}: {entity.Description ?? entity.Topic}";
            var embedding = await _embeddingService.GenerateEmbeddingAsync(textForEmbedding, userId);

            if (embedding != null && embedding.Length > 0)
            {
                var pointId = await _qdrantService.UpsertEmbeddingAsync(
                    OmniEntitiesCollection,
                    embedding,
                    "omni_entity",
                    entity.Id,
                    userId,
                    new Dictionary<string, string>
                    {
                        ["name"] = entity.Name,
                        ["entity_type"] = entity.EntityType,
                        ["subject"] = entity.Subject,
                        ["topic"] = entity.Topic
                    });

                entity.HasEmbedding = true;
                entity.QdrantPointId = pointId;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Konnte Embedding für Entität {EntityId} nicht generieren", entity.Id);
        }

        return entity;
    }

    /// <summary>
    /// Holt eine Entität mit allen Details
    /// </summary>
    public async Task<UnifiedKnowledgeEntity?> GetEntityAsync(int entityId, int userId)
    {
        return await _context.UnifiedKnowledgeEntities
            .Include(e => e.OutgoingRelationships)
                .ThenInclude(r => r.TargetEntity)
            .Include(e => e.IncomingRelationships)
                .ThenInclude(r => r.SourceEntity)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId && e.UserId == userId && e.IsActive);
    }

    /// <summary>
    /// Sucht Entitäten (semantisch oder textbasiert)
    /// </summary>
    public async Task<List<UnifiedEntityDto>> SearchEntitiesAsync(int userId, string query, EntitySearchFilters? filters = null)
    {
        filters ??= new EntitySearchFilters();

        List<UnifiedKnowledgeEntity> entities;

        if (filters.SemanticSearch)
        {
            // Semantische Suche über Qdrant
            try
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, userId);
                if (queryEmbedding != null && queryEmbedding.Length > 0)
                {
                    var searchResults = await _qdrantService.SearchSimilarAsync(
                        OmniEntitiesCollection,
                        queryEmbedding,
                        filters.Limit,
                        0.5,
                        userId);

                    var entityIds = searchResults
                        .Select(r => r.EntityId)
                        .ToList();

                    entities = await _context.UnifiedKnowledgeEntities
                        .Where(e => entityIds.Contains(e.Id) && e.IsActive)
                        .AsNoTracking()
                        .ToListAsync();

                    // Sortiere nach Qdrant-Reihenfolge
                    entities = entities.OrderBy(e => entityIds.IndexOf(e.Id)).ToList();
                }
                else
                {
                    entities = new List<UnifiedKnowledgeEntity>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Semantische Suche fehlgeschlagen, verwende Textsuche");
                entities = await TextSearchEntitiesAsync(userId, query, filters);
            }
        }
        else
        {
            entities = await TextSearchEntitiesAsync(userId, query, filters);
        }

        // Wende Filter an
        if (!string.IsNullOrEmpty(filters.EntityType))
            entities = entities.Where(e => e.EntityType == filters.EntityType).ToList();
        if (!string.IsNullOrEmpty(filters.Subject))
            entities = entities.Where(e => e.Subject == filters.Subject).ToList();
        if (!string.IsNullOrEmpty(filters.Topic))
            entities = entities.Where(e => e.Topic == filters.Topic).ToList();
        if (filters.MinMastery.HasValue)
            entities = entities.Where(e => e.MasteryScore >= filters.MinMastery.Value).ToList();
        if (filters.MaxMastery.HasValue)
            entities = entities.Where(e => e.MasteryScore <= filters.MaxMastery.Value).ToList();

        return entities.Take(filters.Limit).Select(MapToDto).ToList();
    }

    private async Task<List<UnifiedKnowledgeEntity>> TextSearchEntitiesAsync(
        int userId, string query, EntitySearchFilters filters)
    {
        var normalizedQuery = NormalizeName(query);

        return await _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive &&
                (e.NormalizedName!.Contains(normalizedQuery) ||
                 (e.Description != null && e.Description.ToLower().Contains(normalizedQuery)) ||
                 e.Subject.ToLower().Contains(normalizedQuery) ||
                 e.Topic.ToLower().Contains(normalizedQuery)))
            .OrderByDescending(e => e.ImportanceScore)
            .Take(filters.Limit)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Holt verwandte Entitäten (Graph-Traversierung)
    /// </summary>
    public async Task<List<UnifiedEntityDto>> GetRelatedEntitiesAsync(int entityId, int userId, int depth = 2)
    {
        var relatedIds = new HashSet<int> { entityId };
        var currentLevel = new List<int> { entityId };

        for (int level = 0; level < depth; level++)
        {
            var nextLevel = new List<int>();

            // Hole ausgehende Beziehungen
            var outgoing = await _context.UnifiedKnowledgeRelationships
                .Where(r => currentLevel.Contains(r.SourceEntityId) && r.UserId == userId && r.IsActive)
                .Select(r => r.TargetEntityId)
                .ToListAsync();

            // Hole eingehende Beziehungen
            var incoming = await _context.UnifiedKnowledgeRelationships
                .Where(r => currentLevel.Contains(r.TargetEntityId) && r.UserId == userId && r.IsActive)
                .Select(r => r.SourceEntityId)
                .ToListAsync();

            foreach (var id in outgoing.Concat(incoming))
            {
                if (!relatedIds.Contains(id))
                {
                    relatedIds.Add(id);
                    nextLevel.Add(id);
                }
            }

            currentLevel = nextLevel;
            if (!currentLevel.Any()) break;
        }

        relatedIds.Remove(entityId); // Ursprüngliche Entität entfernen

        var entities = await _context.UnifiedKnowledgeEntities
            .Where(e => relatedIds.Contains(e.Id) && e.IsActive)
            .OrderByDescending(e => e.ImportanceScore)
            .AsNoTracking()
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Merged doppelte Entitäten
    /// </summary>
    public async Task<UnifiedKnowledgeEntity> MergeEntitiesAsync(int[] entityIds, int userId)
    {
        if (entityIds.Length < 2)
            throw new ArgumentException("Mindestens 2 Entitäten zum Mergen erforderlich");

        var entities = await _context.UnifiedKnowledgeEntities
            .Where(e => entityIds.Contains(e.Id) && e.UserId == userId && e.IsActive)
            .ToListAsync();

        if (entities.Count < 2)
            throw new InvalidOperationException("Nicht genügend gültige Entitäten gefunden");

        // Wähle primäre Entität (höchste Importance oder älteste)
        var primary = entities.OrderByDescending(e => e.ImportanceScore)
            .ThenBy(e => e.CreatedAt)
            .First();

        var toMerge = entities.Where(e => e.Id != primary.Id).ToList();

        // Aggregiere Statistiken
        primary.OccurrenceCount += toMerge.Sum(e => e.OccurrenceCount);
        primary.TotalAttempts += toMerge.Sum(e => e.TotalAttempts);
        primary.TotalCorrect += toMerge.Sum(e => e.TotalCorrect);
        primary.EasyTotal += toMerge.Sum(e => e.EasyTotal);
        primary.EasyCorrect += toMerge.Sum(e => e.EasyCorrect);
        primary.MediumTotal += toMerge.Sum(e => e.MediumTotal);
        primary.MediumCorrect += toMerge.Sum(e => e.MediumCorrect);
        primary.HardTotal += toMerge.Sum(e => e.HardTotal);
        primary.HardCorrect += toMerge.Sum(e => e.HardCorrect);

        // Beste Mastery übernehmen
        primary.MasteryScore = Math.Max(primary.MasteryScore, toMerge.Max(e => e.MasteryScore));
        primary.CurrentBloomLevel = Math.Max(primary.CurrentBloomLevel, toMerge.Max(e => e.CurrentBloomLevel));
        primary.BestStreak = Math.Max(primary.BestStreak, toMerge.Max(e => e.BestStreak));

        // Beschreibung kombinieren falls nötig
        if (string.IsNullOrEmpty(primary.Description))
        {
            primary.Description = toMerge.FirstOrDefault(e => !string.IsNullOrEmpty(e.Description))?.Description;
        }

        // Aktualisiere Beziehungen - zeige auf primäre Entität
        var mergeIds = toMerge.Select(e => e.Id).ToList();

        var outgoingRelationships = await _context.UnifiedKnowledgeRelationships
            .Where(r => mergeIds.Contains(r.SourceEntityId))
            .ToListAsync();

        foreach (var rel in outgoingRelationships)
        {
            rel.SourceEntityId = primary.Id;
        }

        var incomingRelationships = await _context.UnifiedKnowledgeRelationships
            .Where(r => mergeIds.Contains(r.TargetEntityId))
            .ToListAsync();

        foreach (var rel in incomingRelationships)
        {
            rel.TargetEntityId = primary.Id;
        }

        // Deaktiviere gemergte Entitäten
        foreach (var entity in toMerge)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        primary.UpdatedAt = DateTime.UtcNow;
        primary.UpdateAfterInteraction();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Entitäten {MergedIds} in {PrimaryId} zusammengeführt",
            string.Join(",", mergeIds), primary.Id);

        return primary;
    }

    /// <summary>
    /// Holt alle Entitäten eines Users
    /// </summary>
    public async Task<List<UnifiedEntityDto>> GetUserEntitiesAsync(int userId, EntityListFilters? filters = null)
    {
        filters ??= new EntityListFilters();

        var query = _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive);

        // Filter anwenden
        if (!string.IsNullOrEmpty(filters.Subject))
            query = query.Where(e => e.Subject == filters.Subject);
        if (!string.IsNullOrEmpty(filters.Topic))
            query = query.Where(e => e.Topic == filters.Topic);
        if (!string.IsNullOrEmpty(filters.EntityType))
            query = query.Where(e => e.EntityType == filters.EntityType);
        if (filters.NeedsReview == true)
            query = query.Where(e => e.NextReview != null && e.NextReview <= DateTime.UtcNow);

        // Sortierung
        query = filters.SortBy switch
        {
            "mastery_asc" => query.OrderBy(e => e.MasteryScore),
            "mastery_desc" => query.OrderByDescending(e => e.MasteryScore),
            "name_asc" => query.OrderBy(e => e.Name),
            "name_desc" => query.OrderByDescending(e => e.Name),
            "importance" => query.OrderByDescending(e => e.ImportanceScore),
            "recent" => query.OrderByDescending(e => e.LastInteraction),
            "due" => query.OrderBy(e => e.NextReview ?? DateTime.MaxValue),
            _ => query.OrderByDescending(e => e.ImportanceScore)
        };

        var entities = await query
            .Skip(filters.Offset)
            .Take(filters.Limit)
            .AsNoTracking()
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    #endregion

    #region Relationship Management

    /// <summary>
    /// Erstellt eine neue Beziehung zwischen Entitäten
    /// </summary>
    public async Task<UnifiedKnowledgeRelationship> CreateRelationshipAsync(int userId, CreateRelationshipDto dto)
    {
        // Prüfe ob Beziehung bereits existiert
        var existing = await _context.UnifiedKnowledgeRelationships
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                r.SourceEntityId == dto.SourceEntityId &&
                r.TargetEntityId == dto.TargetEntityId &&
                r.RelationshipType == dto.RelationshipType &&
                r.IsActive);

        if (existing != null)
        {
            // Verstärke existierende Beziehung
            existing.Reinforce();
            await _context.SaveChangesAsync();
            return existing;
        }

        var relationship = new UnifiedKnowledgeRelationship
        {
            UserId = userId,
            SourceEntityId = dto.SourceEntityId,
            TargetEntityId = dto.TargetEntityId,
            RelationshipType = dto.RelationshipType,
            Evidence = dto.Evidence,
            Description = dto.Description,
            IsStrict = dto.IsStrict,
            RequiredMasteryLevel = dto.RequiredMasteryLevel,
            IsAutoExtracted = false,
            CreatedAt = DateTime.UtcNow,
            LastReinforced = DateTime.UtcNow
        };

        _context.UnifiedKnowledgeRelationships.Add(relationship);
        await _context.SaveChangesAsync();

        // Aktualisiere Importance-Scores der verbundenen Entitäten
        await UpdateEntityImportanceAsync(dto.SourceEntityId);
        await UpdateEntityImportanceAsync(dto.TargetEntityId);

        return relationship;
    }

    private async Task UpdateEntityImportanceAsync(int entityId)
    {
        var entity = await _context.UnifiedKnowledgeEntities.FindAsync(entityId);
        if (entity == null) return;

        var relationshipCount = await _context.UnifiedKnowledgeRelationships
            .CountAsync(r => (r.SourceEntityId == entityId || r.TargetEntityId == entityId) && r.IsActive);

        // Importance basierend auf Beziehungsanzahl und Occurrence
        entity.ImportanceScore = Math.Min(1.0,
            0.3 + (relationshipCount * 0.1) + (Math.Log10(entity.OccurrenceCount + 1) * 0.2));

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Generiert automatisch Beziehungen für eine Entität basierend auf semantischer Ähnlichkeit
    /// </summary>
    public async Task<List<UnifiedKnowledgeRelationship>> GenerateRelationshipsAsync(int entityId, int userId)
    {
        var entity = await _context.UnifiedKnowledgeEntities.FindAsync(entityId);
        if (entity == null || !entity.HasEmbedding)
            return new List<UnifiedKnowledgeRelationship>();

        var newRelationships = new List<UnifiedKnowledgeRelationship>();

        try
        {
            // Finde ähnliche Entitäten über Qdrant
            var queryText = $"{entity.Name}: {entity.Description ?? entity.Topic}";
            var embedding = await _embeddingService.GenerateEmbeddingAsync(queryText, userId);

            if (embedding == null) return newRelationships;

            var similarResults = await _qdrantService.SearchSimilarAsync(
                OmniEntitiesCollection,
                embedding,
                10,
                0.7,
                userId);

            foreach (var result in similarResults)
            {
                var targetId = result.EntityId;
                if (targetId == entityId) continue;

                // Prüfe ob Beziehung bereits existiert
                var exists = await _context.UnifiedKnowledgeRelationships
                    .AnyAsync(r => r.UserId == userId &&
                        ((r.SourceEntityId == entityId && r.TargetEntityId == targetId) ||
                         (r.SourceEntityId == targetId && r.TargetEntityId == entityId)) &&
                        r.IsActive);

                if (!exists)
                {
                    var relationship = new UnifiedKnowledgeRelationship
                    {
                        UserId = userId,
                        SourceEntityId = entityId,
                        TargetEntityId = targetId,
                        RelationshipType = UnifiedRelationshipTypes.SimilarTo,
                        InitialStrength = result.Score,
                        ConfidenceScore = result.Score,
                        IsAutoExtracted = true,
                        IsBidirectional = true,
                        CreatedAt = DateTime.UtcNow,
                        LastReinforced = DateTime.UtcNow
                    };

                    _context.UnifiedKnowledgeRelationships.Add(relationship);
                    newRelationships.Add(relationship);
                }
            }

            if (newRelationships.Any())
            {
                await _context.SaveChangesAsync();
                await UpdateEntityImportanceAsync(entityId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler beim Generieren von Beziehungen für Entität {EntityId}", entityId);
        }

        return newRelationships;
    }

    /// <summary>
    /// Prüft ob Prerequisites erfüllt sind
    /// </summary>
    public async Task<PrerequisiteCheckResult> CheckPrerequisitesAsync(int userId, int targetEntityId)
    {
        var result = new PrerequisiteCheckResult { AllMet = true };

        // Finde alle Prerequisite-Beziehungen
        var prerequisites = await _context.UnifiedKnowledgeRelationships
            .Include(r => r.SourceEntity)
            .Where(r => r.TargetEntityId == targetEntityId &&
                       r.UserId == userId &&
                       r.IsActive &&
                       (r.RelationshipType == UnifiedRelationshipTypes.Prerequisite ||
                        r.RelationshipType == UnifiedRelationshipTypes.Requires))
            .AsNoTracking()
            .ToListAsync();

        foreach (var prereq in prerequisites)
        {
            var sourceEntity = prereq.SourceEntity;
            if (sourceEntity == null) continue;

            var isMet = prereq.CheckPrerequisiteMet(sourceEntity.MasteryScore);

            if (isMet)
            {
                result.MetPrerequisiteIds.Add(sourceEntity.Id);
            }
            else
            {
                result.AllMet = false;
                result.BlockingPrerequisites.Add(new BlockingPrerequisiteInfo
                {
                    EntityId = sourceEntity.Id,
                    EntityName = sourceEntity.Name,
                    Subject = sourceEntity.Subject,
                    Topic = sourceEntity.Topic,
                    CurrentMastery = sourceEntity.MasteryScore,
                    RequiredMastery = prereq.RequiredMasteryLevel,
                    IsStrict = prereq.IsStrict
                });
            }
        }

        if (!result.AllMet)
        {
            var strictBlocking = result.BlockingPrerequisites.Where(b => b.IsStrict).ToList();
            if (strictBlocking.Any())
            {
                result.RecommendedAction = $"Bearbeite zuerst: {string.Join(", ", strictBlocking.Select(b => b.EntityName))}";
            }
            else
            {
                result.RecommendedAction = "Empfohlen: Voraussetzungen verbessern für besseres Verständnis";
            }
        }

        return result;
    }

    /// <summary>
    /// Holt die Prerequisite-Kette für eine Entität
    /// </summary>
    public async Task<List<PrerequisiteChainDto>> GetPrerequisiteChainAsync(int entityId, int userId)
    {
        var chain = new List<PrerequisiteChainDto>();
        var visited = new HashSet<int>();

        await BuildPrerequisiteChainRecursiveAsync(entityId, userId, chain, visited, 0);

        return chain;
    }

    private async Task BuildPrerequisiteChainRecursiveAsync(
        int entityId, int userId, List<PrerequisiteChainDto> chain, HashSet<int> visited, int depth)
    {
        if (visited.Contains(entityId) || depth > 5) return;
        visited.Add(entityId);

        var entity = await _context.UnifiedKnowledgeEntities
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId && e.UserId == userId && e.IsActive);

        if (entity == null) return;

        var prerequisites = await _context.UnifiedKnowledgeRelationships
            .Where(r => r.TargetEntityId == entityId &&
                       r.UserId == userId &&
                       r.IsActive &&
                       (r.RelationshipType == UnifiedRelationshipTypes.Prerequisite ||
                        r.RelationshipType == UnifiedRelationshipTypes.Requires))
            .AsNoTracking()
            .ToListAsync();

        var dto = new PrerequisiteChainDto
        {
            EntityId = entity.Id,
            EntityName = entity.Name,
            Depth = depth,
            CurrentMastery = entity.MasteryScore,
            RequiredMastery = 0.6, // Default
            IsMet = entity.MasteryScore >= 0.6
        };

        foreach (var prereq in prerequisites)
        {
            await BuildPrerequisiteChainRecursiveAsync(prereq.SourceEntityId, userId, dto.Prerequisites, visited, depth + 1);
        }

        chain.Add(dto);
    }

    #endregion
}
