using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.UnifiedLearning;

/// <summary>
/// Spaced Repetition - FSRS + Decay combined scheduling and review queue
/// </summary>
public partial class UnifiedLearningService
{
    /// <inheritdoc />
    public async Task<List<UnifiedEntityDto>> GetDueForReviewAsync(int userId, int limit = 10)
    {
        var now = DateTime.UtcNow;

        // Get entities that are due for review based on FSRS scheduling
        var entities = await _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive &&
                (e.NextReview == null || e.NextReview <= now) &&
                e.TotalAttempts > 0) // Only entities that have been practiced
            .OrderBy(e => e.NextReview ?? DateTime.MinValue)
            .ThenByDescending(e => e.ImportanceScore)
            .Take(limit)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<UnifiedEntityDto>> GetFadingEntitiesAsync(
        int userId,
        double decayThreshold = 0.5,
        int limit = 10)
    {
        // Get all entities and filter by decay factor (computed property)
        var allEntities = await _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive && e.TotalAttempts > 0)
            .ToListAsync();

        // Filter by decay factor (can't do this in SQL as it's computed)
        var fadingEntities = allEntities
            .Where(e => e.DecayFactor < decayThreshold)
            .OrderBy(e => e.DecayFactor)
            .ThenByDescending(e => e.ImportanceScore)
            .Take(limit)
            .ToList();

        return fadingEntities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<UnifiedMasteryStatsDto> GetMasteryStatsAsync(int userId, string? subject = null)
    {
        var query = _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive);

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(e => e.Subject == subject);

        var entities = await query.ToListAsync();

        if (!entities.Any())
        {
            return new UnifiedMasteryStatsDto();
        }

        var masteredCount = entities.Count(e => e.MasteryScore >= 0.8);
        var learningCount = entities.Count(e => e.MasteryScore >= 0.3 && e.MasteryScore < 0.8);
        var newCount = entities.Count(e => e.MasteryScore < 0.3);

        var totalAttempts = entities.Sum(e => e.TotalAttempts);
        var totalCorrect = entities.Sum(e => e.TotalCorrect);

        // Calculate stats by subject
        var bySubject = entities
            .GroupBy(e => e.Subject)
            .ToDictionary(
                g => g.Key,
                g => new SubjectMasteryDto
                {
                    Subject = g.Key,
                    TotalEntities = g.Count(),
                    MasteredEntities = g.Count(e => e.MasteryScore >= 0.8),
                    AverageMastery = g.Average(e => e.MasteryScore),
                    Attempts = g.Sum(e => e.TotalAttempts),
                    Correct = g.Sum(e => e.TotalCorrect)
                });

        // Calculate stats by Bloom level
        var byBloom = new Dictionary<int, BloomLevelStatsDto>();
        for (int level = 1; level <= 6; level++)
        {
            var levelAttempts = 0;
            var levelCorrect = 0;

            foreach (var entity in entities)
            {
                var bloom = entity.BloomPerformance;
                if (bloom.TryGetValue(level, out var perf))
                {
                    levelAttempts += perf.Attempts;
                    levelCorrect += perf.Correct;
                }
            }

            byBloom[level] = new BloomLevelStatsDto
            {
                Level = level,
                Name = GetBloomLevelName(level),
                Attempts = levelAttempts,
                Correct = levelCorrect
            };
        }

        // Calculate difficulty stats
        var easyTotal = entities.Sum(e => e.EasyTotal);
        var easyCorrect = entities.Sum(e => e.EasyCorrect);
        var mediumTotal = entities.Sum(e => e.MediumTotal);
        var mediumCorrect = entities.Sum(e => e.MediumCorrect);
        var hardTotal = entities.Sum(e => e.HardTotal);
        var hardCorrect = entities.Sum(e => e.HardCorrect);
        var diffTotal = easyTotal + mediumTotal + hardTotal;

        // Count due for review
        var now = DateTime.UtcNow;
        var dueCount = entities.Count(e => e.NextReview != null && e.NextReview <= now);
        var overdueCount = entities.Count(e => e.NextReview != null && e.NextReview < now.AddDays(-1));

        // Find best streak
        var bestStreak = entities.Any() ? entities.Max(e => e.BestStreak) : 0;
        var currentStreak = entities.Any() ? entities.Max(e => e.CurrentStreak) : 0;

