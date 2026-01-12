using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks Moodle calendar events
/// </summary>
[Table("moodle_calendar_events")]
public class MoodleCalendarEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Moodle's internal event ID
    /// </summary>
    [Required]
    public int MoodleEventId { get; set; }

    /// <summary>
    /// Course ID (optional for site-wide events)
    /// </summary>
    public int? CourseId { get; set; }

    /// <summary>
    /// Course name for display
    /// </summary>
    [MaxLength(300)]
    public string? CourseName { get; set; }

    /// <summary>
    /// Event name/title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Event description
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    /// <summary>
    /// Event type: due, course, user, site, etc.
    /// </summary>
    [MaxLength(50)]
    public string? EventType { get; set; }

    /// <summary>
    /// Module name: assign, quiz, forum, etc.
    /// </summary>
    [MaxLength(100)]
    public string? ModuleName { get; set; }

    /// <summary>
    /// Event start time
    /// </summary>
    [Required]
    public DateTime TimeStart { get; set; }

    /// <summary>
    /// Event duration in seconds
    /// </summary>
    public int TimeDuration { get; set; } = 0;

    /// <summary>
    /// Link to local calendar event
    /// </summary>
    public int? CalendarEventId { get; set; }

    /// <summary>
    /// When the event was last synced
    /// </summary>
    public DateTime? SyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CalendarEventId")]
    public virtual CalendarEvent? CalendarEvent { get; set; }
}
