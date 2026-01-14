using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.UnifiedLearning;

/// <summary>
/// Entity Management - CRUD operations for UnifiedKnowledgeEntity
/// </summary>
public partial class UnifiedLearningService
{
    /// <inheritdoc />
    public async Task<UnifiedKnowledgeEntity> GetOrCreateEntityAsync(
        int userId,
        string subject,
        string topic,
        string? entityType = null,
        string? name = null)
    {
        var normalizedName = NormalizeName(name ?? topic);

        // Try to find existing entity
        var existingEntity = await _context.Set<UnifiedKnowledgeEntity>()
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.Subject == subject &&
                e.NormalizedName == normalizedName &&
                e.IsActive);

        if (existingEntity != null)
        {
            existingEntity.OccurrenceCount++;
            existingEntity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existingEntity;
        }

        // Create new entity
        var newEntity = new UnifiedKnowledgeEntity
        {
            UserId = userId,
            Subject = subject,
            Topic = topic,
            EntityType = entityType ?? UnifiedEntityTypes.Concept,
            Name = name ?? topic,
            NormalizedName = normalizedName,
            CreatedAt = DateTime.UtcNow,
            LastInteraction = DateTime.UtcNow
        };

        _context.Set<UnifiedKnowledgeEntity>().Add(newEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created unified entity: {Name} ({Type}) for user {UserId}",
            newEntity.Name, newEntity.EntityType, userId);

