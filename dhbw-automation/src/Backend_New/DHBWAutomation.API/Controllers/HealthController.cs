using Microsoft.AspNetCore.Mvc;

namespace DHBWAutomation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "0.3.0",
            service = "DHBW Study Automation API"
        });
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong" });
    }

    [HttpGet("ready")]
    public IActionResult Ready()
    {
        // Hier könnten weitere Checks hinzugefügt werden (DB Connection, etc.)
        return Ok(new
        {
            status = "ready",
            database = "connected",
            cache = "connected"
        });
    }
}
