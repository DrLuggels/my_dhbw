using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DHBWAutomation.Core.Interfaces;
using DHBWAutomation.Core.Models;
using DHBWAutomation.Core.DTOs.Requests;
using DHBWAutomation.Core.DTOs.Responses;
using DHBWAutomation.Infrastructure.Database;
using System.Text.Json;

namespace DHBWAutomation.Infrastructure.Services;

public class MailService : IMailService
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private readonly IFileService _fileService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailService> _logger;

    public MailService(
        AppDbContext context,
        IAIService aiService,
        IFileService fileService,
        IConfiguration configuration,
        ILogger<MailService> logger)
    {
        _context = context;
        _aiService = aiService;
        _fileService = fileService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> SyncEmailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            throw new ArgumentException("User not found");

        var imapHost = _configuration["Email:ImapHost"] ?? "outlook.office365.com";
        var imapPort = int.Parse(_configuration["Email:ImapPort"] ?? "993");
        var username = GetEmailUsername(user.Email);
        var password = _configuration[$"Email:Password:{userId}"] ?? _configuration["Email:DefaultPassword"];

        if (string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("No email password configured for user {UserId}", userId);
            return 0;
        }

        var newEmailsCount = 0;

        using var client = new ImapClient();
        try
        {
            await client.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            // Hole nur ungelesene E-Mails der letzten 30 Tage
            var query = SearchQuery.NotSeen
                .And(SearchQuery.DeliveredAfter(DateTime.UtcNow.AddDays(-30)));

            var uids = await inbox.SearchAsync(query, cancellationToken);

            _logger.LogInformation("Found {Count} unread emails for user {UserId}", uids.Count, userId);

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var message = await inbox.GetMessageAsync(uid, cancellationToken);

                    // Prüfe, ob E-Mail bereits existiert (via MessageId)
                    var existingEmail = await _context.Emails
                        .FirstOrDefaultAsync(e => e.MessageId == message.MessageId && e.UserId == userId, cancellationToken);

                    if (existingEmail != null)
                        continue; // Bereits synchronisiert

                    var email = new Email
                    {
                        UserId = userId,
                        MessageId = message.MessageId ?? Guid.NewGuid().ToString(),
                        Subject = message.Subject ?? "(Kein Betreff)",
                        FromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? "",
                        FromName = message.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                        ToAddresses = string.Join(", ", message.To.Mailboxes.Select(m => m.Address)),
                        CcAddresses = string.Join(", ", message.Cc.Mailboxes.Select(m => m.Address)),
                        BodyText = message.TextBody ?? message.HtmlBody ?? "",
                        BodyHtml = message.HtmlBody,
                        ReceivedAt = message.Date.UtcDateTime,
                        FetchedAt = DateTime.UtcNow,
                        IsRead = false,
                        HasAttachments = message.Attachments.Any(),
                        Folder = "INBOX"
                    };

                    // Speichere Anhänge
                    foreach (var attachment in message.Attachments)
                    {
                        if (attachment is MimePart mimePart)
                        {
                            var emailAttachment = new EmailAttachment
                            {
                                FileName = mimePart.FileName ?? "unnamed",
                                ContentType = mimePart.ContentType.MimeType,
                                FileSize = mimePart.Content?.Stream?.Length ?? 0,
                                ContentId = mimePart.ContentId,
                                IsInline = mimePart.ContentDisposition?.Disposition == "inline"
                            };

                            email.Attachments.Add(emailAttachment);
                        }
                    }

                    _context.Emails.Add(email);
                    await _context.SaveChangesAsync(cancellationToken);

                    newEmailsCount++;

                    // Starte asynchrone Verarbeitung (wird von Background Worker gemacht)
                    _logger.LogInformation("Fetched email: {Subject} from {From}", email.Subject, email.FromAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching email UID {Uid}", uid);
                }
            }

            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing emails for user {UserId}", userId);
            throw;
        }

        return newEmailsCount;
    }

    public async Task<List<EmailResponse>> GetEmailsAsync(
        int userId,
        string? folder = null,
        bool? isRead = null,
        bool? requiresAction = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Emails
            .Where(e => e.UserId == userId)
            .Include(e => e.Attachments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(folder))
            query = query.Where(e => e.Folder == folder);

        if (isRead.HasValue)
            query = query.Where(e => e.IsRead == isRead.Value);

        if (requiresAction.HasValue)
            query = query.Where(e => e.RequiresUserAction == requiresAction.Value);

        var emails = await query
            .OrderByDescending(e => e.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return emails.Select(MapToResponse).ToList();
    }

    public async Task<EmailResponse?> GetEmailByIdAsync(int emailId, int userId, CancellationToken cancellationToken = default)
    {
        var email = await _context.Emails
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == emailId && e.UserId == userId, cancellationToken);

        return email == null ? null : MapToResponse(email);
    }

    public async Task<Email> ProcessEmailAsync(int emailId, CancellationToken cancellationToken = default)
    {
        var email = await _context.Emails
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == emailId, cancellationToken);

        if (email == null)
            throw new ArgumentException("Email not found");

        if (email.IsProcessed)
            return email; // Bereits verarbeitet

        try
        {
            // KI-Analyse des E-Mail-Inhalts
            var analysisPrompt = $@"Analysiere diese E-Mail und gib ein JSON zurück mit folgenden Feldern:
- category: (appointment|question|information|task|newsletter|spam)
- isAppointment: boolean
- requiresUserAction: boolean
- suggestedAction: (accept|decline|remind_later|archive|delete)
- priority: 1-3 (1=high, 2=medium, 3=low)
- summary: Eine kurze Zusammenfassung (max. 150 Zeichen) für Dashboard
- extractedData: JSON mit extrahierten Details wie Datum/Zeit/Ort für Termine

E-Mail:
Von: {email.FromAddress} ({email.FromName})
Betreff: {email.Subject}
Text: {email.BodyText.Substring(0, Math.Min(2000, email.BodyText.Length))}";

            var analysisResult = await _aiService.ChatCompletionAsync(analysisPrompt, cancellationToken);

            // Parse JSON-Antwort
            var analysis = JsonSerializer.Deserialize<EmailAnalysisResult>(analysisResult);

            if (analysis != null)
            {
                email.Category = analysis.Category;
                email.IsAppointment = analysis.IsAppointment;
                email.RequiresUserAction = analysis.RequiresUserAction;
                email.SuggestedAction = analysis.SuggestedAction;
                email.Priority = analysis.Priority;
                email.Summary = analysis.Summary;
                email.ExtractedData = analysis.ExtractedData != null 
                    ? JsonSerializer.Serialize(analysis.ExtractedData) 
                    : null;
            }

            email.IsProcessed = true;
            email.ProcessedAt = DateTime.UtcNow;

            // Automatisch Termin erstellen, wenn eindeutig identifiziert
            if (email.IsAppointment && !email.RequiresUserAction && email.ExtractedData != null)
            {
                try
                {
                    var calendarEvent = await CreateCalendarEventFromEmailAsync(emailId, cancellationToken);
                    if (calendarEvent != null)
                    {
                        email.RelatedCalendarEventId = calendarEvent.Id;
                        _logger.LogInformation("Auto-created calendar event {EventId} from email {EmailId}", 
                            calendarEvent.Id, emailId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to auto-create calendar event for email {EmailId}", emailId);
                }
            }

            // Lade Anhänge herunter
            if (email.HasAttachments && email.Attachments.Any())
            {
                _ = Task.Run(async () => await ProcessAttachmentsAsync(emailId, cancellationToken), cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processed email {EmailId}: {Category}, Action: {Action}", 
                emailId, email.Category, email.SuggestedAction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email {EmailId}", emailId);
            email.IsProcessed = true; // Markiere als verarbeitet, um Endlosschleifen zu vermeiden
            email.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return email;
    }

    public async Task<EmailResponse> ExecuteActionAsync(
        int userId,
        EmailActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = await _context.Emails
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == request.EmailId && e.UserId == userId, cancellationToken);

        if (email == null)
            throw new ArgumentException("Email not found");

        email.ActionStatus = request.Action switch
        {
            "accept" => "accepted",
            "decline" => "declined",
            "snooze" => "snoozed",
            "archive" => "archived",
            "mark_read" => email.ActionStatus, // Behält aktuellen Status
            _ => "archived"
        };

        email.ActionTakenAt = DateTime.UtcNow;

        if (request.Action == "mark_read")
        {
            email.IsRead = true;
        }

        // Bei Accept: Erstelle Kalendereintrag
        if (request.Action == "accept" && request.CreateCalendarEvent && email.IsAppointment)
        {
            var calendarEvent = await CreateCalendarEventFromEmailAsync(request.EmailId, cancellationToken);
            if (calendarEvent != null)
            {
                email.RelatedCalendarEventId = calendarEvent.Id;
            }
        }

        // Bei Snooze: Erstelle Reminder
        if (request.Action == "snooze" && request.SnoozeUntil.HasValue)
        {
            var reminder = new Reminder
            {
                UserId = userId,
                Title = $"Erinnerung: {email.Subject}",
                Description = email.Summary ?? email.BodyText.Substring(0, Math.Min(200, email.BodyText.Length)),
                DueDate = request.SnoozeUntil.Value,
                Priority = email.Priority,
                Status = "pending",
                IsRecurring = false
            };

            _context.Reminders.Add(reminder);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(email);
    }

    public async Task<List<int>> ProcessAttachmentsAsync(int emailId, CancellationToken cancellationToken = default)
    {
        var email = await _context.Emails
            .Include(e => e.Attachments)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == emailId, cancellationToken);

        if (email == null || !email.HasAttachments)
            return new List<int>();

        var documentIds = new List<int>();

        // Hole Original-MimeMessage erneut vom Server, um Anhänge zu downloaden
        var imapHost = _configuration["Email:ImapHost"] ?? "outlook.office365.com";
        var imapPort = int.Parse(_configuration["Email:ImapPort"] ?? "993");
        var username = GetEmailUsername(email.User.Email);
        var password = _configuration[$"Email:Password:{email.UserId}"] ?? _configuration["Email:DefaultPassword"];

        if (string.IsNullOrEmpty(password))
            return documentIds;

        using var client = new ImapClient();
        try
        {
            await client.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            // Suche E-Mail via MessageId
            var query = SearchQuery.HeaderContains("Message-Id", email.MessageId);
            var uids = await inbox.SearchAsync(query, cancellationToken);

            if (uids.Count > 0)
            {
                var message = await inbox.GetMessageAsync(uids[0], cancellationToken);

                foreach (var attachment in message.Attachments.OfType<MimePart>())
                {
                    if (attachment.IsAttachment && !string.IsNullOrEmpty(attachment.FileName))
                    {
                        // Speichere als Document via FileService
                        using var memoryStream = new MemoryStream();
                        await attachment.Content.DecodeToAsync(memoryStream, cancellationToken);
                        memoryStream.Position = 0;

                        // Erstelle IFormFile-Mock für FileService
                        var formFile = new FormFileAdapter(
                            memoryStream,
                            attachment.FileName,
                            attachment.ContentType.MimeType);

                        var document = await _fileService.UploadFileAsync(
                            email.UserId,
                            formFile,
                            "email_attachments", // Kategorie
                            cancellationToken);

                        // Verlinke mit EmailAttachment
                        var emailAttachment = email.Attachments
                            .FirstOrDefault(a => a.FileName == attachment.FileName);

                        if (emailAttachment != null)
                        {
                            emailAttachment.RelatedDocumentId = document.Id;
                            emailAttachment.IsProcessed = true;
                            emailAttachment.ProcessedAt = DateTime.UtcNow;
                        }

                        documentIds.Add(document.Id);

                        _logger.LogInformation("Processed attachment {FileName} for email {EmailId} -> Document {DocumentId}",
                            attachment.FileName, emailId, document.Id);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing attachments for email {EmailId}", emailId);
        }

        return documentIds;
    }

    public async Task<CalendarEvent?> CreateCalendarEventFromEmailAsync(
        int emailId,
        CancellationToken cancellationToken = default)
    {
        var email = await _context.Emails.FindAsync(new object[] { emailId }, cancellationToken);

        if (email == null || !email.IsAppointment || string.IsNullOrEmpty(email.ExtractedData))
            return null;

        try
        {
            var extractedData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(email.ExtractedData);

            if (extractedData == null)
                return null;

            var calendarEvent = new CalendarEvent
            {
                UserId = email.UserId,
                Title = extractedData.TryGetValue("title", out var title) 
                    ? title.GetString() ?? email.Subject 
                    : email.Subject,
                Description = extractedData.TryGetValue("description", out var desc) 
                    ? desc.GetString() 
                    : email.Summary,
                Location = extractedData.TryGetValue("location", out var loc) 
                    ? loc.GetString() 
                    : null,
                StartTime = extractedData.TryGetValue("startTime", out var start) 
                    ? start.GetDateTime() 
                    : DateTime.UtcNow,
                EndTime = extractedData.TryGetValue("endTime", out var end) 
                    ? end.GetDateTime() 
                    : DateTime.UtcNow.AddHours(1),
                IsAllDay = extractedData.TryGetValue("isAllDay", out var allDay) && allDay.GetBoolean(),
                EventType = "meeting",
                Source = "email",
                ExternalId = email.MessageId,
                Notes = $"Automatisch erstellt aus E-Mail von {email.FromAddress}",
                LastSyncedAt = DateTime.UtcNow
            };

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync(cancellationToken);

            return calendarEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar event from email {EmailId}", emailId);
            return null;
        }
    }

    public async Task<EmailSummaryResponse> GetSummaryAsync(int userId, CancellationToken cancellationToken = default)
    {
        var unreadCount = await _context.Emails
            .CountAsync(e => e.UserId == userId && !e.IsRead, cancellationToken);

        var pendingActionsCount = await _context.Emails
            .CountAsync(e => e.UserId == userId && e.RequiresUserAction && e.ActionStatus == "pending", cancellationToken);

        var appointmentsTodayCount = await _context.Emails
            .Where(e => e.UserId == userId && e.IsAppointment && e.ReceivedAt.Date == DateTime.UtcNow.Date)
            .CountAsync(cancellationToken);

        var recentEmails = await _context.Emails
            .Where(e => e.UserId == userId)
            .Include(e => e.Attachments)
            .OrderByDescending(e => e.ReceivedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new EmailSummaryResponse
        {
            TotalUnread = unreadCount,
            PendingActions = pendingActionsCount,
            AppointmentsToday = appointmentsTodayCount,
            RecentEmails = recentEmails.Select(MapToResponse).ToList()
        };
    }

    public async Task MarkAsReadAsync(int emailId, bool isRead, CancellationToken cancellationToken = default)
    {
        var email = await _context.Emails.FindAsync(new object[] { emailId }, cancellationToken);
        if (email != null)
        {
            email.IsRead = isRead;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteEmailAsync(int emailId, bool deleteFromServer = false, CancellationToken cancellationToken = default)
    {
        var email = await _context.Emails.FindAsync(new object[] { emailId }, cancellationToken);
        if (email != null)
        {
            _context.Emails.Remove(email);
            await _context.SaveChangesAsync(cancellationToken);

            // TODO: Implementiere Server-Löschen via IMAP wenn deleteFromServer=true
        }
    }

    // === Helper Methods ===

    private string GetEmailUsername(string email)
    {
        // DHBW-spezifische Username-Konvertierung
        // Cvitanovic.Luka-25@stud.dhbw-ravensburg.de -> Cvitanovic.Luka-25
        if (email.Contains("@"))
        {
            return email.Split('@')[0];
        }
        return email;
    }

    private EmailResponse MapToResponse(Email email)
    {
        return new EmailResponse
        {
            Id = email.Id,
            MessageId = email.MessageId,
            Subject = email.Subject,
            FromAddress = email.FromAddress,
            FromName = email.FromName,
            ToAddresses = email.ToAddresses,
            BodyText = email.BodyText,
            BodyHtml = email.BodyHtml,
            ReceivedAt = email.ReceivedAt,
            IsRead = email.IsRead,
            IsImportant = email.IsImportant,
            HasAttachments = email.HasAttachments,
            Folder = email.Folder,
            IsProcessed = email.IsProcessed,
            Summary = email.Summary,
            Category = email.Category,
            IsAppointment = email.IsAppointment,
            RequiresUserAction = email.RequiresUserAction,
            SuggestedAction = email.SuggestedAction,
            Priority = email.Priority,
            ExtractedData = email.ExtractedData,
            ActionStatus = email.ActionStatus,
            RelatedCalendarEventId = email.RelatedCalendarEventId,
            Attachments = email.Attachments.Select(a => new EmailAttachmentResponse
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                IsInline = a.IsInline,
                RelatedDocumentId = a.RelatedDocumentId
            }).ToList()
        };
    }

    // Helper für JSON-Parsing
    private class EmailAnalysisResult
    {
        public string Category { get; set; } = "information";
        public bool IsAppointment { get; set; }
        public bool RequiresUserAction { get; set; }
        public string SuggestedAction { get; set; } = "archive";
        public int Priority { get; set; } = 2;
        public string Summary { get; set; } = "";
        public Dictionary<string, object>? ExtractedData { get; set; }
    }
}

// Helper-Klasse für IFormFile-Mocking
public class FormFileAdapter : Microsoft.AspNetCore.Http.IFormFile
{
    private readonly Stream _stream;
    private readonly string _fileName;
    private readonly string _contentType;

    public FormFileAdapter(Stream stream, string fileName, string contentType)
    {
        _stream = stream;
        _fileName = fileName;
        _contentType = contentType;
    }

    public string ContentType => _contentType;
    public string ContentDisposition => $"attachment; filename=\"{_fileName}\"";
    public Microsoft.AspNetCore.Http.IHeaderDictionary Headers => new Microsoft.AspNetCore.Http.HeaderDictionary();
    public long Length => _stream.Length;
    public string Name => _fileName;
    public string FileName => _fileName;

    public void CopyTo(Stream target) => _stream.CopyTo(target);
    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) 
        => _stream.CopyToAsync(target, cancellationToken);
    public Stream OpenReadStream() => _stream;
}
