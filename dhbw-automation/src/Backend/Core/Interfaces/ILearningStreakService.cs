using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for managing daily learning streaks.
/// Provides gamification through streak bonuses and multipliers.
/// </summary>
public interface ILearningStreakService
{
    /// <summary>
    /// Gets the current streak for a user.
    /// </summary>
    Task<LearningStreak> GetStreakAsync(int userId);

    /// <summary>
    /// Updates the streak after a learning activity.
    /// </summary>
    Task<StreakUpdateResult> UpdateStreakAsync(int userId);

    /// <summary>
    /// Gets the current streak multiplier for a user.
    /// </summary>
    Task<double> GetMultiplierAsync(int userId);

    /// <summary>
    /// Uses a streak freeze to protect the streak.
    /// </summary>
    Task<bool> UseStreakFreezeAsync(int userId);

    /// <summary>
    /// Gets streak statistics for a user.
    /// </summary>
    Task<StreakStatistics> GetStatisticsAsync(int userId);

    /// <summary>
    /// Checks if user has learned today.
    /// </summary>
    Task<bool> HasLearnedTodayAsync(int userId);

    /// <summary>
    /// Increments exercise count.
    /// </summary>
    Task IncrementExerciseCountAsync(int userId);
}

/// <summary>
/// Statistics about user's learning streak.
/// </summary>
public class StreakStatistics
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double Multiplier { get; set; }
    public int FreezesAvailable { get; set; }
    public DateTime? LastActivity { get; set; }
    public int TotalExercises { get; set; }
    public int TotalActiveDays { get; set; }
    public bool LearnedToday { get; set; }
    public int DaysUntilStreakLost { get; set; }
}
