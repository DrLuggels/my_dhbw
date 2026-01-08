using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(AppDbContext context, ILogger<CalendarController> logger)
    {
        _context = context;
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
            // TODO: Implementiere Rapla-Sync
            _logger.LogInformation($"Rapla-Sync für User {userId} angefordert");

            await Task.Delay(100); // Simuliere async operation

            return Ok(new
            {
                success = true,
                data = new
                {
                    syncedEvents = 0,
                    message = "Rapla-Synchronisation noch nicht implementiert"
                },
                message = "Rapla-Sync wird später implementiert",
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
            // TODO: Implementiere Rapla-Verbindungstest
            _logger.LogInformation("Rapla-Verbindungstest angefordert");

            await Task.Delay(100); // Simuliere async operation

            var raplaUrl = Environment.GetEnvironmentVariable("RAPLA_BASE_URL")
                          ?? "https://rapla-ravensburg.dhbw.de/rapla";

            return Ok(new
            {
                success = true,
                data = new
                {
                    connected = false,
                    url = raplaUrl,
                    message = "Rapla-Integration noch nicht vollständig implementiert"
                },
                message = "Rapla-Test erfolgreich (Stub)",
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
