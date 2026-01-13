using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks Moodle course resources (files, URLs, pages, folders, books, wikis, etc.)
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
    /// Resource type: "resource", "file", "url", "page", "folder", "label", "book", "wiki", "glossary", "forum", "quiz"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Moodle's internal resource ID (module instance ID)
    /// </summary>
    [Required]
    public int MoodleResourceId { get; set; }

    /// <summary>
    /// Moodle course module ID (cmid)
    /// </summary>
    public int? MoodleCourseModuleId { get; set; }

    /// <summary>
    /// Resource/file title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description/intro from Moodle (may contain HTML)
    /// </summary>
    [Column(TypeName = "LONGTEXT")]
    public string? Description { get; set; }

    /// <summary>
    /// HTML content for pages, labels, wiki pages, etc.
    /// </summary>
    [Column(TypeName = "LONGTEXT")]
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Download URL (for files)
    /// </summary>
    [MaxLength(2000)]
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// External URL (for URL resources)
    /// </summary>
    [MaxLength(2000)]
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// File type/extension (e.g., ".pdf", "application/pdf")
    /// </summary>
    [MaxLength(100)]
    public string? FileType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// File path within folder (for folder contents)
    /// </summary>
    [MaxLength(500)]
    public string? FilePath { get; set; }

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
    /// Parent resource ID (for hierarchical resources like book chapters, wiki subpages)
    /// </summary>
    public int? ParentResourceId { get; set; }

    /// <summary>
    /// Additional metadata as JSON (for quiz info, forum stats, etc.)
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Link to local document (after download)
    /// </summary>
    public int? LocalDocumentId { get; set; }

    /// <summary>
    /// Whether this resource has been downloaded
    /// </summary>
    public bool IsDownloaded { get; set; } = false;

    /// <summary>
    /// Whether this resource is visible in Moodle
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Last modification time in Moodle (Unix timestamp converted)
    /// </summary>
    public DateTime? MoodleTimeModified { get; set; }

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

    [ForeignKey("ParentResourceId")]
    public virtual MoodleResource? ParentResource { get; set; }
}
