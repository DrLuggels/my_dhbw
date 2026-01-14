using DHBWAutomation.Backend.Core.DTOs.Requests;
using DHBWAutomation.Backend.Core.DTOs.Responses;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.Mail;

public partial class MailService
{
    public Task<int> SyncEmailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.SyncEmailsAsync is not yet implemented");
        throw new NotImplementedException("Email sync functionality is not yet implemented");
    }

    public Task<List<EmailResponse>> GetEmailsAsync(
        int userId,
        string? folder = null,
        bool? isRead = null,
        bool? requiresAction = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.GetEmailsAsync is not yet implemented");
        throw new NotImplementedException("Get emails functionality is not yet implemented");
    }

    public Task<EmailResponse?> GetEmailByIdAsync(int emailId, int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.GetEmailByIdAsync is not yet implemented");
        throw new NotImplementedException("Get email by ID functionality is not yet implemented");
    }

    public Task<Email> ProcessEmailAsync(int emailId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.ProcessEmailAsync is not yet implemented");
        throw new NotImplementedException("Process email functionality is not yet implemented");
    }

    public Task<EmailResponse> ExecuteActionAsync(
        int userId,
        EmailActionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.ExecuteActionAsync is not yet implemented");
        throw new NotImplementedException("Execute email action functionality is not yet implemented");
    }

    public Task<List<int>> ProcessAttachmentsAsync(int emailId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.ProcessAttachmentsAsync is not yet implemented");
        throw new NotImplementedException("Process attachments functionality is not yet implemented");
    }

    public Task<CalendarEvent?> CreateCalendarEventFromEmailAsync(
        int emailId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.CreateCalendarEventFromEmailAsync is not yet implemented");
        throw new NotImplementedException("Create calendar event from email functionality is not yet implemented");
    }

    public Task<EmailSummaryResponse> GetSummaryAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.GetSummaryAsync is not yet implemented");
        throw new NotImplementedException("Get email summary functionality is not yet implemented");
    }

    public Task MarkAsReadAsync(int emailId, bool isRead, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.MarkAsReadAsync is not yet implemented");
        throw new NotImplementedException("Mark as read functionality is not yet implemented");
    }

    public Task DeleteEmailAsync(int emailId, bool deleteFromServer = false, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("MailService.DeleteEmailAsync is not yet implemented");
        throw new NotImplementedException("Delete email functionality is not yet implemented");
    }
}
