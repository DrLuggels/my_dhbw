using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.OmniLearning;

public partial class OmniLearningEngineService
{
    #region Analytics

    /// <summary>
    /// Holt Mastery-Statistiken
    /// </summary>
    public async Task<MasteryStatsDto> GetMasteryStatsAsync(int userId)
    {
        var entities = await _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive)
            .AsNoTracking()
            .ToListAsync();

        var stats = new MasteryStatsDto
        {
            TotalEntities = entities.Count,
            MasteredEntities = entities.Count(e => e.MasteryScore >= 0.8),
            LearningEntities = entities.Count(e => e.MasteryScore >= 0.3 && e.MasteryScore < 0.8),
            NewEntities = entities.Count(e => e.MasteryScore < 0.3),
            TotalExercises = entities.Sum(e => e.TotalAttempts),
            CorrectAnswers = entities.Sum(e => e.TotalCorrect),
            OverallMastery = entities.Any() ? entities.Average(e => e.MasteryScore) : 0,
            OverallSuccessRate = entities.Sum(e => e.TotalAttempts) > 0
                ? (double)entities.Sum(e => e.TotalCorrect) / entities.Sum(e => e.TotalAttempts)
                : 0
        };

        // Nach Subject gruppieren
        var bySubject = entities.GroupBy(e => e.Subject);
        foreach (var group in bySubject)
        {
            var subjectEntities = group.ToList();
            stats.BySubject[group.Key] = new SubjectStats
            {
                Subject = group.Key,
                EntityCount = subjectEntities.Count,
                AverageMastery = subjectEntities.Average(e => e.MasteryScore),
                ExerciseCount = subjectEntities.Sum(e => e.TotalAttempts),
                SuccessRate = subjectEntities.Sum(e => e.TotalAttempts) > 0
                    ? (double)subjectEntities.Sum(e => e.TotalCorrect) / subjectEntities.Sum(e => e.TotalAttempts)
                    : 0
            };
        }

        // Nach Bloom Level
        for (int level = 1; level <= 6; level++)
        {
            var levelEntities = entities.Where(e => e.CurrentBloomLevel == level).ToList();
            if (levelEntities.Any())
            {
                stats.ByBloomLevel[level] = levelEntities.Average(e => e.MasteryScore);
            }
        }

