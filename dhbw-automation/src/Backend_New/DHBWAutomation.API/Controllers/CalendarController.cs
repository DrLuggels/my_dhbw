using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Infrastructure.Database;
using DHBWAutomation.Core.Models;
using DHBWAutomation.Infrastructure.Services;
using DHBWAutomation.Core.Interfaces;

namespace DHBWAutomation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRaplaService _raplaService;
    private readonly IGoogleCalendarService? _googleCalendarService;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(
        AppDbContext context,
        IRaplaService raplaService,
        ILogger<CalendarController> logger,
        IGoogleCalendarService? googleCalendarService = null)
    {
        _context = context;
        _raplaService = raplaService;
        _googleCalendarService = googleCalendarService;
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

            // TEMPORARILY DISABLED
            return Ok(new
            {
                success = true,
                data = new { IsConnected = false, Message = "Test temporär deaktiviert" },
                message = "Test temporär deaktiviert",
                errors = (string[]?)null
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

    // ==================== Google Calendar Integration ====================

    /// <summary>
    /// Startet die Google Calendar Autorisierung
    /// </summary>
    [HttpGet("google/authorize/{userId}")]
    public async Task<IActionResult> AuthorizeGoogle(int userId)
    {
        try
        {
            if (_googleCalendarService == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Google Calendar Service ist nicht konfiguriert",
                    errors = new[] { "Service nicht verfügbar" }
                });
            }

            var authUrl = await _googleCalendarService.GetAuthorizationUrlAsync(userId);

            return Ok(new
            {
                success = true,
                data = new { authorizationUrl = authUrl },
                message = "Bitte öffne die URL zur Autorisierung",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei Google Authorization");
            return StatusCode(500, new
            {
                success = false,
                message = "Fehler bei Google Authorization",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Callback für Google OAuth
    /// </summary>
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            if (_googleCalendarService == null)
            {
                return BadRequest("Google Calendar Service nicht konfiguriert");
            }

            if (!int.TryParse(state, out int userId))
            {
                return BadRequest("Ungültige User ID");
            }

            var success = await _googleCalendarService.HandleCallbackAsync(userId, code);

            if (success)
            {
                return Redirect("/calendar?googleConnected=true");
            }

            return Redirect("/calendar?googleConnected=false");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Google Callback");
            return Redirect("/calendar?error=true");
        }
    }

    /// <summary>
    /// Synchronisiert Events von Google Calendar
    /// </summary>
    [HttpPost("google/sync-from/{userId}")]
    public async Task<IActionResult> SyncFromGoogle(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (_googleCalendarService == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Google Calendar Service ist nicht konfiguriert",
                    errors = new[] { "Service nicht verfügbar" }
                });
            }

            var syncedCount = await _googleCalendarService.SyncFromGoogleAsync(userId, startDate, endDate);

            return Ok(new
            {
                success = true,
                data = new { syncedEvents = syncedCount },
                message = $"{syncedCount} Events von Google Calendar importiert",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Google Calendar Import");
            return StatusCode(500, new
            {
                success = false,
                message = "Fehler beim Google Calendar Import",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Synchronisiert Events zu Google Calendar
    /// </summary>
    [HttpPost("google/sync-to/{userId}")]
    public async Task<IActionResult> SyncToGoogle(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (_googleCalendarService == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Google Calendar Service ist nicht konfiguriert",
                    errors = new[] { "Service nicht verfügbar" }
                });
            }

            var exportedCount = await _googleCalendarService.SyncToGoogleAsync(userId, startDate, endDate);

            return Ok(new
            {
                success = true,
                data = new { exportedEvents = exportedCount },
                message = $"{exportedCount} Events zu Google Calendar exportiert",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Google Calendar Export");
            return StatusCode(500, new
            {
                success = false,
                message = "Fehler beim Google Calendar Export",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Bidirektionale Synchronisation mit Google Calendar
    /// </summary>
    [HttpPost("google/sync-bidirectional/{userId}")]
    public async Task<IActionResult> SyncBidirectional(int userId)
    {
        try
        {
            if (_googleCalendarService == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Google Calendar Service ist nicht konfiguriert",
                    errors = new[] { "Service nicht verfügbar" }
                });
            }

            var (imported, exported) = await _googleCalendarService.SyncBidirectionalAsync(userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    importedEvents = imported,
                    exportedEvents = exported,
                    totalSynced = imported + exported
                },
                message = $"Sync abgeschlossen: {imported} importiert, {exported} exportiert",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei bidirektionaler Synchronisation");
            return StatusCode(500, new
            {
                success = false,
                message = "Fehler bei bidirektionaler Synchronisation",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Prüft den Google Calendar Verbindungsstatus
    /// </summary>
    [HttpGet("google/status/{userId}")]
    public async Task<IActionResult> GoogleConnectionStatus(int userId)
    {
        try
        {
            if (_googleCalendarService == null)
            {
                return Ok(new
                {
                    success = true,
                    data = new { isConnected = false, available = false },
                    message = "Google Calendar Service nicht konfiguriert",
                    errors = (string[]?)null
                });
            }

            var isConnected = await _googleCalendarService.IsConnectedAsync(userId);

            return Ok(new
            {
                success = true,
                data = new { isConnected, available = true },
                message = isConnected ? "Verbunden" : "Nicht verbunden",
                errors = (string[]?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Prüfen des Google Calendar Status");
            return StatusCode(500, new
            {
                success = false,
                message = "Fehler beim Prüfen des Status",
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
