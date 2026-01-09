using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.API.DTOs;
using DHBWAutomation.Backend.Core.Interfaces;

namespace DHBWAutomation.Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TravelController : ControllerBase
{
    private readonly ITravelService _travelService;
    private readonly ILogger<TravelController> _logger;

    public TravelController(ITravelService travelService, ILogger<TravelController> logger)
    {
        _travelService = travelService;
        _logger = logger;
    }

    /// <summary>
    /// Ruft Zugverbindungen ab
    /// </summary>
    /// <param name="request">Verbindungsanfrage mit Start, Ziel und optionaler Zeit</param>
    /// <returns>Liste der verfügbaren Verbindungen</returns>
    [HttpPost("connections")]
    public async Task<ActionResult<TrainConnectionResponse>> GetConnections([FromBody] TrainConnectionRequest request)
    {
        try
        {
            _logger.LogInformation("Fetching train connections from {From} to {To}", request.From, request.To);
            
            var result = await _travelService.GetConnectionsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving train connections");
            return StatusCode(500, new { error = "Fehler beim Abrufen der Verbindungen" });
        }
    }

    /// <summary>
    /// Ruft Verbindungen für Standard-Route (Laupheim West - Ravensburg) ab
    /// </summary>
    [HttpGet("connections/default")]
    public async Task<ActionResult<TrainConnectionResponse>> GetDefaultConnections([FromQuery] int maxConnections = 5)
    {
        try
        {
            var request = new TrainConnectionRequest
            {
                From = "Laupheim West",
                To = "Ravensburg",
                MaxConnections = maxConnections
            };

            var result = await _travelService.GetConnectionsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default train connections");
            return StatusCode(500, new { error = "Fehler beim Abrufen der Verbindungen" });
        }
    }
}
