using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks files synced from Nextcloud
/// </summary>
[Table("nextcloud_files")]
public class NextcloudFile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CredentialId { get; set; }

    /// <summary>
    /// Full remote path in Nextcloud (e.g., /Documents/Studium/Java/Skript.pdf)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string RemotePath { get; set; } = string.Empty;

    /// <summary>
    /// File name only
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File extension/type (e.g., pdf, docx)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// ETag for change detection (from WebDAV)
    /// </summary>
    [MaxLength(64)]
    public string? ETag { get; set; }

    /// <summary>
    /// Last modified date on Nextcloud server
    /// </summary>
    public DateTime RemoteModifiedAt { get; set; }

    /// <summary>
    /// When the file was last synced locally
    /// </summary>
    public DateTime? LocalSyncedAt { get; set; }

    /// <summary>
    /// Link to local document (after download and processing)
    /// </summary>
    public int? LocalDocumentId { get; set; }

    /// <summary>
    /// Whether this file has been downloaded
    /// </summary>
    public bool IsDownloaded { get; set; } = false;

    /// <summary>
    /// Whether this file has been processed by AI
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CredentialId")]
    public virtual NextcloudCredential Credential { get; set; } = null!;

    [ForeignKey("LocalDocumentId")]
    public virtual Document? LocalDocument { get; set; }
}
