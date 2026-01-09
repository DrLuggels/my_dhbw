using DHBWAutomation.Backend.Core.DTOs;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DHBWAutomation.Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly ILearningAnalyticsService _learningService;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(
        ILearningAnalyticsService learningService,
        ILogger<KnowledgeBaseController> logger)
    {
        _learningService = learningService;
        _logger = logger;
    }

    /// <summary>
    /// Get all stale knowledge items (not tested in X days)
    /// </summary>
    [HttpGet("stale")]
    public async Task<ActionResult<List<KnowledgeBaseItem>>> GetStaleKnowledgeItems(
        [FromQuery] int daysSinceLastTest = 30)
    {
        try
        {
            var userId = GetUserId();
            var items = await _learningService.GetStaleKnowledgeItemsAsync(userId, daysSinceLastTest);

            _logger.LogInformation($"Retrieved {items.Count} stale knowledge items for user {userId}");

            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stale knowledge items");
            return StatusCode(500, new { message = "Fehler beim Abrufen der Grundkenntnisse" });
        }
    }

    /// <summary>
    /// Generate periodic review exercises for fundamental knowledge
    /// </summary>
    [HttpPost("periodic-review")]
    public async Task<ActionResult<List<GeneratedExercise>>> GeneratePeriodicReviewExercises(
        [FromQuery] int count = 5)
    {
        try
        {
            var userId = GetUserId();

            _logger.LogInformation($"Generating {count} periodic review exercises for user {userId}");

            var exercises = await _learningService.GeneratePeriodicReviewExercisesAsync(userId, count);

            if (exercises.Count == 0)
            {
                return Ok(new
                {
                    message = "Keine Grundkenntnisse zum Auffrischen gefunden. Super!",
                    exercises = new List<GeneratedExercise>()
                });
            }

            _logger.LogInformation($"Generated {exercises.Count} periodic review exercises");

            return Ok(new
            {
                message = $"{exercises.Count} Übungen zur Auffrischung deiner Grundkenntnisse generiert",
                exercises
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating periodic review exercises");
            return StatusCode(500, new { message = "Fehler beim Generieren der Auffrischungs-Übungen" });
        }
    }

    /// <summary>
    /// Create or update a knowledge base item
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseItem>> UpsertKnowledgeBaseItem(
        [FromBody] UpsertKnowledgeBaseRequest request)
    {
        try
        {
            var userId = GetUserId();

            var item = await _learningService.UpsertKnowledgeBaseItemAsync(
                userId,
                request.Subject,
                request.Topic,
                request.Category ?? "grundlagen",
                request.Importance ?? "medium");

            _logger.LogInformation($"Upserted knowledge base item: {request.Subject}/{request.Topic}");

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting knowledge base item");
            return StatusCode(500, new { message = "Fehler beim Speichern des Grundkenntnisse-Items" });
        }
    }

    /// <summary>
    /// Update knowledge base score after exercise completion
    /// </summary>
    [HttpPost("{knowledgeBaseItemId}/score")]
    public async Task<ActionResult> UpdateKnowledgeBaseScore(
        int knowledgeBaseItemId,
        [FromBody] UpdateScoreRequest request)
    {
        try
        {
            await _learningService.UpdateKnowledgeBaseScoreAsync(knowledgeBaseItemId, request.Score);

            _logger.LogInformation($"Updated knowledge base item {knowledgeBaseItemId} with score {request.Score}");

            return Ok(new { message = "Score erfolgreich aktualisiert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating knowledge base score for item {knowledgeBaseItemId}");
            return StatusCode(500, new { message = "Fehler beim Aktualisieren des Scores" });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User ID not found in token");
    }
}

public class UpsertKnowledgeBaseRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Importance { get; set; }
}

public class UpdateScoreRequest
{
    public double Score { get; set; }
}
