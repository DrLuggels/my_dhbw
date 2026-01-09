namespace DHBWAutomation.Backend.Core.DTOs.Responses;

public class EmailResponse
{
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToAddresses { get; set; } = string.Empty;
    public string BodyText { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsImportant { get; set; }
    public bool HasAttachments { get; set; }
    public string Folder { get; set; } = string.Empty;

    // KI-Analyse
    public bool IsProcessed { get; set; }
    public string? Summary { get; set; }
    public string? Category { get; set; }
    public bool IsAppointment { get; set; }
    public bool RequiresUserAction { get; set; }
    public string? SuggestedAction { get; set; }
    public int Priority { get; set; }
    public string? ExtractedData { get; set; }

    // Benutzer-Aktion
    public string ActionStatus { get; set; } = "pending";
    public int? RelatedCalendarEventId { get; set; }

    // Anhänge
    public List<EmailAttachmentResponse> Attachments { get; set; } = new();
}

public class EmailAttachmentResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsInline { get; set; }
    public int? RelatedDocumentId { get; set; }
}

public class EmailSummaryResponse
{
    public int TotalUnread { get; set; }
    public int PendingActions { get; set; }
    public int AppointmentsToday { get; set; }
    public List<EmailResponse> RecentEmails { get; set; } = new();
}
