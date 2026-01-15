using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.OmniLearning;

public partial class OmniLearningEngineService
{
    #region Adaptive Scheduling

    /// <summary>
    /// Berechnet Lern-Prioritäten für einen User
    /// </summary>
    public async Task<List<UnifiedLearningPriority>> CalculatePrioritiesAsync(
        int userId, PriorityCalculationOptions? options = null)
    {
        options ??= new PriorityCalculationOptions();

        var entities = await _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive)
            .ToListAsync();

        // Hole relevante Deadlines
        var upcomingDeadlines = options.IncludeDeadlines
            ? await _context.MoodleAssignments
                .Where(a => a.UserId == userId && a.DueDate > DateTime.UtcNow)
                .OrderBy(a => a.DueDate)
                .Take(10)
                .ToListAsync()
            : new List<MoodleAssignment>();

        var priorities = new List<UnifiedLearningPriority>();

        foreach (var entity in entities)
        {
            // Filter nach Subject wenn angegeben
            if (!string.IsNullOrEmpty(options.FocusSubject) && entity.Subject != options.FocusSubject)
                continue;

            var priority = new UnifiedLearningPriority
            {
                UserId = userId,
                UnifiedEntityId = entity.Id,
                Subject = entity.Subject,
                Topic = entity.Topic,
                EntityName = entity.Name,
                CurrentBloomLevel = entity.CurrentBloomLevel,
                TargetBloomLevel = Math.Min(entity.CurrentBloomLevel + 1, 6),
                CalculatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // 1. Deadline Urgency (0-100)
            var relevantDeadline = upcomingDeadlines
                .FirstOrDefault(d => d.CourseName?.Contains(entity.Subject) == true ||
                                    d.Title?.Contains(entity.Topic) == true);

            if (relevantDeadline != null && relevantDeadline.DueDate.HasValue)
            {
                var daysUntil = (relevantDeadline.DueDate.Value - DateTime.UtcNow).TotalDays;
                priority.DeadlineUrgency = Math.Max(0, 100 * (1 - daysUntil / 30));
                priority.Deadline = relevantDeadline.DueDate;
                priority.MoodleAssignmentId = relevantDeadline.Id;
                priority.RelatedEventName = relevantDeadline.Title;
            }

            // 2. Topic Relevance (0-100) - Basierend auf kürzlichen Aktivitäten
            var recentlyStudied = await _context.UnifiedKnowledgeEntities
                .Where(e => e.UserId == userId && e.Subject == entity.Subject && e.IsActive)
                .OrderByDescending(e => e.LastInteraction)
                .Take(5)
                .AnyAsync(e => e.Id == entity.Id);

            priority.TopicRelevance = recentlyStudied ? 80 : 50;

            // 3. Mastery Gap (0-100)
            priority.MasteryGap = (1 - entity.MasteryScore) * 100;

            // 4. Decay Amount (0-100)
            if (options.IncludeDecay)
            {
                priority.DecayAmount = (1 - entity.DecayFactor) * 100;
            }

            // 5. Bloom Gap (0-100)
            if (options.IncludeBloomGap)
            {
                priority.BloomGap = Math.Max(0, (priority.TargetBloomLevel - entity.CurrentBloomLevel)) * 20;
            }

            // Berechne Composite Score mit Gewichten
            priority.CompositeScore = UnifiedPriorityWeights.CalculateCustom(
                priority.DeadlineUrgency,
                priority.TopicRelevance,
                priority.MasteryGap,
                priority.DecayAmount,
                priority.BloomGap,
                options.UrgencyWeight,
                options.RelevanceWeight,
                options.MasteryWeight,
                options.DecayWeight,
                options.BloomWeight);

            // Check Prerequisite Blocking
            var prereqCheck = await CheckPrerequisitesAsync(userId, entity.Id);
            if (!prereqCheck.AllMet)
            {
                var strictBlocking = prereqCheck.BlockingPrerequisites.Where(b => b.IsStrict).ToList();
                if (strictBlocking.Any())
                {
                    priority.IsBlocked = true;
                    priority.BlockReason = $"Voraussetzungen nicht erfüllt: {string.Join(", ", strictBlocking.Select(b => b.EntityName))}";
                    priority.BlockingPrerequisites = prereqCheck.BlockingPrerequisites;
                    priority.CompositeScore *= 0.5; // Reduziere Score aber eliminiere nicht
                }
            }

            priorities.Add(priority);
        }

        // Sortiere und vergebe Ränge
        priorities = priorities.OrderByDescending(p => p.CompositeScore).ToList();
        for (int i = 0; i < priorities.Count; i++)
        {
            priorities[i].Rank = i + 1;
        }

        // Speichere Prioritäten
        var existingPriorities = await _context.UnifiedLearningPriorities
            .Where(p => p.UserId == userId)
            .ToListAsync();

        _context.UnifiedLearningPriorities.RemoveRange(existingPriorities);
        _context.UnifiedLearningPriorities.AddRange(priorities);
        await _context.SaveChangesAsync();

        return priorities;
    }

