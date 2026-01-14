using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks user performance per Knowledge Graph entity.
/// Used for adaptive difficulty and spaced repetition (FSRS algorithm).
/// </summary>
[Table("user_entity_performance")]
public class UserEntityPerformance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Knowledge Graph Entity ID
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// Question type this performance record is for (mc, fill_blank, true_false, short_answer, calculation, connection)
    /// Null means overall performance across all question types
    /// </summary>
    [MaxLength(50)]
    public string? QuestionType { get; set; }

    /// <summary>
    /// Bloom's Taxonomy level (1-6)
    /// 1=Remember, 2=Understand, 3=Apply, 4=Analyze, 5=Evaluate, 6=Create
    /// </summary>
    public int BloomLevel { get; set; } = 1;

    /// <summary>
    /// Total number of attempts
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Number of correct answers
    /// </summary>
    public int Correct { get; set; } = 0;

    /// <summary>
    /// Last attempt timestamp
    /// </summary>
    public DateTime? LastAttempt { get; set; }

    /// <summary>
    /// Mastery score (0.0 to 1.0)
    /// Calculated using FSRS algorithm
    /// </summary>
    public double MasteryScore { get; set; } = 0.0;

    /// <summary>
    /// Next scheduled review date (spaced repetition)
    /// </summary>
    public DateTime? NextReview { get; set; }

    // === FSRS (Free Spaced Repetition Scheduler) Parameters ===

    /// <summary>
    /// FSRS Stability - how long the memory will last
    /// </summary>
    public double Stability { get; set; } = 0.0;

    /// <summary>
    /// FSRS Difficulty - inherent difficulty of the item (0.0 to 1.0)
    /// </summary>
    public double Difficulty { get; set; } = 0.5;

    /// <summary>
    /// FSRS Elapsed days since last review
    /// </summary>
    public int ElapsedDays { get; set; } = 0;

    /// <summary>
    /// FSRS Scheduled days until next review
    /// </summary>
    public int ScheduledDays { get; set; } = 0;

    /// <summary>
    /// FSRS Number of reviews (reps)
    /// </summary>
    public int Reps { get; set; } = 0;

    /// <summary>
    /// FSRS Number of lapses (forgetting events)
    /// </summary>
    public int Lapses { get; set; } = 0;

    /// <summary>
    /// FSRS State: 0=New, 1=Learning, 2=Review, 3=Relearning
    /// </summary>
    public int State { get; set; } = 0;

    /// <summary>
    /// Average response time in seconds
    /// </summary>
    public double? AverageResponseTime { get; set; }

    /// <summary>
    /// Streak of consecutive correct answers
    /// </summary>
    public int CurrentStreak { get; set; } = 0;

    /// <summary>
    /// Best streak achieved
    /// </summary>
    public int BestStreak { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("EntityId")]
    public virtual KgEntity Entity { get; set; } = null!;
}

/// <summary>
/// Constants for question types
/// </summary>
public static class QuestionTypes
{
    public const string MultipleChoice = "mc";
    public const string FillInBlank = "fill_blank";
    public const string TrueFalse = "true_false";
    public const string ShortAnswer = "short_answer";
    public const string Calculation = "calculation";
    public const string Connection = "connection";
    public const string Essay = "essay";
}

/// <summary>
/// Constants for Bloom's Taxonomy levels
/// </summary>
public static class BloomLevels
{
    public const int Remember = 1;
    public const int Understand = 2;
    public const int Apply = 3;
    public const int Analyze = 4;
    public const int Evaluate = 5;
    public const int Create = 6;

    public static string GetName(int level) => level switch
    {
        1 => "Remember",
        2 => "Understand",
        3 => "Apply",
        4 => "Analyze",
        5 => "Evaluate",
        6 => "Create",
        _ => "Unknown"
    };
}

/// <summary>
/// FSRS State constants
/// </summary>
public static class FsrsStates
{
    public const int New = 0;
    public const int Learning = 1;
    public const int Review = 2;
    public const int Relearning = 3;
}
