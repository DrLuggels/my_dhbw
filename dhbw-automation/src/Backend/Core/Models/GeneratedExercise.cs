using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("generated_exercises")]
public class GeneratedExercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? DeficitId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ExerciseType { get; set; } = string.Empty; // "multiple_choice", "text_answer", "calculation", "code"

    [Column(TypeName = "TEXT")]
    public string Question { get; set; } = string.Empty; // HTML-formatiert

    [Column(TypeName = "TEXT")]
    public string? HelpText { get; set; }

    [Column(TypeName = "TEXT")]
    public string CorrectAnswer { get; set; } = string.Empty; // JSON mit korrekter Lösung

    [Column(TypeName = "TEXT")]
    public string? Explanation { get; set; } // Erklärung der Lösung

    [Required]
    [MaxLength(50)]
    public string Difficulty { get; set; } = "medium"; // "easy", "medium", "hard"

    // User Response
    [Column(TypeName = "TEXT")]
    public string? UserAnswer { get; set; }

    public bool? IsCorrect { get; set; }
    public DateTime? AnsweredAt { get; set; }

    // Knowledge Base Integration for Periodic Reviews
    public int? KnowledgeBaseItemId { get; set; }
    public bool IsPeriodicReview { get; set; } = false;

    // Spaced Repetition
    public DateTime NextReviewDate { get; set; } = DateTime.UtcNow;
    public int ReviewCount { get; set; } = 0;
    public double EaseFactor { get; set; } = 2.5; // SM-2 Algorithm

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("DeficitId")]
    public virtual LearningDeficit? Deficit { get; set; }

    [ForeignKey("KnowledgeBaseItemId")]
    public virtual KnowledgeBaseItem? KnowledgeBaseItem { get; set; }
}
