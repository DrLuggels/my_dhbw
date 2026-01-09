using DHBWAutomation.Backend.Core.DTOs.Requests;
using DHBWAutomation.Backend.Core.DTOs.Responses;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IMailService
{
    /// <summary>
    /// Synchronisiert E-Mails vom Server (IMAP) für einen Benutzer
    /// </summary>
    Task<int> SyncEmailsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ruft alle E-Mails eines Benutzers ab (mit Filterung)
    /// </summary>
    Task<List<EmailResponse>> GetEmailsAsync(
        int userId,
        string? folder = null,
        bool? isRead = null,
        bool? requiresAction = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ruft eine einzelne E-Mail ab
    /// </summary>
    Task<EmailResponse?> GetEmailByIdAsync(int emailId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verarbeitet eine E-Mail mit KI-Analyse
    /// </summary>
    Task<Email> ProcessEmailAsync(int emailId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Führt Benutzer-Aktion auf E-Mail aus (accept, decline, snooze, etc.)
    /// </summary>
    Task<EmailResponse> ExecuteActionAsync(
        int userId,
        EmailActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lädt Anhänge herunter und speichert sie im Document-System
    /// </summary>
    Task<List<int>> ProcessAttachmentsAsync(int emailId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Erstellt automatisch einen Kalendereintrag aus einer E-Mail
    /// </summary>
    Task<CalendarEvent?> CreateCalendarEventFromEmailAsync(
        int emailId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dashboard-Zusammenfassung für E-Mails
    /// </summary>
    Task<EmailSummaryResponse> GetSummaryAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Markiert E-Mail als gelesen/ungelesen
    /// </summary>
    Task MarkAsReadAsync(int emailId, bool isRead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht E-Mail (optional auch vom Server)
    /// </summary>
    Task DeleteEmailAsync(int emailId, bool deleteFromServer = false, CancellationToken cancellationToken = default);
}
