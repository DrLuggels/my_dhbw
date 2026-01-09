using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("calendar_events")]
public class CalendarEvent
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

    [MaxLength(200)]
    public string? Location { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public bool IsAllDay { get; set; } = false;

    [MaxLength(100)]
    public string? EventType { get; set; }

    [MaxLength(100)]
    public string? Subject { get; set; }

    [MaxLength(100)]
    public string? Professor { get; set; }

    [MaxLength(255)]
    public string? ExternalId { get; set; }

    [MaxLength(50)]
    public string Source { get; set; } = "manual";

    [Column(TypeName = "TEXT")]
    public string? Notes { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
