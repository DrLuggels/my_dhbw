using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Core.Services;

namespace DHBWAutomation.Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRaplaService _raplaService;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(
        AppDbContext context,
        IRaplaService raplaService,
        ILogger<CalendarController> logger)
    {
        _context = context;
        _raplaService = raplaService;
        _logger = logger;
    }

    /// <summary>
    /// Holt alle Events eines Benutzers
    /// </summary>
    [HttpGet("events/{userId}")]
    public async Task<IActionResult> GetUserEvents(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? source = null)
    {
        try
        {
            var query = _context.CalendarEvents.Where(e => e.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(e => e.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.EndTime <= endDate.Value);

            if (!string.IsNullOrEmpty(source))
                query = query.Where(e => e.Source == source);

            var events = await Task.FromResult(query.ToList());

            return Ok(new
            {
                success = true,
                data = events,
                message = $"{events.Count} Events gefunden",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der Calendar Events");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Fehler beim Abrufen der Events",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Synchronisiert den Rapla-Kalender
    /// </summary>
    [HttpPost("sync-rapla/{userId}")]
    public async Task<IActionResult> SyncRaplaCalendar(int userId)
    {
        try
        {
            _logger.LogInformation($"Rapla-Sync für User {userId} angefordert");

            var syncedCount = await _raplaService.SyncCalendarAsync(userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    syncedEvents = syncedCount,
                    message = $"Successfully synced {syncedCount} events from Rapla"
                },
                message = $"Rapla-Sync erfolgreich. {syncedCount} Events synchronisiert.",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Rapla-Sync");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Fehler beim Rapla-Sync",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Testet die Rapla-Verbindung
    /// </summary>
    [HttpGet("test-rapla")]
    public async Task<IActionResult> TestRaplaConnection()
    {
        try
        {
            _logger.LogInformation("Rapla-Verbindungstest angefordert");

            var testResult = await _raplaService.TestConnectionAsync();

            return Ok(new
            {
                success = testResult.IsConnected,
                data = testResult,
                message = testResult.Message,
                errors = testResult.IsConnected ? null : new[] { testResult.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Rapla-Test");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Fehler beim Rapla-Test",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Holt den Wochenplan
    /// </summary>
    [HttpGet("week-schedule")]
    public async Task<IActionResult> GetWeekSchedule([FromQuery] DateTime? weekStart = null)
    {
        try
        {
            var startOfWeek = weekStart ?? DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);

            var events = await Task.FromResult(
                _context.CalendarEvents
                    .Where(e => e.StartTime >= startOfWeek && e.StartTime < endOfWeek)
                    .OrderBy(e => e.StartTime)
                    .ToList()
            );

            return Ok(new
            {
                success = true,
                data = new
                {
                    weekStart = startOfWeek,
                    weekEnd = endOfWeek,
                    events
                },
                message = $"{events.Count} Events für die Woche gefunden",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen des Wochenplans");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Fehler beim Abrufen des Wochenplans",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Erstellt ein neues Event
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            var calendarEvent = new CalendarEvent
            {
                UserId = request.UserId,
                Title = request.Title,
                Description = request.Description,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Location = request.Location,
                Source = request.Source ?? "manual",
                CreatedAt = DateTime.UtcNow
            };

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = calendarEvent,
                message = "Event erfolgreich erstellt",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erstellen des Events");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Fehler beim Erstellen des Events",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Löscht ein Event
    /// </summary>
    [HttpDelete("events/{eventId}")]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        try
        {
            var calendarEvent = await Task.FromResult(_context.CalendarEvents.FirstOrDefault(e => e.Id == eventId));

            if (calendarEvent == null)
            {
                return NotFound(new
                {
                    success = false,
                    data = (object?)null,
                    message = "Event nicht gefunden",
                    errors = new[] { "Event existiert nicht" }
                });
            }

            _context.CalendarEvents.Remove(calendarEvent);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = (object?)null,
                message = "Event erfolgreich gelöscht",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen des Events");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Fehler beim Löschen des Events",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Aktualisiert die Notizen eines Events
    /// </summary>
    [HttpPatch("{eventId}/notes")]
    public async Task<IActionResult> UpdateEventNotes(int eventId, [FromBody] UpdateNotesRequest request)
    {
        try
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(eventId);

            if (calendarEvent == null)
            {
                return NotFound(new
                {
                    success = false,
                    data = (object?)null,
                    message = "Event nicht gefunden",
                    errors = new[] { "Event existiert nicht" }
                });
            }

            calendarEvent.Notes = request.Notes;
            calendarEvent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = calendarEvent,
                message = "Notizen erfolgreich aktualisiert",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren der Notizen");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Fehler beim Aktualisieren der Notizen",
                errors = new[] { ex.Message }
            });
        }
    }
}

// Request DTOs
public class CreateEventRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public string? Source { get; set; }
}

public class UpdateNotesRequest
{
    public string? Notes { get; set; }
}
