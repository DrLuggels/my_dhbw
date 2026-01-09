namespace DHBWAutomation.Backend.Core.DTOs.Responses;

public class UserProfileResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MatriculationNumber { get; set; }
    public string? Course { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // E-Mail-Integration Settings
    public bool EmailSyncEnabled { get; set; }
    public string? EmailSyncAddress { get; set; }
    // WICHTIG: Passwort wird NIE zurückgegeben
    public string? EmailImapHost { get; set; }
    public int EmailImapPort { get; set; }
    public string? EmailSmtpHost { get; set; }
    public int EmailSmtpPort { get; set; }
    public int EmailSyncIntervalMinutes { get; set; }
    public DateTime? LastEmailSync { get; set; }
}
