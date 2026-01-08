using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Core.Models;

public class Email
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    /// <summary>
    /// Eindeutige Message-ID vom E-Mail-Server (für Duplikatserkennung)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string MessageId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FromAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Komma-separierte Liste von Empfängern
    /// </summary>
    public string ToAddresses { get; set; } = string.Empty;

    public string CcAddresses { get; set; } = string.Empty;

    /// <summary>
    /// Plain-Text Body
    /// </summary>
    public string BodyText { get; set; } = string.Empty;

    /// <summary>
    /// HTML Body (optional)
    /// </summary>
    public string? BodyHtml { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;

    public bool IsImportant { get; set; } = false;

    public bool HasAttachments { get; set; } = false;

    /// <summary>
    /// IMAP Folder (INBOX, Sent, Draft, etc.)
    /// </summary>
    [MaxLength(100)]
    public string Folder { get; set; } = "INBOX";

    // === KI-Analyse Ergebnisse ===

    public bool IsProcessed { get; set; } = false;

    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// KI-generierte Zusammenfassung für Dashboard
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Kategorisierung: appointment, question, information, task, newsletter, spam
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Ist dies ein Termin?
    /// </summary>
    public bool IsAppointment { get; set; } = false;

    /// <summary>
    /// Benötigt Benutzer-Aktion?
    /// </summary>
    public bool RequiresUserAction { get; set; } = false;

    /// <summary>
    /// Vorgeschlagene Aktion: accept, decline, remind_later, archive, delete
    /// </summary>
    [MaxLength(50)]
    public string? SuggestedAction { get; set; }

    /// <summary>
    /// Priorität (1=High, 2=Medium, 3=Low)
    /// </summary>
    public int Priority { get; set; } = 2;

    /// <summary>
    /// JSON mit extrahierten Daten (Termin-Details, Kontakte, etc.)
    /// </summary>
    public string? ExtractedData { get; set; }

    // === Verknüpfungen ===

    /// <summary>
    /// Falls automatisch ein Kalendereintrag erstellt wurde
    /// </summary>
    public int? RelatedCalendarEventId { get; set; }

    [ForeignKey("RelatedCalendarEventId")]
    public CalendarEvent? RelatedCalendarEvent { get; set; }

    /// <summary>
    /// Benutzer-Aktionsstatus: pending, accepted, declined, snoozed, archived
    /// </summary>
    [MaxLength(50)]
    public string ActionStatus { get; set; } = "pending";

    public DateTime? ActionTakenAt { get; set; }

    // Navigation Properties
    public ICollection<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
}
