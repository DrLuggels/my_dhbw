using Microsoft.AspNetCore.Mvc;

namespace DHBWAutomation.API.Controllers;

[ApiController]
[Route("api/calendar/google")]
public class GoogleCalendarController : ControllerBase
{
    private readonly ILogger<GoogleCalendarController> _logger;

    public GoogleCalendarController(ILogger<GoogleCalendarController> logger)
    {
        _logger = logger;
    }

    [HttpGet("status/{userId}")]
    public IActionResult GetStatus(int userId)
    {
        return Ok(new
        {
            success = true,
            data = new
            {
                isConnected = false,
                available = false,
                message = "Google Calendar Service noch nicht vollständig konfiguriert"
            },
            message = "Service nicht verfügbar",
            errors = (string[]?)null
        });
    }

    [HttpGet("authorize/{userId}")]
    public IActionResult Authorize(int userId)
    {
        return Ok(new
        {
            success = false,
            data = (object?)null,
            message = "Google Calendar Service noch nicht konfiguriert. Bitte Google OAuth Credentials in appsettings.json hinzufügen.",
            errors = new[] { "Service nicht konfiguriert" }
        });
    }
}