        return newEntity;
    }

    /// <inheritdoc />
    public async Task<UnifiedKnowledgeEntity?> GetEntityAsync(int entityId)
    {
        return await _context.Set<UnifiedKnowledgeEntity>()
            .Include(e => e.OutgoingRelationships)
            .Include(e => e.IncomingRelationships)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.IsActive);
    }

    /// <inheritdoc />
    public async Task<List<UnifiedKnowledgeEntity>> GetUserEntitiesAsync(
        int userId,
        UnifiedEntityFilter? filter = null)
    {
        var query = _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId);

        // Apply filters
        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Subject))
                query = query.Where(e => e.Subject == filter.Subject);

            if (!string.IsNullOrEmpty(filter.Topic))
                query = query.Where(e => e.Topic.Contains(filter.Topic));

            if (!string.IsNullOrEmpty(filter.EntityType))
                query = query.Where(e => e.EntityType == filter.EntityType);

            if (filter.MinMastery.HasValue)
                query = query.Where(e => e.MasteryScore >= filter.MinMastery.Value);

            if (filter.MaxMastery.HasValue)
                query = query.Where(e => e.MasteryScore <= filter.MaxMastery.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(e => e.IsActive == filter.IsActive.Value);

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "mastery" => filter.SortDescending
                    ? query.OrderByDescending(e => e.MasteryScore)
                    : query.OrderBy(e => e.MasteryScore),
                "lastinteraction" => filter.SortDescending
                    ? query.OrderByDescending(e => e.LastInteraction)
                    : query.OrderBy(e => e.LastInteraction),
                "name" => filter.SortDescending
                    ? query.OrderByDescending(e => e.Name)
                    : query.OrderBy(e => e.Name),
                _ => query.OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt)
            };

            if (filter.Limit.HasValue)
                query = query.Take(filter.Limit.Value);
        }
        else
        {
            query = query.Where(e => e.IsActive)
                         .OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt);
        }

        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UnifiedKnowledgeEntity> UpdateEntityAfterInteractionAsync(
        int entityId,
        bool isCorrect,
        string difficulty,
        int bloomLevel,
        double responseTimeSeconds)
    {
        var entity = await GetEntityAsync(entityId)
            ?? throw new ArgumentException($"Entity {entityId} not found");

        // Record the attempt with all tracking
        entity.RecordAttempt(isCorrect, difficulty, responseTimeSeconds, bloomLevel);

        // Update FSRS parameters
        UpdateFsrsParameters(entity, isCorrect);

        // Calculate next review
        entity.NextReview = CalculateNextReview(entity);

        // Save changes
        await _context.SaveChangesAsync();

        _logger.LogDebug(
            "Updated entity {EntityId} after interaction: correct={IsCorrect}, mastery={Mastery:F2}, nextReview={NextReview}",
            entityId, isCorrect, entity.MasteryScore, entity.NextReview);

        return entity;
    }

    /// <inheritdoc />
    public async Task<UnifiedKnowledgeEntity> MergeEntitiesAsync(
        int primaryEntityId,
        IEnumerable<int> duplicateEntityIds,
        int userId)
    {
        var primary = await GetEntityAsync(primaryEntityId)
            ?? throw new ArgumentException($"Primary entity {primaryEntityId} not found");

        foreach (var duplicateId in duplicateEntityIds)
        {
            var duplicate = await GetEntityAsync(duplicateId);
            if (duplicate == null || duplicate.UserId != userId)
                continue;

            // Merge statistics
            primary.TotalAttempts += duplicate.TotalAttempts;
            primary.TotalCorrect += duplicate.TotalCorrect;
            primary.EasyTotal += duplicate.EasyTotal;
            primary.EasyCorrect += duplicate.EasyCorrect;
            primary.MediumTotal += duplicate.MediumTotal;
            primary.MediumCorrect += duplicate.MediumCorrect;
            primary.HardTotal += duplicate.HardTotal;
            primary.HardCorrect += duplicate.HardCorrect;
            primary.OccurrenceCount += duplicate.OccurrenceCount;

            // Use better values
            if (duplicate.MasteryScore > primary.MasteryScore)
                primary.MasteryScore = duplicate.MasteryScore;
            if (duplicate.BestStreak > primary.BestStreak)
                primary.BestStreak = duplicate.BestStreak;
            if (duplicate.ImportanceScore > primary.ImportanceScore)
                primary.ImportanceScore = duplicate.ImportanceScore;

            // Merge Bloom performance
            var primaryBloom = primary.BloomPerformance;
            foreach (var kvp in duplicate.BloomPerformance)
            {
                if (!primaryBloom.ContainsKey(kvp.Key))
                    primaryBloom[kvp.Key] = new BloomLevelPerformance();
                primaryBloom[kvp.Key].Attempts += kvp.Value.Attempts;
                primaryBloom[kvp.Key].Correct += kvp.Value.Correct;
            }
            primary.BloomPerformance = primaryBloom;

            // Update relationships to point to primary
            var relationships = await _context.Set<UnifiedKnowledgeRelationship>()
                .Where(r => r.SourceEntityId == duplicateId || r.TargetEntityId == duplicateId)
                .ToListAsync();

            foreach (var rel in relationships)
            {
                if (rel.SourceEntityId == duplicateId)
                    rel.SourceEntityId = primaryEntityId;
                if (rel.TargetEntityId == duplicateId)
                    rel.TargetEntityId = primaryEntityId;
            }

            // Mark duplicate as inactive
            duplicate.IsActive = false;
            duplicate.UpdatedAt = DateTime.UtcNow;
        }

        primary.UpdateAfterInteraction();
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Merged entities into primary {PrimaryId}: {Count} duplicates merged",
            primaryEntityId, duplicateEntityIds.Count());

        return primary;
    }

    /// <summary>
    /// Update FSRS parameters based on answer correctness
    /// </summary>
    private void UpdateFsrsParameters(UnifiedKnowledgeEntity entity, bool isCorrect)
    {
        // Grade: 1=Again, 2=Hard, 3=Good, 4=Easy
        int grade = isCorrect ? 3 : 1;

        if (entity.FsrsState == FsrsStates.New)
        {
            // First review - initialize FSRS
            entity.Stability = isCorrect ? FsrsW2 : FsrsW0;
            entity.Difficulty = isCorrect ? FsrsW5 : FsrsW4;
            entity.FsrsState = isCorrect ? FsrsStates.Learning : FsrsStates.Learning;
            entity.Reps = 1;
        }
        else
        {
            // Calculate retrievability
            var elapsed = entity.ElapsedDays;
            var retrievability = Math.Pow(1 + elapsed / (9.0 * entity.Stability), -1);

            // Update difficulty
            var difficultyDelta = (grade - 3) * FsrsW7;
            entity.Difficulty = Math.Clamp(entity.Difficulty + difficultyDelta, 0.0, 1.0);

            if (isCorrect)
            {
                // Update stability for successful recall
                var stabilityFactor = 1 + Math.Exp(FsrsW8) *
                    (11 - entity.Difficulty) *
                    Math.Pow(entity.Stability, -FsrsW9) *
                    (Math.Exp((1 - retrievability) * FsrsW10) - 1);

                entity.Stability = Math.Min(entity.Stability * stabilityFactor, FsrsMaximumInterval);
                entity.Reps++;

                if (entity.FsrsState == FsrsStates.Learning || entity.FsrsState == FsrsStates.Relearning)
                {
                    entity.FsrsState = FsrsStates.Review;
                }
            }
            else
            {
                // Lapse - reduce stability
                entity.Stability = Math.Max(FsrsW0, entity.Stability * FsrsW11);
                entity.Lapses++;
                entity.FsrsState = FsrsStates.Relearning;
            }
        }

        entity.ElapsedDays = 0;
    }

    /// <inheritdoc />
    public DateTime CalculateNextReview(UnifiedKnowledgeEntity entity)
    {
        if (entity.FsrsState == FsrsStates.New)
            return DateTime.UtcNow;

        // FSRS interval calculation
        var targetRetention = FsrsRequestRetention;
        var interval = entity.Stability * 9.0 * (1 / Math.Pow(targetRetention, 1.0 / FsrsW16) - 1);

        // Apply decay adjustment - if entity is decaying fast, review sooner
        var decayFactor = entity.DecayFactor;
        if (decayFactor < 0.7)
        {
            interval *= 0.5 + 0.5 * decayFactor; // Reduce interval for fast-decaying entities
        }

        // Apply personal factor based on historical performance
        var personalFactor = entity.SuccessRate > 0.8 ? 1.2 : (entity.SuccessRate < 0.5 ? 0.7 : 1.0);
        interval *= personalFactor;

        // Clamp to reasonable bounds
        interval = Math.Clamp(interval, 0.5, FsrsMaximumInterval);

        entity.ScheduledDays = (int)Math.Ceiling(interval);

        return DateTime.UtcNow.AddDays(interval);
    }
}
