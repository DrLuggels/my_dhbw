using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? MatriculationNumber { get; set; }

    [MaxLength(100)]
    public string? Course { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // === E-Mail-Integration Settings (vom User konfigurierbar) ===
    
    /// <summary>
    /// E-Mail-Sync aktiviert?
    /// </summary>
    public bool EmailSyncEnabled { get; set; } = false;

    /// <summary>
    /// DHBW E-Mail-Adresse (z.B. Cvitanovic.Luka-25@stud.dhbw-ravensburg.de)
    /// </summary>
    [MaxLength(200)]
    public string? EmailSyncAddress { get; set; }

    /// <summary>
    /// Verschlüsseltes E-Mail-Passwort (wird beim Speichern verschlüsselt)
    /// </summary>
    [MaxLength(500)]
    public string? EmailSyncPassword { get; set; }

    /// <summary>
    /// IMAP Host (Standard: outlook.office365.com)
    /// </summary>
    [MaxLength(200)]
    public string? EmailImapHost { get; set; }

    /// <summary>
    /// IMAP Port (Standard: 993)
    /// </summary>
    public int EmailImapPort { get; set; } = 993;

    /// <summary>
    /// SMTP Host (Standard: smtp.office365.com)
    /// </summary>
    [MaxLength(200)]
    public string? EmailSmtpHost { get; set; }

    /// <summary>
    /// SMTP Port (Standard: 587)
    /// </summary>
    public int EmailSmtpPort { get; set; } = 587;

    /// <summary>
    /// Sync-Intervall in Minuten (Standard: 1)
    /// </summary>
    public int EmailSyncIntervalMinutes { get; set; } = 1;

    /// <summary>
    /// Letzte erfolgreiche E-Mail-Synchronisation
    /// </summary>
    public DateTime? LastEmailSync { get; set; }

    // === AI API Keys (benutzerspezifisch, optional) ===
    
    /// <summary>
    /// OpenAI API Key (verschlüsselt)
    /// </summary>
    [MaxLength(500)]
    public string? OpenAiApiKey { get; set; }

    /// <summary>
    /// Anthropic API Key (verschlüsselt)
    /// </summary>
    [MaxLength(500)]
    public string? AnthropicApiKey { get; set; }

    /// <summary>
    /// Google Gemini API Key (verschlüsselt)
    /// </summary>
    [MaxLength(500)]
    public string? GeminiApiKey { get; set; }

    // Navigation Properties
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public virtual ICollection<Email> Emails { get; set; } = new List<Email>();
}
