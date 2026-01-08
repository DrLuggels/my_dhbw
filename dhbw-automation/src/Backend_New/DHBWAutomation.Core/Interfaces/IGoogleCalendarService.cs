using DHBWAutomation.Core.Models;

namespace DHBWAutomation.Core.Interfaces;

public interface IGoogleCalendarService
{
    /// <summary>
    /// Erstellt eine Autorisierungs-URL für OAuth 2.0
    /// </summary>
    Task<string> GetAuthorizationUrlAsync(int userId);

    /// <summary>
    /// Handhabt den OAuth-Callback und speichert das Token
    /// </summary>
    Task<bool> HandleCallbackAsync(int userId, string code);

    /// <summary>
    /// Synchronisiert Events von Google Calendar in die lokale DB
    /// </summary>
    Task<int> SyncFromGoogleAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Synchronisiert lokale Events zu Google Calendar
    /// </summary>
    Task<int> SyncToGoogleAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Bidirektionale Synchronisation
    /// </summary>
    Task<(int imported, int exported)> SyncBidirectionalAsync(int userId);

    /// <summary>
    /// Erstellt ein Event in Google Calendar
    /// </summary>
    Task<string?> CreateEventAsync(int userId, CalendarEvent calendarEvent);

    /// <summary>
    /// Aktualisiert ein Event in Google Calendar
    /// </summary>
    Task<bool> UpdateEventAsync(int userId, CalendarEvent calendarEvent);

    /// <summary>
    /// Löscht ein Event in Google Calendar
    /// </summary>
    Task<bool> DeleteEventAsync(int userId, string googleEventId);

    /// <summary>
    /// Prüft ob der Benutzer mit Google Calendar verbunden ist
    /// </summary>
    Task<bool> IsConnectedAsync(int userId);
}
