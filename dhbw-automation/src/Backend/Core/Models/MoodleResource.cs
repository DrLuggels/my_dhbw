using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks Moodle course resources (files, URLs, pages)
/// </summary>
[Table("moodle_resources")]
public class MoodleResource
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
    /// Resource type: "file", "url", "page", "folder", "label"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Moodle's internal resource ID
    /// </summary>
    [Required]
    public int MoodleResourceId { get; set; }

    /// <summary>
    /// Resource/file title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description from Moodle
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    /// <summary>
    /// Download URL (for files)
    /// </summary>
    [MaxLength(1000)]
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// External URL (for URL resources)
    /// </summary>
    [MaxLength(1000)]
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// File type/extension
    /// </summary>
    [MaxLength(100)]
    public string? FileType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Section number in course
    /// </summary>
    public int SectionNumber { get; set; } = 0;

    /// <summary>
    /// Section name
    /// </summary>
    [MaxLength(300)]
    public string? SectionName { get; set; }

    /// <summary>
    /// Link to local document (after download)
    /// </summary>
    public int? LocalDocumentId { get; set; }

    /// <summary>
    /// Whether this resource has been downloaded
    /// </summary>
    public bool IsDownloaded { get; set; } = false;

    /// <summary>
    /// Last time Moodle was checked for updates
    /// </summary>
    public DateTime? LastCheckedAt { get; set; }

    /// <summary>
    /// When the resource was synced
    /// </summary>
    public DateTime? SyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("LocalDocumentId")]
    public virtual Document? LocalDocument { get; set; }
}