        return new UnifiedMasteryStatsDto
        {
            TotalEntities = entities.Count,
            MasteredEntities = masteredCount,
            LearningEntities = learningCount,
            NewEntities = newCount,
            AverageMastery = entities.Average(e => e.MasteryScore),
            AverageEffectiveKnowledge = entities.Average(e => e.EffectiveKnowledge),
            TotalAttempts = totalAttempts,
            TotalCorrect = totalCorrect,
            OverallSuccessRate = totalAttempts > 0 ? (double)totalCorrect / totalAttempts : 0,
            BySubject = bySubject,
            ByBloomLevel = byBloom,
            EasyStats = new DifficultyStatsDto
            {
                Difficulty = "easy",
                Total = easyTotal,
                Correct = easyCorrect,
                TargetRatio = 0.20,
                ActualRatio = diffTotal > 0 ? (double)easyTotal / diffTotal : 0
            },
            MediumStats = new DifficultyStatsDto
            {
                Difficulty = "medium",
                Total = mediumTotal,
                Correct = mediumCorrect,
                TargetRatio = 0.40,
                ActualRatio = diffTotal > 0 ? (double)mediumTotal / diffTotal : 0
            },
            HardStats = new DifficultyStatsDto
            {
                Difficulty = "hard",
                Total = hardTotal,
                Correct = hardCorrect,
                TargetRatio = 0.40,
                ActualRatio = diffTotal > 0 ? (double)hardTotal / diffTotal : 0
            },
            CurrentStreak = currentStreak,
            BestStreak = bestStreak,
            DueForReviewCount = dueCount,
            OverdueCount = overdueCount
        };
    }

    /// <inheritdoc />
    public async Task<List<UnifiedWeakAreaDto>> GetWeakAreasAsync(int userId, int limit = 10)
    {
        var entities = await _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive && e.TotalAttempts > 0)
            .ToListAsync();

        var weakAreas = new List<UnifiedWeakAreaDto>();
        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            string? reason = null;
            int priority = 0;

            // Low mastery
            if (entity.MasteryScore < 0.4)
            {
                reason = "low_mastery";
                priority = 100 - (int)(entity.MasteryScore * 100);
            }
            // Overdue for review
            else if (entity.NextReview != null && entity.NextReview < now)
            {
                reason = "overdue";
                var daysBehind = (now - entity.NextReview.Value).TotalDays;
                priority = Math.Min(100, 50 + (int)(daysBehind * 5));
            }
            // High error rate
            else if (entity.SuccessRate < 0.5 && entity.TotalAttempts >= 5)
            {
                reason = "high_error_rate";
                priority = (int)((1 - entity.SuccessRate) * 80);
            }
            // Decaying fast
            else if (entity.DecayFactor < 0.5)
            {
                reason = "decaying_fast";
                priority = (int)((1 - entity.DecayFactor) * 60);
            }

            if (reason != null)
            {
                // Check if blocked by prerequisites
                var checkResult = await CheckPrerequisitesAsync(entity.Id, userId);

                weakAreas.Add(new UnifiedWeakAreaDto
                {
                    EntityId = entity.Id,
                    EntityName = entity.Name,
                    EntityType = entity.EntityType,
                    Subject = entity.Subject,
                    Topic = entity.Topic,
                    MasteryScore = entity.MasteryScore,
                    EffectiveKnowledge = entity.EffectiveKnowledge,
                    Attempts = entity.TotalAttempts,
                    Correct = entity.TotalCorrect,
                    SuccessRate = entity.SuccessRate,
                    Reason = reason,
                    Priority = priority,
                    IsBlocked = !checkResult.CanProceed,
                    BlockingPrerequisites = checkResult.BlockingPrerequisites
                        .Select(b => b.EntityName)
                        .ToList()
                });
            }
        }

        return weakAreas
            .OrderByDescending(w => w.Priority)
            .Take(limit)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<UnifiedLearningSummaryDto> GetLearningSummaryAsync(int userId)
    {
        var stats = await GetMasteryStatsAsync(userId);
        var weakAreas = await GetWeakAreasAsync(userId, 5);
        var topPriority = await GetNextRecommendationAsync(userId);

        // Get today's activity
        var today = DateTime.UtcNow.Date;
        var entities = await _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive)
            .ToListAsync();

        var todayEntities = entities.Where(e => e.LastInteraction.Date == today).ToList();
        var todayAttempts = todayEntities.Sum(e => e.TotalAttempts); // Approximate
        var todayCorrect = todayEntities.Sum(e => e.TotalCorrect); // Approximate
        var todayNew = entities.Count(e => e.CreatedAt.Date == today);

        // Get blocked entities count
        var blockedCount = 0;
        foreach (var entity in entities.Take(50)) // Check first 50 for performance
        {
            var check = await CheckPrerequisitesAsync(entity.Id, userId);
            if (!check.CanProceed) blockedCount++;
        }

        // Get next deadline
        var nextAssignment = await _context.Set<MoodleAssignment>()
            .Where(a => a.UserId == userId && a.DueDate > DateTime.UtcNow)
            .OrderBy(a => a.DueDate)
            .FirstOrDefaultAsync();

        // Calculate streak risk
        var streakAtRisk = stats.CurrentStreak > 0 &&
            !entities.Any(e => e.LastInteraction.Date == today);

        return new UnifiedLearningSummaryDto
        {
            UserId = userId,
            TotalEntities = stats.TotalEntities,
            MasteredCount = stats.MasteredEntities,
            OverallMastery = stats.AverageMastery,
            OverallEffectiveKnowledge = stats.AverageEffectiveKnowledge,
            TodayAttempts = todayAttempts,
            TodayCorrect = todayCorrect,
            TodayNewEntities = todayNew,
            DueForReviewCount = stats.DueForReviewCount,
            WeakAreasCount = weakAreas.Count,
            BlockedEntitiesCount = blockedCount,
            TopPriority = topPriority,
            CurrentStreak = stats.CurrentStreak,
            BestStreak = stats.BestStreak,
            StreakAtRisk = streakAtRisk,
            NextDeadline = nextAssignment?.DueDate,
            NextDeadlineName = nextAssignment?.Title,
            DaysUntilNextDeadline = nextAssignment?.DueDate != null
                ? (int)(nextAssignment.DueDate.Value - DateTime.UtcNow).TotalDays
                : null
        };
    }
}