        return stats;
    }

    /// <summary>
    /// Holt Lern-Streak-Informationen
    /// </summary>
    public async Task<LearningStreakDto> GetStreakAsync(int userId)
    {
        var streak = await _context.LearningStreaks
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (streak == null)
        {
            return new LearningStreakDto
            {
                CurrentStreak = 0,
                BestStreak = 0,
                TotalActiveDays = 0
            };
        }

        // Aktualisiere Streak falls nötig
        var today = DateTime.UtcNow.Date;
        var lastActivity = streak.LastActivityDate.Date;
        var daysSinceActivity = (today - lastActivity).Days;

        if (daysSinceActivity > 1)
        {
            // Streak unterbrochen
            streak.CurrentStreak = 0;
        }

        // Hole letzte Aktivitätstage
        var recentActivities = await _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive)
            .Select(e => e.LastInteraction.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(30)
            .ToListAsync();

        return new LearningStreakDto
        {
            CurrentStreak = streak.CurrentStreak,
            BestStreak = streak.LongestStreak,
            LastActivityDate = streak.LastActivityDate,
            TotalActiveDays = streak.TotalActiveDays,
            RecentActivityDates = recentActivities
        };
    }

    /// <summary>
    /// Holt Schwierigkeitsverteilung (20/40/40)
    /// </summary>
    public async Task<DifficultyDistributionDto> GetDifficultyDistributionAsync(int userId, string? subject = null)
    {
        var query = _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive);

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(e => e.Subject == subject);

        var entities = await query.AsNoTracking().ToListAsync();

        var distribution = new DifficultyDistributionDto
        {
            EasyTotal = entities.Sum(e => e.EasyTotal),
            EasyCorrect = entities.Sum(e => e.EasyCorrect),
            MediumTotal = entities.Sum(e => e.MediumTotal),
            MediumCorrect = entities.Sum(e => e.MediumCorrect),
            HardTotal = entities.Sum(e => e.HardTotal),
            HardCorrect = entities.Sum(e => e.HardCorrect)
        };

        distribution.EasySuccessRate = distribution.EasyTotal > 0
            ? (double)distribution.EasyCorrect / distribution.EasyTotal
            : 0;
        distribution.MediumSuccessRate = distribution.MediumTotal > 0
            ? (double)distribution.MediumCorrect / distribution.MediumTotal
            : 0;
        distribution.HardSuccessRate = distribution.HardTotal > 0
            ? (double)distribution.HardCorrect / distribution.HardTotal
            : 0;

        var total = distribution.EasyTotal + distribution.MediumTotal + distribution.HardTotal;
        if (total > 0)
        {
            var easyRatio = (double)distribution.EasyTotal / total;
            var mediumRatio = (double)distribution.MediumTotal / total;
            var hardRatio = (double)distribution.HardTotal / total;

            // Prüfe ob 20/40/40 Regel erfüllt ist (mit 5% Toleranz)
            distribution.FollowsTwentyFortyForty =
                Math.Abs(easyRatio - 0.20) <= 0.05 &&
                Math.Abs(mediumRatio - 0.40) <= 0.05 &&
                Math.Abs(hardRatio - 0.40) <= 0.05;

            distribution.RecommendedDifficulty = DetermineRecommendedDifficulty(
                distribution.EasyTotal, distribution.MediumTotal, distribution.HardTotal);

            if (!distribution.FollowsTwentyFortyForty)
            {
                if (easyRatio < 0.15)
                    distribution.DistributionAdvice = "Mehr leichte Übungen machen um Grundlagen zu festigen";
                else if (mediumRatio < 0.35)
                    distribution.DistributionAdvice = "Mehr mittelschwere Übungen für optimales Lernen";
                else if (hardRatio < 0.35)
                    distribution.DistributionAdvice = "Bereit für mehr Herausforderung - versuche schwere Übungen";
                else
                    distribution.DistributionAdvice = "Verteilung ist ausgewogen";
            }
            else
            {
                distribution.DistributionAdvice = "Perfekte 20/40/40 Verteilung - weiter so!";
            }
        }
        else
        {
            distribution.RecommendedDifficulty = "easy";
            distribution.DistributionAdvice = "Starte mit leichten Übungen";
        }

        return distribution;
    }

    /// <summary>
    /// Holt Bloom-Progression
    /// </summary>
    public async Task<BloomProgressionDto> GetBloomProgressionAsync(int userId, string? subject = null)
    {
        var query = _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive);

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(e => e.Subject == subject);

        var entities = await query.AsNoTracking().ToListAsync();

        var progression = new BloomProgressionDto
        {
            CurrentLevel = entities.Any() ? (int)Math.Round(entities.Average(e => e.CurrentBloomLevel)) : 1,
            TargetLevel = 6
        };

        progression.CurrentLevelName = GetBloomLevelName(progression.CurrentLevel);
        progression.TargetLevelName = GetBloomLevelName(progression.TargetLevel);

        // Statistiken pro Level
        for (int level = 1; level <= 6; level++)
        {
            var levelEntities = entities.Where(e => e.CurrentBloomLevel >= level).ToList();
            var totalAttempts = 0;
            var totalCorrect = 0;

            foreach (var entity in entities)
            {
                var bloomPerf = entity.BloomPerformance;
                if (bloomPerf.TryGetValue(level, out var perf))
                {
                    totalAttempts += perf.Attempts;
                    totalCorrect += perf.Correct;
                }
            }

            progression.LevelStats[level] = new BloomLevelStats
            {
                Level = level,
                Name = GetBloomLevelName(level),
                Attempts = totalAttempts,
                Correct = totalCorrect,
                SuccessRate = totalAttempts > 0 ? (double)totalCorrect / totalAttempts : 0,
                IsMastered = totalAttempts >= 3 && totalCorrect >= totalAttempts * 0.7
            };
        }

        // Kann zum nächsten Level aufsteigen?
        if (progression.CurrentLevel < 6 &&
            progression.LevelStats.TryGetValue(progression.CurrentLevel, out var currentStats))
        {
            progression.CanAdvance = currentStats.IsMastered;
        }

        if (progression.CanAdvance)
        {
            progression.ProgressAdvice = $"Bereit für Level {progression.CurrentLevel + 1}: {GetBloomLevelName(progression.CurrentLevel + 1)}!";
        }
        else if (progression.CurrentLevel < 6)
        {
            var currentLevelStats = progression.LevelStats.GetValueOrDefault(progression.CurrentLevel);
            if (currentLevelStats != null && currentLevelStats.Attempts < 3)
            {
                progression.ProgressAdvice = $"Noch {3 - currentLevelStats.Attempts} Übungen auf Level {GetBloomLevelName(progression.CurrentLevel)} nötig";
            }
            else
            {
                progression.ProgressAdvice = $"Erfolgsrate auf Level {GetBloomLevelName(progression.CurrentLevel)} verbessern (70% benötigt)";
            }
        }
        else
        {
            progression.ProgressAdvice = "Maximales Bloom-Level erreicht!";
        }

        return progression;
    }

    #endregion
}
