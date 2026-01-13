using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Represents a simulated exam session with time pressure and no hints.
/// Used for realistic exam preparation.
/// </summary>
[Table("exam_simulations")]
public class ExamSimulation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Topic { get; set; }

    /// <summary>
    /// Optional link to a Moodle assignment this exam prepares for.
    /// </summary>
    public int? MoodleAssignmentId { get; set; }

    // Exam parameters
    public int TotalQuestions { get; set; } = 20;
    public int TimeLimitMinutes { get; set; } = 60;

    /// <summary>
    /// Difficulty distribution following 20/40/40 rule.
    /// </summary>
    public int EasyQuestions { get; set; } = 4;    // 20%
    public int MediumQuestions { get; set; } = 8;  // 40%
    public int HardQuestions { get; set; } = 8;    // 40%

    // Exam mode restrictions
    public bool NoHints { get; set; } = true;
    public bool NoRetries { get; set; } = true;
    public bool TimePressure { get; set; } = true;

    // Progress tracking
    public int CurrentQuestionIndex { get; set; } = 0;
    public int CorrectAnswers { get; set; } = 0;
    public int IncorrectAnswers { get; set; } = 0;
    public int SkippedAnswers { get; set; } = 0;

    /// <summary>
    /// JSON-serialized list of question IDs in order.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string QuestionIds { get; set; } = "[]";

    /// <summary>
    /// JSON-serialized list of user answers.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string UserAnswers { get; set; } = "[]";

    // Timing
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Remaining time in seconds at last pause/update.
    /// </summary>
    public int? RemainingSeconds { get; set; }

    // Results
    /// <summary>
    /// Final score as percentage (0-100).
    /// </summary>
    public double Score { get; set; } = 0.0;

    /// <summary>
    /// AI-generated feedback after exam completion.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Feedback { get; set; }

    /// <summary>
    /// Exam status.
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = ExamStatus.NotStarted;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("MoodleAssignmentId")]
    public virtual MoodleAssignment? MoodleAssignment { get; set; }

    // Computed properties
    [NotMapped]
    public bool IsExpired => StartedAt.HasValue &&
                              (DateTime.UtcNow - StartedAt.Value).TotalMinutes > TimeLimitMinutes;

    [NotMapped]
    public int AnsweredQuestions => CorrectAnswers + IncorrectAnswers;

    [NotMapped]
    public double CorrectPercentage => TotalQuestions > 0
        ? (double)CorrectAnswers / TotalQuestions * 100
        : 0;
}

/// <summary>
/// Exam status constants.
/// </summary>
public static class ExamStatus
{
    public const string NotStarted = "not_started";
    public const string InProgress = "in_progress";
    public const string Paused = "paused";
    public const string Completed = "completed";
    public const string Expired = "expired";
    public const string Cancelled = "cancelled";
}

/// <summary>
/// Result of submitting an exam answer.
/// </summary>
public class ExamAnswerResult
{
    public bool IsCorrect { get; set; }
    public int QuestionNumber { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectSoFar { get; set; }
    public int RemainingSeconds { get; set; }
    public bool IsLastQuestion { get; set; }
    public string? CorrectAnswer { get; set; }  // Only shown after exam completion
}

/// <summary>
/// Final exam result with detailed feedback.
/// </summary>
public class ExamResult
{
    public int ExamId { get; set; }
    public double Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public int TimeTakenMinutes { get; set; }
    public string Grade { get; set; } = string.Empty;  // A, B, C, D, F
    public string Feedback { get; set; } = string.Empty;
    public List<ExamQuestionReview> QuestionReviews { get; set; } = new();
}

/// <summary>
/// Review of a single exam question.
/// </summary>
public class ExamQuestionReview
{
    public int QuestionIndex { get; set; }
    public string Question { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}
