using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("user_interactions")]
public class UserInteraction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string InteractionType { get; set; } = string.Empty; // "schedule_meeting", "schedule_learning", "acknowledge_deficit", etc.

    [Column(TypeName = "TEXT")]
    public string Context { get; set; } = string.Empty; // JSON mit relevanten Daten

    [Required]
    [Column(TypeName = "TEXT")]
    public string Question { get; set; } = string.Empty; // Die Frage an den User

    [Column(TypeName = "JSON")]
    public string? SuggestedOptions { get; set; } // JSON Array von Vorschlägen

    [Column(TypeName = "TEXT")]
    public string? UserResponse { get; set; } // null = noch nicht beantwortet

    public DateTime? RespondedAt { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // "pending", "answered", "snoozed", "dismissed"

    public DateTime? SnoozeUntil { get; set; }

    public int? RelatedDocumentId { get; set; }
    public int? RelatedEventId { get; set; }
    public int? RelatedTodoId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RelatedDocumentId")]
    public virtual Document? RelatedDocument { get; set; }

    [ForeignKey("RelatedEventId")]
    public virtual CalendarEvent? RelatedEvent { get; set; }

    [ForeignKey("RelatedTodoId")]
    public virtual Todo? RelatedTodo { get; set; }
}
