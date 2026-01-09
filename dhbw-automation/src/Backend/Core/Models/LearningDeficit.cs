using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("learning_deficits")]
public class LearningDeficit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty; // "Mathematik", "Programmierung", etc.

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty; // "Lineare Algebra", "OOP", etc.

    [MaxLength(200)]
    public string? Subtopic { get; set; } // "Matrizen", "Vererbung", etc.

    [Required]
    [MaxLength(100)]
    public string ErrorType { get; set; } = string.Empty; // "concept", "calculation", "application"

    [Column(TypeName = "TEXT")]
    public string ErrorDescription { get; set; } = string.Empty;

    public int OccurrenceCount { get; set; } = 1; // Wie oft dieser Fehler aufgetreten ist

    public DateTime FirstOccurrence { get; set; } = DateTime.UtcNow;
    public DateTime LastOccurrence { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Severity { get; set; } = "low"; // "low", "medium", "high", "critical"

    public bool NeedsTutoring { get; set; } = false; // Automatisch auf true wenn Count > 3

    // Relations zu Dokumenten mit diesem Fehler
    [Column(TypeName = "JSON")]
    public string RelatedDocumentIds { get; set; } = "[]"; // JSON Array

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
