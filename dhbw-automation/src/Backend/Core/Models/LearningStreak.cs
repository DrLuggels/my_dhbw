using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks daily learning streaks for gamification and bonus reinforcement.
/// Consecutive learning days provide a multiplier on knowledge reinforcement.
/// </summary>
[Table("learning_streaks")]
public class LearningStreak
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Current consecutive days of learning.
    /// </summary>
    public int CurrentStreak { get; set; } = 0;

    /// <summary>
    /// Longest streak ever achieved.
    /// </summary>
    public int LongestStreak { get; set; } = 0;

    /// <summary>
    /// Date of last learning activity.
    /// </summary>
    public DateTime LastActivityDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Streak multiplier for reinforcement bonus.
    /// Formula: 1.0 + min(0.5, CurrentStreak * 0.02)
    /// Day 1: 1.02x, Day 7: 1.14x, Day 25+: 1.5x (max)
    /// </summary>
    [NotMapped]
    public double StreakMultiplier => 1.0 + Math.Min(0.5, CurrentStreak * 0.02);

    /// <summary>
    /// Number of streak freeze tokens available.
    /// One freeze protects the streak if a day is missed.
    /// </summary>
    public int StreakFreezes { get; set; } = 1;

    /// <summary>
    /// When the last freeze was used.
    /// Freezes regenerate weekly.
    /// </summary>
    public DateTime? LastFreezeUsed { get; set; }

    /// <summary>
    /// Total exercises completed ever.
    /// </summary>
    public int TotalExercisesCompleted { get; set; } = 0;

    /// <summary>
    /// Total days with learning activity.
    /// </summary>
    public int TotalActiveDays { get; set; } = 0;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Result of a streak update operation.
/// </summary>
public class StreakUpdateResult
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double Multiplier { get; set; }
    public bool StreakBroken { get; set; }
    public bool FreezeUsed { get; set; }
    public bool IsNewRecord { get; set; }
    public string Message { get; set; } = string.Empty;
}
