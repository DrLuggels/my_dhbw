using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.BackgroundServices;

namespace DHBWAutomation.Backend.API.Controllers;

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

    /// <summary>
    /// Get JavaDocs sync status
    /// </summary>
    [HttpGet("javadocs-sync")]
    public IActionResult GetJavaDocsSyncStatus(
        [FromServices] JavaDocsSyncBackgroundService syncService)
    {
        var status = syncService.GetStatus();

        return Ok(new
        {
            isRunning = status.IsRunning,
            lastSuccessfulSync = status.LastSuccessfulSync,
            startedAt = status.StartedAt,
            completedAt = status.CompletedAt,
            lastResult = status.LastResult != null ? new
            {
                success = status.LastResult.Success,
                added = status.LastResult.Added,
                updated = status.LastResult.Updated,
                unchanged = status.LastResult.Unchanged,
                embeddingsGenerated = status.LastResult.EmbeddingsGenerated,
                error = status.LastResult.Error
            } : null,
            lastError = status.LastError
        });
    }

    /// <summary>
    /// Manually trigger JavaDocs sync (requires authentication)
    /// </summary>
    [HttpPost("javadocs-sync/trigger")]
    [Authorize]
    public async Task<IActionResult> TriggerJavaDocsSync(
        [FromServices] JavaDocsSyncBackgroundService syncService)
    {
        _logger.LogInformation("Manual JavaDocs sync triggered via API");

        var result = await syncService.TriggerSyncAsync();

        return Ok(new
        {
            success = result.Success,
            added = result.Added,
            updated = result.Updated,
            unchanged = result.Unchanged,
            embeddingsGenerated = result.EmbeddingsGenerated,
            error = result.Error
        });
    }
}
