using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for Personal Knowledge Graph operations.
/// Manages knowledge nodes, edges, and graph visualization.
/// </summary>
[Authorize]
[ApiController]
[Route("api/pkg")]
public class PersonalKnowledgeGraphController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPersonalKnowledgeGraphService _pkgService;
    private readonly ILogger<PersonalKnowledgeGraphController> _logger;

    public PersonalKnowledgeGraphController(
        AppDbContext context,
        IPersonalKnowledgeGraphService pkgService,
        ILogger<PersonalKnowledgeGraphController> logger)
    {
        _context = context;
        _pkgService = pkgService;
        _logger = logger;
    }

    /// <summary>
    /// Get the complete knowledge graph for a user.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserGraph(int userId)
    {
        try
        {
            var graph = await _pkgService.GetUserGraphAsync(userId);
            return Ok(new { success = true, data = graph });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge graph for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen des Wissensgraphen" });
        }
    }

    /// <summary>
    /// Get all knowledge nodes for a user.
    /// </summary>
    [HttpGet("{userId}/nodes")]
    public async Task<IActionResult> GetUserNodes(int userId, [FromQuery] string? subject = null)
    {
        try
        {
            var nodes = await _pkgService.GetUserNodesAsync(userId, subject);
            return Ok(new { success = true, data = nodes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nodes for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der Knoten" });
        }
    }

    /// <summary>
    /// Get a specific knowledge node.
    /// </summary>
    [HttpGet("node/{nodeId}")]
    public async Task<IActionResult> GetNode(int nodeId)
    {
        try
        {
            var node = await _pkgService.GetNodeAsync(nodeId);
            if (node == null)
            {
                return NotFound(new { success = false, message = "Knoten nicht gefunden" });
            }
            return Ok(new { success = true, data = node });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting node {NodeId}", nodeId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen des Knotens" });
        }
    }

    /// <summary>
    /// Get or create a knowledge node.
    /// </summary>
    [HttpPost("{userId}/nodes")]
    public async Task<IActionResult> GetOrCreateNode(
        int userId,
        [FromBody] CreateNodeRequest request)
    {
        try
        {
            var node = await _pkgService.GetOrCreateNodeAsync(
                userId,
                request.Subject,
                request.Topic,
                request.Subtopic);

            return Ok(new { success = true, data = node });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating node for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Erstellen des Knotens" });
        }
    }

    /// <summary>
    /// Get weak nodes (low mastery) for a user.
    /// </summary>
    [HttpGet("{userId}/weak-areas")]
    public async Task<IActionResult> GetWeakAreas(int userId, [FromQuery] double threshold = 0.4)
    {
        try
        {
            var weakNodes = await _pkgService.GetWeakNodesAsync(userId, threshold);
            return Ok(new { success = true, data = weakNodes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weak areas for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der schwachen Bereiche" });
        }
    }

    /// <summary>
    /// Get fading nodes (low effective strength) for a user.
    /// </summary>
    [HttpGet("{userId}/fading")]
    public async Task<IActionResult> GetFadingNodes(int userId, [FromQuery] double threshold = 0.5)
    {
        try
        {
            var fadingNodes = await _pkgService.GetFadingNodesAsync(userId, threshold);
            return Ok(new { success = true, data = fadingNodes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fading nodes for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der verblassenden Knoten" });
        }
    }

    /// <summary>
    /// Get edges for a node.
    /// </summary>
    [HttpGet("node/{nodeId}/edges")]
    public async Task<IActionResult> GetNodeEdges(int nodeId)
    {
        try
        {
            var edges = await _pkgService.GetNodeEdgesAsync(nodeId);
            return Ok(new { success = true, data = edges });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting edges for node {NodeId}", nodeId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der Verbindungen" });
        }
    }

    /// <summary>
    /// Get fading edges for a user.
    /// </summary>
    [HttpGet("{userId}/fading-edges")]
    public async Task<IActionResult> GetFadingEdges(int userId, [FromQuery] double threshold = 0.3)
    {
        try
        {
            var fadingEdges = await _pkgService.GetFadingEdgesAsync(userId, threshold);
            return Ok(new { success = true, data = fadingEdges });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fading edges for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der verblassenden Verbindungen" });
        }
    }

    /// <summary>
    /// Record an exercise result and update the knowledge graph.
    /// </summary>
    [HttpPost("{userId}/exercise-result")]
    public async Task<IActionResult> RecordExerciseResult(
        int userId,
        [FromBody] ExerciseResultRequest request)
    {
        try
        {
            var impact = await _pkgService.RecordExerciseResultAsync(
                userId,
                request.NodeId,
                request.IsCorrect,
                request.Difficulty,
                request.ResponseTimeSeconds);

            return Ok(new { success = true, data = impact });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording exercise result for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Speichern des Ergebnisses" });
        }
    }

    /// <summary>
    /// Generate semantic edges for a user's knowledge graph.
    /// </summary>
    [HttpPost("{userId}/generate-edges")]
    public async Task<IActionResult> GenerateSemanticEdges(int userId, [FromQuery] double threshold = 0.7)
    {
        try
        {
            var edgesCreated = await _pkgService.GenerateSemanticEdgesAsync(userId, threshold);
            return Ok(new { success = true, data = new { edgesCreated } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating edges for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Generieren der Verbindungen" });
        }
    }

    /// <summary>
    /// Apply time decay to all nodes and edges.
    /// </summary>
    [HttpPost("{userId}/apply-decay")]
    public async Task<IActionResult> ApplyTimeDecay(int userId)
    {
        try
        {
            await _pkgService.ApplyTimeDecayAsync(userId);
            return Ok(new { success = true, message = "Zeitverfall wurde angewendet" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying decay for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Anwenden des Zeitverfalls" });
        }
    }

    /// <summary>
    /// Get the personal decay rate for a user and subject.
    /// </summary>
    [HttpGet("{userId}/decay-rate")]
    public async Task<IActionResult> GetPersonalDecayRate(int userId, [FromQuery] string subject)
    {
        try
        {
            var decayRate = await _pkgService.GetPersonalDecayRateAsync(userId, subject);
            return Ok(new { success = true, data = new { subject, decayRate } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting decay rate for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der Decay-Rate" });
        }
    }
}

/// <summary>
/// Request to create or get a knowledge node.
/// </summary>
public class CreateNodeRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Subtopic { get; set; }
}

/// <summary>
/// Request to record an exercise result.
/// </summary>
public class ExerciseResultRequest
{
    public int NodeId { get; set; }
    public bool IsCorrect { get; set; }
    public string Difficulty { get; set; } = "medium";
    public double? ResponseTimeSeconds { get; set; }
}
