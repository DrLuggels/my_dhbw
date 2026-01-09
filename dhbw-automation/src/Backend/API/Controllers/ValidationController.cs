using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DHBWAutomation.Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ValidationController : ControllerBase
{
    private readonly IValidationService _validationService;
    private readonly ILogger<ValidationController> _logger;

    public ValidationController(IValidationService validationService, ILogger<ValidationController> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Holt alle ausstehenden Staging-Entitäten für den aktuellen User
    /// </summary>
    /// <param name="status">Optional: Filter nach Status (pending_review, confirmed, rejected)</param>
    /// <returns>Liste von Staging-Entitäten mit Fragen</returns>
    [HttpGet("pending")]
    public async Task<ActionResult<List<StagedEntity>>> GetPendingEntities([FromQuery] string? status = null)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User ID nicht gefunden");

            var entities = await _validationService.GetPendingStagedEntitiesAsync(userId, status);

            return Ok(new
            {
                count = entities.Count,
                entities,
                summary = new
                {
                    highPriority = entities.Count(e => e.Priority == "high" || e.Priority == "urgent"),
                    withQuestions = entities.Count(e => e.Questions.Any()),
                    lowConfidence = entities.Count(e => e.ConfidenceScore < 70)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending staged entities");
            return StatusCode(500, "Fehler beim Abrufen der ausstehenden Entitäten");
        }
    }

    /// <summary>
    /// Holt Details zu einer spezifischen Staging-Entität
    /// </summary>
    /// <param name="id">ID der Staging-Entität</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<StagedEntity>> GetStagedEntity(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User ID nicht gefunden");

            var entities = await _validationService.GetPendingStagedEntitiesAsync(userId);
            var entity = entities.FirstOrDefault(e => e.Id == id);

            if (entity == null)
                return NotFound($"Staging-Entität {id} nicht gefunden");

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching staged entity {id}");
            return StatusCode(500, "Fehler beim Abrufen der Entität");
        }
    }

    /// <summary>
    /// Beantwortet Fragen zu einer Staging-Entität
    /// </summary>
    /// <param name="id">ID der Staging-Entität</param>
    /// <param name="request">Antworten auf die Fragen</param>
    [HttpPost("{id}/answer")]
    public async Task<ActionResult> AnswerQuestions(int id, [FromBody] AnswerQuestionsRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User ID nicht gefunden");

            var success = await _validationService.AnswerQuestionsAsync(id, userId, request.Answers);

            if (!success)
                return NotFound($"Staging-Entität {id} nicht gefunden oder Fehler beim Beantworten");

            return Ok(new { message = $"{request.Answers.Count} Fragen beantwortet" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error answering questions for staged entity {id}");
            return StatusCode(500, "Fehler beim Beantworten der Fragen");
        }
    }

    /// <summary>
    /// Bestätigt eine Staging-Entität und überträgt sie in die Produktiv-DB
    /// </summary>
    /// <param name="id">ID der Staging-Entität</param>
    /// <param name="request">Optional: Notizen des Users</param>
    [HttpPost("{id}/confirm")]
    public async Task<ActionResult> ConfirmEntity(int id, [FromBody] ConfirmEntityRequest? request = null)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User ID nicht gefunden");

            var promotedId = await _validationService.ConfirmAndPromoteAsync(id, userId, request?.UserNotes);

            if (promotedId == null)
                return BadRequest("Entität konnte nicht bestätigt werden. Möglicherweise fehlen kritische Antworten.");

            return Ok(new
            {
                message = "Entität erfolgreich bestätigt und in Produktiv-DB übertragen",
                promotedEntityId = promotedId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error confirming staged entity {id}");
            return StatusCode(500, "Fehler beim Bestätigen der Entität");
        }
    }

    /// <summary>
    /// Lehnt eine Staging-Entität ab
    /// </summary>
    /// <param name="id">ID der Staging-Entität</param>
    /// <param name="request">Grund der Ablehnung</param>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectEntity(int id, [FromBody] RejectEntityRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User ID nicht gefunden");

            var success = await _validationService.RejectStagedEntityAsync(id, userId, request.Reason);

            if (!success)
                return NotFound($"Staging-Entität {id} nicht gefunden");

            return Ok(new { message = "Entität abgelehnt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error rejecting staged entity {id}");
            return StatusCode(500, "Fehler beim Ablehnen der Entität");
        }
    }

    /// <summary>
    /// Ändert die Daten einer Staging-Entität (User-Korrektur)
    /// </summary>
    /// <param name="id">ID der Staging-Entität</param>
    /// <param name="request">Geänderte Daten</param>
    [HttpPut("{id}")]
    public async Task<ActionResult> ModifyEntity(int id, [FromBody] ModifyEntityRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User ID nicht gefunden");

            var success = await _validationService.ModifyStagedEntityAsync(id, userId, request.ModifiedData);

            if (!success)
                return NotFound($"Staging-Entität {id} nicht gefunden");

            return Ok(new { message = "Entität erfolgreich geändert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error modifying staged entity {id}");
            return StatusCode(500, "Fehler beim Ändern der Entität");
        }
    }

    /// <summary>
    /// Holt Statistiken über das Staging-System
    /// </summary>
    /// <param name="days">Zeitraum in Tagen (default: 30)</param>
    [HttpGet("statistics")]
    public async Task<ActionResult<StagingStatistics>> GetStatistics([FromQuery] int days = 30)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User ID nicht gefunden");

            var stats = await _validationService.GetStagingStatisticsAsync(userId, days);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching staging statistics");
            return StatusCode(500, "Fehler beim Abrufen der Statistiken");
        }
    }

    /// <summary>
    /// Bulk-Bestätigung: Bestätigt alle Entitäten mit hohem Confidence Score
    /// </summary>
    /// <param name="minConfidence">Minimum Confidence Score (default: 95)</param>
    [HttpPost("bulk-confirm")]
    public async Task<ActionResult> BulkConfirm([FromQuery] int minConfidence = 95)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User ID nicht gefunden");

            var entities = await _validationService.GetPendingStagedEntitiesAsync(userId, "pending_review");
            var highConfidence = entities.Where(e => e.ConfidenceScore >= minConfidence && e.Questions.Count == 0).ToList();

            var promotedCount = 0;
            foreach (var entity in highConfidence)
            {
                var promotedId = await _validationService.ConfirmAndPromoteAsync(entity.Id, userId, $"Bulk confirmed (Confidence: {entity.ConfidenceScore}%)");
                if (promotedId != null)
                    promotedCount++;
            }

            return Ok(new
            {
                message = $"{promotedCount} von {highConfidence.Count} Entitäten automatisch bestätigt",
                promotedCount,
                totalEligible = highConfidence.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk confirm");
            return StatusCode(500, "Fehler bei der Bulk-Bestätigung");
        }
    }
}

// Request DTOs
public class AnswerQuestionsRequest
{
    public Dictionary<string, string> Answers { get; set; } = new();
}

public class ConfirmEntityRequest
{
    public string? UserNotes { get; set; }
}

public class RejectEntityRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class ModifyEntityRequest
{
    public string ModifiedData { get; set; } = string.Empty;
}
