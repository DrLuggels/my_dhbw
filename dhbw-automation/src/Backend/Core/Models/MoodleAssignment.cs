using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks Moodle assignments with deadlines
/// </summary>
[Table("moodle_assignments")]
public class MoodleAssignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Moodle course ID
    /// </summary>
    [Required]
    public int CourseId { get; set; }

    /// <summary>
    /// Course name for display
    /// </summary>
    [MaxLength(300)]
    public string? CourseName { get; set; }

    /// <summary>
    /// Moodle's internal assignment ID
    /// </summary>
    [Required]
    public int MoodleAssignmentId { get; set; }

    /// <summary>
    /// Assignment title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Assignment description/instructions
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    /// <summary>
    /// Due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Cutoff date (no submissions after this)
    /// </summary>
    public DateTime? CutoffDate { get; set; }

    /// <summary>
    /// Allow submissions from
    /// </summary>
    public DateTime? AllowSubmissionsFrom { get; set; }

    /// <summary>
    /// Maximum grade points
    /// </summary>
    public int MaxGrade { get; set; } = 100;

    /// <summary>
    /// Whether user has submitted
    /// </summary>
    public bool IsSubmitted { get; set; } = false;

    /// <summary>
    /// Submission timestamp
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Submission status from Moodle
    /// </summary>
    [MaxLength(50)]
    public string? SubmissionStatus { get; set; }

    /// <summary>
    /// Grade received (if graded)
    /// </summary>
    public double? Grade { get; set; }

    /// <summary>
    /// Grading status
    /// </summary>
    [MaxLength(50)]
    public string? GradingStatus { get; set; }

    /// <summary>
    /// Link to auto-created calendar event
    /// </summary>
    public int? CalendarEventId { get; set; }

    /// <summary>
    /// Link to auto-created todo
    /// </summary>
    public int? TodoId { get; set; }

    /// <summary>
    /// Last sync from Moodle
    /// </summary>
    public DateTime? SyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CalendarEventId")]
    public virtual CalendarEvent? CalendarEvent { get; set; }

    [ForeignKey("TodoId")]
    public virtual Todo? Todo { get; set; }
}
