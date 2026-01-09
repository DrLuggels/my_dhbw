using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("reminders")]
public class Reminder
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
    public DateTime DueDate { get; set; }

    [MaxLength(50)]
    public string Priority { get; set; } = "medium";

    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    [MaxLength(100)]
    public string? Category { get; set; }

    public int? RelatedEventId { get; set; }

    public int? RelatedDocumentId { get; set; }

    public bool IsRecurring { get; set; } = false;

    [MaxLength(50)]
    public string? RecurrencePattern { get; set; }

    public bool NotificationSent { get; set; } = false;

    public DateTime? NotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RelatedEventId")]
    public virtual CalendarEvent? RelatedEvent { get; set; }

    [ForeignKey("RelatedDocumentId")]
    public virtual Document? RelatedDocument { get; set; }
}
