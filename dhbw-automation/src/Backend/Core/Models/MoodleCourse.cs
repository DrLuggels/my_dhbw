using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks Moodle courses for a user
/// </summary>
[Table("moodle_courses")]
public class MoodleCourse
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Moodle's internal course ID
    /// </summary>
    [Required]
    public int MoodleCourseId { get; set; }

    /// <summary>
    /// Short course name (e.g. "WDS125")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Shortname { get; set; } = string.Empty;

    /// <summary>
    /// Full course name
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Fullname { get; set; } = string.Empty;

    /// <summary>
    /// Course summary/description
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Summary { get; set; }

    /// <summary>
    /// Course format (topics, weeks, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? Format { get; set; }

    /// <summary>
    /// Course start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Course end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether the course is visible to the user
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Course completion progress (0-100)
    /// </summary>
    public int? Progress { get; set; }

    /// <summary>
    /// When the course was last synced from Moodle
    /// </summary>
    public DateTime? LastSynced { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    // Related assignments and resources
    public virtual ICollection<MoodleAssignment> Assignments { get; set; } = new List<MoodleAssignment>();
    public virtual ICollection<MoodleResource> Resources { get; set; } = new List<MoodleResource>();
}
