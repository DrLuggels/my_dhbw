using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DHBWAutomation.Core.Interfaces;

namespace DHBWAutomation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly IRaplaService _raplaService;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(
        IRaplaService raplaService,
        ILogger<CalendarController> logger)
    {
        _raplaService = raplaService;
        _logger = logger;
    }

    /// <summary>
    /// Synchronisiert den Rapla-Kalender für einen Benutzer
    /// </summary>
    [HttpPost("sync-rapla/{userId}")]
    public async Task<IActionResult> SyncRaplaCalendar(int userId)
    {
        try
        {
            _logger.LogInformation("Received Rapla sync request for user {UserId}", userId);

            var success = await _raplaService.SyncCalendarAsync(userId);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "Rapla-Kalender erfolgreich synchronisiert"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Fehler beim Synchronisieren des Rapla-Kalenders"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Rapla calendar for user {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "Interner Serverfehler",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Ruft den Wochenstundenplan aus Rapla ab
    /// </summary>
    [HttpGet("week-schedule")]
    public async Task<IActionResult> GetWeekSchedule([FromQuery] DateTime? weekStart = null)
    {
        try
        {
            var startDate = weekStart ?? DateTime.Now.Date;
            // Wochenbeginn auf Montag setzen
            var dayOfWeek = (int)startDate.DayOfWeek;
            var monday = startDate.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));

            var events = await _raplaService.GetWeekScheduleAsync(monday);

            return Ok(new
            {
                success = true,
                weekStart = monday,
                events = events.Select(e => new
                {
                    e.Title,
                    e.Description,
                    e.Location,
                    e.StartTime,
                    e.EndTime,
                    e.Lecturer,
                    e.CourseCode
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting week schedule from Rapla");
            return StatusCode(500, new
            {
                success = false,
                message = "Fehler beim Abrufen des Wochenstundenplans",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Ruft alle Kalendereinträge für einen Benutzer ab
    /// </summary>
    [HttpGet("events/{userId}")]
    public async Task<IActionResult> GetUserEvents(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // TODO: Implement via service layer
            return Ok(new
            {
                success = true,
                count = 0,
                events = Array.Empty<object>(),
                message = "Feature wird über Service-Layer implementiert"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar events for user {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "Fehler beim Abrufen der Kalendereinträge",
                error = ex.Message
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
            var rawData = await _raplaService.GetRawCalendarDataAsync();
            var dataLength = rawData?.Length ?? 0;

            return Ok(new
            {
                success = true,
                message = "Rapla-Verbindung erfolgreich",
                dataLength,
                preview = rawData?.Substring(0, Math.Min(500, dataLength))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Rapla connection");
            return StatusCode(500, new
            {
                success = false,
                message = "Fehler beim Testen der Rapla-Verbindung",
                error = ex.Message
            });
        }
    }
}
