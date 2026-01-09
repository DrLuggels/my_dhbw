using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

public class EmailAttachment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EmailId { get; set; }

    [ForeignKey("EmailId")]
    public Email Email { get; set; } = null!;

    /// <summary>
    /// Originaler Dateiname aus der E-Mail
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME-Type (application/pdf, image/png, etc.)
    /// </summary>
    [MaxLength(200)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Dateigröße in Bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Content-ID für inline Attachments (Bilder in HTML)
    /// </summary>
    [MaxLength(500)]
    public string? ContentId { get; set; }

    /// <summary>
    /// Ist dies ein inline Attachment?
    /// </summary>
    public bool IsInline { get; set; } = false;

    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;

    // === Verknüpfung zu Document-System ===

    /// <summary>
    /// Falls der Anhang automatisch als Document gespeichert wurde
    /// </summary>
    public int? RelatedDocumentId { get; set; }

    [ForeignKey("RelatedDocumentId")]
    public Document? RelatedDocument { get; set; }

    /// <summary>
    /// Wurde der Anhang verarbeitet?
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    public DateTime? ProcessedAt { get; set; }
}
