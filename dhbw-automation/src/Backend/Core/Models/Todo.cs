using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("todos")]
public class Todo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "general"; // "meeting", "learning", "project", "general"

    [Required]
    [MaxLength(50)]
    public string Priority { get; set; } = "medium"; // "low", "medium", "high", "urgent"

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // "pending", "in_progress", "completed", "cancelled"

    public DateTime? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }

    // Relations
    public int? RelatedDocumentId { get; set; }
    public int? RelatedEventId { get; set; }
    public int? RelatedProjectId { get; set; }

    // AI-generierte Kontextinformationen
    [Column(TypeName = "TEXT")]
    public string? ExtractedFrom { get; set; } // Text aus dem die TODO extrahiert wurde

    [Column(TypeName = "TEXT")]
    public string? AiSuggestion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RelatedDocumentId")]
    public virtual Document? RelatedDocument { get; set; }

    [ForeignKey("RelatedEventId")]
    public virtual CalendarEvent? RelatedEvent { get; set; }

    [ForeignKey("RelatedProjectId")]
    public virtual Project? RelatedProject { get; set; }
}