    /// <summary>
    /// Holt die nächste empfohlene Übung
    /// </summary>
    public async Task<OmniExerciseDto?> GetNextExerciseAsync(int userId)
    {
        // Prüfe zuerst auf überfällige Übungen
        var dueExercises = await GetDueExercisesAsync(userId, 1);
        if (dueExercises.Any())
        {
            return dueExercises.First();
        }

        // Hole höchste Priorität
        var topPriority = await _context.UnifiedLearningPriorities
            .Where(p => p.UserId == userId && p.IsActive && !p.IsBlocked)
            .OrderByDescending(p => p.CompositeScore)
            .FirstOrDefaultAsync();

        if (topPriority?.UnifiedEntityId == null)
            return null;

        // Generiere Übung für diese Entität
        return await GenerateExerciseAsync(userId, new GenerateExerciseRequest
        {
            EntityId = topPriority.UnifiedEntityId,
            UseAdaptive = true
        });
    }

    /// <summary>
    /// Holt Schwachstellen des Users
    /// </summary>
    public async Task<List<WeakAreaDto>> GetWeakAreasAsync(int userId, int limit = 10)
    {
        var weakAreas = new List<WeakAreaDto>();

        // Niedrige Mastery
        var lowMastery = await _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive && e.MasteryScore < 0.4 && e.TotalAttempts > 0)
            .OrderBy(e => e.MasteryScore)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        foreach (var entity in lowMastery)
        {
            weakAreas.Add(new WeakAreaDto
            {
                EntityId = entity.Id,
                EntityName = entity.Name,
                Subject = entity.Subject,
                Topic = entity.Topic,
                MasteryScore = entity.MasteryScore,
                SuccessRate = entity.SuccessRate,
                TotalAttempts = entity.TotalAttempts,
                WeaknessType = "low_mastery",
                RecommendedAction = "Grundlagen wiederholen und mehr Übungen machen"
            });
        }

        // Hoher Decay
        var highDecay = await _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive && e.MasteryScore > 0.5)
            .ToListAsync();

        foreach (var entity in highDecay.Where(e => e.DecayFactor < 0.5).OrderBy(e => e.DecayFactor).Take(limit - weakAreas.Count))
        {
            if (!weakAreas.Any(w => w.EntityId == entity.Id))
            {
                weakAreas.Add(new WeakAreaDto
                {
                    EntityId = entity.Id,
                    EntityName = entity.Name,
                    Subject = entity.Subject,
                    Topic = entity.Topic,
                    MasteryScore = entity.MasteryScore,
                    SuccessRate = entity.SuccessRate,
                    TotalAttempts = entity.TotalAttempts,
                    WeaknessType = "high_decay",
                    RecommendedAction = "Auffrischung empfohlen - Wissen verblasst"
                });
            }
        }

        // Niedriger Bloom Level bei hoher Mastery
        var lowBloom = await _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive &&
                       e.MasteryScore > 0.6 && e.CurrentBloomLevel < 3)
            .OrderBy(e => e.CurrentBloomLevel)
            .Take(limit - weakAreas.Count)
            .AsNoTracking()
            .ToListAsync();

        foreach (var entity in lowBloom)
        {
            if (!weakAreas.Any(w => w.EntityId == entity.Id))
            {
                weakAreas.Add(new WeakAreaDto
                {
                    EntityId = entity.Id,
                    EntityName = entity.Name,
                    Subject = entity.Subject,
                    Topic = entity.Topic,
                    MasteryScore = entity.MasteryScore,
                    SuccessRate = entity.SuccessRate,
                    TotalAttempts = entity.TotalAttempts,
                    WeaknessType = "low_bloom",
                    RecommendedAction = $"Bloom-Level steigern: Aktuell {GetBloomLevelName(entity.CurrentBloomLevel)}"
                });
            }
        }

        return weakAreas.Take(limit).ToList();
    }

    /// <summary>
    /// Holt überfällige Wiederholungen
    /// </summary>
    public async Task<List<OverdueItemDto>> GetOverdueItemsAsync(int userId)
    {
        var now = DateTime.UtcNow;

        var overdueEntities = await _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive &&
                       e.NextReview != null && e.NextReview < now)
            .OrderBy(e => e.NextReview)
            .Take(20)
            .AsNoTracking()
            .ToListAsync();

        return overdueEntities.Select(e => new OverdueItemDto
        {
            EntityId = e.Id,
            EntityName = e.Name,
            Subject = e.Subject,
            Topic = e.Topic,
            DueDate = e.NextReview!.Value,
            DaysOverdue = (int)(now - e.NextReview.Value).TotalDays,
            CurrentMastery = e.MasteryScore,
            EstimatedMasteryLoss = (1 - e.DecayFactor) * e.MasteryScore
        }).ToList();
    }

    #endregion
}
