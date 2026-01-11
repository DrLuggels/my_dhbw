using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Stores encrypted Nextcloud credentials for a user
/// </summary>
[Table("nextcloud_credentials")]
public class NextcloudCredential
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Nextcloud server URL (e.g., https://nextcloud.dhbw-ravensburg.de)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string NextcloudUrl { get; set; } = "https://nextcloud.dhbw-ravensburg.de";

    /// <summary>
    /// Nextcloud username
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted password (use IEncryptionService)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string EncryptedPassword { get; set; } = string.Empty;

    /// <summary>
    /// Whether sync is currently enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sync interval in minutes
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Folders to sync (JSON array, null = sync everything)
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? SyncFolders { get; set; }

    /// <summary>
    /// Last successful sync timestamp
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Last sync error message (if any)
    /// </summary>
    [MaxLength(1000)]
    public string? LastSyncError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<NextcloudFile> Files { get; set; } = new List<NextcloudFile>();
}
