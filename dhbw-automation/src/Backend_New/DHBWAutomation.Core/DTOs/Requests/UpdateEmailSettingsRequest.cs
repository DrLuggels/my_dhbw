using System.ComponentModel.DataAnnotations;

namespace DHBWAutomation.Core.DTOs.Requests;

public class UpdateEmailSettingsRequest
{
    /// <summary>
    /// E-Mail-Synchronisation aktivieren/deaktivieren
    /// </summary>
    public bool EmailSyncEnabled { get; set; }

    /// <summary>
    /// DHBW E-Mail-Adresse
    /// </summary>
    [EmailAddress]
    [MaxLength(200)]
    public string? EmailSyncAddress { get; set; }

    /// <summary>
    /// E-Mail-Passwort (wird verschlüsselt gespeichert)
    /// </summary>
    [MaxLength(200)]
    public string? EmailSyncPassword { get; set; }

    /// <summary>
    /// IMAP Host (optional, Standard: outlook.office365.com)
    /// </summary>
    [MaxLength(200)]
    public string? EmailImapHost { get; set; }

    /// <summary>
    /// IMAP Port (optional, Standard: 993)
    /// </summary>
    public int? EmailImapPort { get; set; }

    /// <summary>
    /// SMTP Host (optional, Standard: smtp.office365.com)
    /// </summary>
    [MaxLength(200)]
    public string? EmailSmtpHost { get; set; }

    /// <summary>
    /// SMTP Port (optional, Standard: 587)
    /// </summary>
    public int? EmailSmtpPort { get; set; }

    /// <summary>
    /// Sync-Intervall in Minuten (optional, Standard: 1)
    /// </summary>
    [Range(1, 60)]
    public int? EmailSyncIntervalMinutes { get; set; }
}
