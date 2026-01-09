using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

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
    /// Konfidenz-Score der KI-Analyse (0.0-1.0)
    /// </summary>
    public double? ConfidenceScore { get; set; }

    /// <summary>
    /// Extrahierte Termine aus dem E-Mail-Text
    /// </summary>
    public string? ExtractedDates { get; set; }

    /// <summary>
    /// RAW JSON für KI-Analyse-Ergebnisse
    /// </summary>
    public string? AnalysisResultJson { get; set; }

    // === Benutzer-Aktionen ===

    /// <summary>
    /// Status der Benutzer-Aktion: pending, accepted, declined, snoozed, archived
    /// </summary>
    [MaxLength(50)]
    public string ActionStatus { get; set; } = "pending";

    /// <summary>
    /// Zeitpunkt der Benutzer-Aktion
    /// </summary>
    public DateTime? ActionTakenAt { get; set; }

    /// <summary>
    /// Wenn ein Kalendereintrag aus dieser E-Mail erstellt wurde
    /// </summary>
    public int? RelatedCalendarEventId { get; set; }

    [ForeignKey("RelatedCalendarEventId")]
    public CalendarEvent? RelatedCalendarEvent { get; set; }

    /// <summary>
    /// Priorität: 1=high, 2=medium, 3=low
    /// </summary>
    public int Priority { get; set; } = 2;

    /// <summary>
    /// Extrahierte strukturierte Daten (JSON)
    /// </summary>
    public string? ExtractedData { get; set; }

    // === Navigation Properties ===

    public ICollection<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
}
