using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Rapla;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services;

public interface IRaplaService
{
    Task<int> SyncCalendarAsync(int userId);
    Task<RaplaConnectionTestResult> TestConnectionAsync();
}

public class RaplaService : IRaplaService
{
    private readonly RaplaClient _raplaClient;
    private readonly AppDbContext _context;
    private readonly ILogger<RaplaService> _logger;

    public RaplaService(
        RaplaClient raplaClient,
        AppDbContext context,
        ILogger<RaplaService> logger)
    {
        _raplaClient = raplaClient;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Synchronisiert den Rapla-Kalender für einen Benutzer
    /// Holt alle Events und speichert/aktualisiert sie in der Datenbank
    /// </summary>
    public async Task<int> SyncCalendarAsync(int userId)
    {
        try
        {
            _logger.LogInformation($"Starting Rapla calendar sync for user {userId}");

            // Hole Events vom Rapla-Server
            var fetchedEvents = await _raplaClient.FetchEventsAsync(userId);

            if (fetchedEvents == null || !fetchedEvents.Any())
            {
                _logger.LogWarning($"No events fetched from Rapla for user {userId}");
                return 0;
            }

            int syncedCount = 0;

            foreach (var fetchedEvent in fetchedEvents)
            {
                // Prüfe ob Event bereits existiert (via ExternalId)
                var existingEvent = await _context.CalendarEvents
                    .FirstOrDefaultAsync(e =>
                        e.UserId == userId &&
                        e.Source == "rapla" &&
                        e.ExternalId == fetchedEvent.ExternalId);

                if (existingEvent != null)
                {
                    // Event existiert - update nur wenn sich Daten geändert haben
                    if (HasEventChanged(existingEvent, fetchedEvent))
                    {
                        existingEvent.Title = fetchedEvent.Title;
                        existingEvent.Description = fetchedEvent.Description;
                        existingEvent.StartTime = fetchedEvent.StartTime;
                        existingEvent.EndTime = fetchedEvent.EndTime;
                        existingEvent.Location = fetchedEvent.Location;
                        existingEvent.Subject = fetchedEvent.Subject;
                        existingEvent.Professor = fetchedEvent.Professor;
                        existingEvent.EventType = fetchedEvent.EventType;
                        existingEvent.IsAllDay = fetchedEvent.IsAllDay;
                        existingEvent.LastSyncedAt = DateTime.UtcNow;
                        existingEvent.UpdatedAt = DateTime.UtcNow;

                        _logger.LogDebug($"Updated existing event: {fetchedEvent.Title}");
                    }
                }
                else
                {
                    // Neues Event - hinzufügen
                    _context.CalendarEvents.Add(fetchedEvent);
                    syncedCount++;
                    _logger.LogDebug($"Added new event: {fetchedEvent.Title}");
                }
            }

            // Lösche alte Rapla-Events, die nicht mehr im Kalender sind
            var fetchedExternalIds = fetchedEvents.Select(e => e.ExternalId).ToList();
            var eventsToDelete = await _context.CalendarEvents
                .Where(e =>
                    e.UserId == userId &&
                    e.Source == "rapla" &&
                    !fetchedExternalIds.Contains(e.ExternalId))
                .ToListAsync();

            if (eventsToDelete.Any())
            {
                _context.CalendarEvents.RemoveRange(eventsToDelete);
                _logger.LogInformation($"Removed {eventsToDelete.Count} obsolete events");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Rapla sync completed for user {userId}. " +
                                 $"Added/Updated: {syncedCount}, Removed: {eventsToDelete.Count}");

            return syncedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing Rapla calendar for user {userId}");
            throw;
        }
    }

    /// <summary>
    /// Prüft ob sich ein Event geändert hat
    /// </summary>
    private bool HasEventChanged(CalendarEvent existing, CalendarEvent fetched)
    {
        return existing.Title != fetched.Title ||
               existing.Description != fetched.Description ||
               existing.StartTime != fetched.StartTime ||
               existing.EndTime != fetched.EndTime ||
               existing.Location != fetched.Location ||
               existing.Subject != fetched.Subject ||
               existing.Professor != fetched.Professor ||
               existing.EventType != fetched.EventType ||
               existing.IsAllDay != fetched.IsAllDay;
    }

    /// <summary>
    /// Testet die Verbindung zum Rapla-Server
    /// </summary>
    public async Task<RaplaConnectionTestResult> TestConnectionAsync()
    {
        return await _raplaClient.TestConnectionAsync();
    }
}
