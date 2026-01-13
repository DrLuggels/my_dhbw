using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for managing daily learning streaks.
/// Provides gamification through streak bonuses and multipliers.
/// </summary>
public class LearningStreakService : ILearningStreakService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LearningStreakService> _logger;

    public LearningStreakService(
        AppDbContext dbContext,
        ILogger<LearningStreakService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LearningStreak> GetStreakAsync(int userId)
    {
        var streak = await _dbContext.LearningStreaks
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (streak == null)
        {
            streak = new LearningStreak
            {
                UserId = userId,
                CurrentStreak = 0,
                LongestStreak = 0,
                StreakFreezes = 1,
                LastActivityDate = DateTime.UtcNow.AddDays(-1) // No activity yet
            };
            _dbContext.LearningStreaks.Add(streak);
            await _dbContext.SaveChangesAsync();
        }

        return streak;
    }

    /// <inheritdoc />
    public async Task<StreakUpdateResult> UpdateStreakAsync(int userId)
    {
        var streak = await GetStreakAsync(userId);
        var result = new StreakUpdateResult();
        var today = DateTime.UtcNow.Date;
        var lastDay = streak.LastActivityDate.Date;

        if (lastDay == today)
        {
            // Already learned today - just return current state
            result.CurrentStreak = streak.CurrentStreak;
            result.LongestStreak = streak.LongestStreak;
            result.Multiplier = streak.StreakMultiplier;
            result.Message = "Bereits heute gelernt!";
            return result;
        }

        if (lastDay == today.AddDays(-1))
        {
            // Consecutive day - extend streak!
            streak.CurrentStreak++;
            result.IsNewRecord = streak.CurrentStreak > streak.LongestStreak;

            if (result.IsNewRecord)
            {
                streak.LongestStreak = streak.CurrentStreak;
            }

            result.Message = streak.CurrentStreak switch
            {
                7 => "Eine Woche am Stück! Weiter so!",
                30 => "Ein ganzer Monat! Unglaublich!",
                100 => "100 Tage! Du bist ein Lern-Champion!",
                _ => $"Tag {streak.CurrentStreak}! Streak-Multiplikator: {streak.StreakMultiplier:F2}x"
            };
        }
        else if (lastDay < today.AddDays(-1))
        {
            // Streak broken - check for freeze
            if (streak.StreakFreezes > 0 && lastDay >= today.AddDays(-2))
            {
                // Use freeze to save streak
                streak.StreakFreezes--;
                streak.LastFreezeUsed = DateTime.UtcNow;
                result.FreezeUsed = true;
                result.Message = "Streak-Freeze verwendet! Dein Streak wurde gerettet!";
            }
            else
            {
                // Streak lost
                result.StreakBroken = true;
                streak.CurrentStreak = 1; // Start new streak
                result.Message = "Dein Streak wurde zurückgesetzt. Starte neu durch!";
            }
        }

        streak.LastActivityDate = DateTime.UtcNow;
        streak.TotalActiveDays++;
        streak.UpdatedAt = DateTime.UtcNow;

        result.CurrentStreak = streak.CurrentStreak;
        result.LongestStreak = streak.LongestStreak;
        result.Multiplier = streak.StreakMultiplier;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Updated streak for user {UserId}: Day {StreakDay}, Multiplier {Multiplier:F2}x",
            userId, streak.CurrentStreak, streak.StreakMultiplier);

        return result;
    }

    /// <inheritdoc />
    public async Task<double> GetMultiplierAsync(int userId)
    {
        var streak = await GetStreakAsync(userId);
        return streak.StreakMultiplier;
    }

    /// <inheritdoc />
    public async Task<bool> UseStreakFreezeAsync(int userId)
    {
        var streak = await GetStreakAsync(userId);

        if (streak.StreakFreezes <= 0)
        {
            return false;
        }

        streak.StreakFreezes--;
        streak.LastFreezeUsed = DateTime.UtcNow;
        streak.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<StreakStatistics> GetStatisticsAsync(int userId)
    {
        var streak = await GetStreakAsync(userId);
        var today = DateTime.UtcNow.Date;
        var lastDay = streak.LastActivityDate.Date;

        var daysUntilLost = lastDay == today
            ? 2 // Learned today, will lose after tomorrow if not learning
            : lastDay == today.AddDays(-1)
                ? 1 // Learned yesterday, will lose tomorrow if not learning today
                : 0; // Already lost or never started

        return new StreakStatistics
        {
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            Multiplier = streak.StreakMultiplier,
            FreezesAvailable = streak.StreakFreezes,
            LastActivity = streak.LastActivityDate,
            TotalExercises = streak.TotalExercisesCompleted,
            TotalActiveDays = streak.TotalActiveDays,
            LearnedToday = lastDay == today,
            DaysUntilStreakLost = daysUntilLost
        };
    }

    /// <inheritdoc />
    public async Task<bool> HasLearnedTodayAsync(int userId)
    {
        var streak = await GetStreakAsync(userId);
        return streak.LastActivityDate.Date == DateTime.UtcNow.Date;
    }

    /// <inheritdoc />
    public async Task IncrementExerciseCountAsync(int userId)
    {
        var streak = await GetStreakAsync(userId);
        streak.TotalExercisesCompleted++;
        streak.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}
