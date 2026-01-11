using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Interactive exercise with step-by-step progression (Brilliant-style)
/// Used for conceptual learning and fundamentals
/// </summary>
[Table("interactive_exercises")]
public class InteractiveExercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? DeficitId { get; set; }
    public int? KnowledgeBaseItemId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Difficulty { get; set; } = "medium"; // "easy", "medium", "hard"

    /// <summary>
    /// JSON-serialized exercise content with steps, components, and feedback
    /// Schema version 2.0 with InteractiveExerciseContent structure
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string ExerciseContent { get; set; } = "{}";

    /// <summary>
    /// JSON-serialized step progress tracking
    /// Maps stepId to StepProgress (completed, score, attempts, userAnswer)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string StepProgress { get; set; } = "{}";

    // Progress tracking
    public int CompletedSteps { get; set; } = 0;
    public int TotalSteps { get; set; } = 0;
    public double Score { get; set; } = 0; // Overall score 0-100
    public int TimeSpentSeconds { get; set; } = 0;

    // Timestamps
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Spaced Repetition (same as GeneratedExercise for consistency)
    public DateTime NextReviewDate { get; set; } = DateTime.UtcNow;
    public int ReviewCount { get; set; } = 0;
    public double EaseFactor { get; set; } = 2.5; // SM-2 Algorithm

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("DeficitId")]
    public virtual LearningDeficit? Deficit { get; set; }

    [ForeignKey("KnowledgeBaseItemId")]
    public virtual KnowledgeBaseItem? KnowledgeBaseItem { get; set; }
}

/// <summary>
/// JSON structure for ExerciseContent - used for serialization/deserialization
/// </summary>
public class InteractiveExerciseContent
{
    public string Version { get; set; } = "2.0";
    public ExerciseMetadata Metadata { get; set; } = new();
    public List<ExerciseStep> Steps { get; set; } = new();
}

public class ExerciseMetadata
{
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "medium";
    public int EstimatedMinutes { get; set; } = 10;
    public List<string> LearningObjectives { get; set; } = new();
    public List<string> Prerequisites { get; set; } = new();
}

public class ExerciseStep
{
    public string Id { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty; // HTML-safe content
    public ExerciseComponent Component { get; set; } = new();
    public ValidationRule Validation { get; set; } = new();
    public FeedbackConfig Feedback { get; set; } = new();
    public List<Hint> Hints { get; set; } = new();
}

public class ExerciseComponent
{
    public string Type { get; set; } = "text_input"; // multiple_choice, drag_drop, slider_range, code_editor, fill_blank, match_pairs
    public Dictionary<string, object> Config { get; set; } = new();
    public List<ComponentOption>? Options { get; set; } // For multiple_choice
    public List<DraggableItem>? Draggables { get; set; } // For drag_drop
    public List<DropZone>? DropZones { get; set; } // For drag_drop
    public string? CorrectAnswer { get; set; } // For simple types
}

public class ComponentOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
}

public class DraggableItem
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
}

public class DropZone
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<string> AcceptedItems { get; set; } = new(); // IDs of correct draggables
    public int? MaxItems { get; set; }
}

public class ValidationRule
{
    public string Type { get; set; } = "exact"; // exact, fuzzy, custom, code_execution
    public bool RealTimeValidation { get; set; } = false;
    public bool PartialCredit { get; set; } = false;
    public Dictionary<string, object>? Params { get; set; }
}

public class FeedbackConfig
{
    public FeedbackMessage OnCorrect { get; set; } = new() { Message = "Richtig!" };
    public FeedbackMessage OnIncorrect { get; set; } = new() { Message = "Nicht ganz richtig." };
    public FeedbackMessage? OnPartialCorrect { get; set; }
}

public class FeedbackMessage
{
    public string Message { get; set; } = string.Empty;
    public string? Animation { get; set; } // confetti, checkmark, bounce
    public bool ShowExplanation { get; set; } = true;
    public bool AllowRetry { get; set; } = true;
    public int? MaxRetries { get; set; }
}

public class Hint
{
    public int Order { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Cost { get; set; } // Points deducted for using hint
}

/// <summary>
/// JSON structure for StepProgress tracking
/// </summary>
public class StepProgressData
{
    public Dictionary<string, StepProgressEntry> Steps { get; set; } = new();
}

public class StepProgressEntry
{
    public bool Completed { get; set; }
    public double Score { get; set; }
    public int Attempts { get; set; }
    public string? UserAnswer { get; set; } // JSON-serialized answer
    public int HintsUsed { get; set; }
    public DateTime? CompletedAt { get; set; }
}
