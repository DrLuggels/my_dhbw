using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for the DeepTutor-style Learning Engine.
/// Handles document processing pipeline, knowledge graph, and adaptive question generation.
/// </summary>
[Authorize]
[ApiController]
[Route("api/learning-engine")]
public class LearningEngineController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILearningEngineService _learningEngine;
    private readonly ILogger<LearningEngineController> _logger;

    public LearningEngineController(
        AppDbContext context,
        ILearningEngineService learningEngine,
        ILogger<LearningEngineController> logger)
    {
        _context = context;
        _learningEngine = learningEngine;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("id")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        throw new UnauthorizedAccessException("User ID not found in claims");
    }

    // === Document Processing Pipeline ===

    /// <summary>
    /// Process a document through the Learning Engine pipeline.
    /// Triggers: Parse -> Chunk -> Embed -> Extract Entities -> Extract Relationships
    /// </summary>
    [HttpPost("process-document/{documentId}")]
    public async Task<IActionResult> ProcessDocument(
        int documentId,
        [FromBody] LearningProcessingOptions? options = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify document ownership
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
            {
                return NotFound(new { success = false, message = "Dokument nicht gefunden" });
            }

            _logger.LogInformation("Processing document {DocumentId} for user {UserId}", documentId, userId);

            var result = await _learningEngine.ProcessDocumentAsync(documentId, userId, options);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage,
                    warnings = result.Warnings
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Dokument verarbeitet: {result.EntitiesExtracted} Entitäten, {result.RelationshipsExtracted} Beziehungen",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            return StatusCode(500, new { success = false, message = "Fehler bei der Dokumentverarbeitung" });
        }
    }

    /// <summary>
    /// Process multiple documents in batch.
    /// </summary>
    [HttpPost("process-documents")]
    public async Task<IActionResult> ProcessDocumentsBatch(
        [FromBody] BatchProcessRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify ownership of all documents
            var documents = await _context.Documents
                .Where(d => request.DocumentIds.Contains(d.Id) && d.UserId == userId)
                .Select(d => d.Id)
                .ToListAsync();

            if (documents.Count != request.DocumentIds.Count)
            {
                return BadRequest(new { success = false, message = "Einige Dokumente wurden nicht gefunden" });
            }

            _logger.LogInformation("Batch processing {Count} documents for user {UserId}",
                request.DocumentIds.Count, userId);

            var results = await _learningEngine.ProcessDocumentsBatchAsync(
                request.DocumentIds,
                userId,
                request.Options);

            var successCount = results.Count(r => r.Success);
            var totalEntities = results.Sum(r => r.EntitiesExtracted);
            var totalRelationships = results.Sum(r => r.RelationshipsExtracted);

            return Ok(new
            {
                success = true,
                message = $"{successCount}/{results.Count} Dokumente verarbeitet",
                data = new
                {
                    results,
                    summary = new
                    {
                        totalDocuments = results.Count,
                        successCount,
                        failureCount = results.Count - successCount,
                        totalEntities,
                        totalRelationships
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch document processing");
            return StatusCode(500, new { success = false, message = "Fehler bei der Batch-Verarbeitung" });
        }
    }

    // === Knowledge Graph ===

    /// <summary>
    /// Get the knowledge graph for a document.
    /// </summary>
    [HttpGet("knowledge-graph/document/{documentId}")]
    public async Task<IActionResult> GetDocumentKnowledgeGraph(int documentId)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify document ownership
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
            {
                return NotFound(new { success = false, message = "Dokument nicht gefunden" });
            }

            var graph = await _learningEngine.GetDocumentKnowledgeGraphAsync(documentId, userId);

            return Ok(new { success = true, data = graph });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge graph for document {DocumentId}", documentId);
            return StatusCode(500, new { success = false, message = "Fehler beim Laden des Wissensgraphen" });
        }
    }

    /// <summary>
    /// Get the full knowledge graph for the current user.
    /// </summary>
    [HttpGet("knowledge-graph")]
    public async Task<IActionResult> GetUserKnowledgeGraph([FromQuery] KnowledgeGraphOptions? options = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            var graph = await _learningEngine.GetUserKnowledgeGraphAsync(userId, options);

            return Ok(new { success = true, data = graph });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user knowledge graph");
            return StatusCode(500, new { success = false, message = "Fehler beim Laden des Wissensgraphen" });
        }
    }

    /// <summary>
    /// Get entities related to a specific entity.
    /// </summary>
    [HttpGet("entities/{entityId}/related")]
    public async Task<IActionResult> GetRelatedEntities(
        int entityId,
        [FromQuery] int depth = 1)
    {
        try
        {
            var userId = GetCurrentUserId();

            var entities = await _learningEngine.GetRelatedEntitiesAsync(entityId, userId, depth);

            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related entities for {EntityId}", entityId);
            return StatusCode(500, new { success = false, message = "Fehler beim Laden verwandter Entitäten" });
        }
    }

    /// <summary>
    /// Search entities by name or description.
    /// </summary>
    [HttpGet("entities/search")]
    public async Task<IActionResult> SearchEntities(
        [FromQuery] string query,
        [FromQuery] string? entityType = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { success = false, message = "Suchbegriff erforderlich" });
            }

            var entities = await _learningEngine.SearchEntitiesAsync(userId, query, entityType, limit);

            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching entities with query: {Query}", query);
            return StatusCode(500, new { success = false, message = "Fehler bei der Suche" });
        }
    }

    /// <summary>
    /// Merge duplicate entities.
    /// </summary>
    [HttpPost("entities/{primaryId}/merge")]
    public async Task<IActionResult> MergeEntities(
        int primaryId,
        [FromBody] MergeEntitiesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var success = await _learningEngine.MergeEntitiesAsync(primaryId, request.DuplicateIds, userId);

            if (!success)
            {
                return BadRequest(new { success = false, message = "Zusammenführung fehlgeschlagen" });
            }

            return Ok(new { success = true, message = "Entitäten erfolgreich zusammengeführt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging entities");
            return StatusCode(500, new { success = false, message = "Fehler beim Zusammenführen" });
        }
    }

    // === Question Generation ===

    /// <summary>
    /// Generate questions based on documents and knowledge graph.
    /// </summary>
    [HttpPost("generate-questions")]
    public async Task<IActionResult> GenerateQuestions([FromBody] QuestionGenerationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var questions = await _learningEngine.GenerateQuestionsAsync(userId, request);

            return Ok(new
            {
                success = true,
                message = $"{questions.Count} Fragen generiert",
                data = questions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating questions");
            return StatusCode(500, new { success = false, message = "Fehler bei der Fragengenerierung" });
        }
    }

    /// <summary>
    /// Generate questions for a specific entity.
    /// </summary>
    [HttpPost("entities/{entityId}/generate-questions")]
    public async Task<IActionResult> GenerateEntityQuestions(
        int entityId,
        [FromQuery] int count = 5,
        [FromQuery] string? questionType = null,
        [FromQuery] int? bloomLevel = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            var questions = await _learningEngine.GenerateEntityQuestionsAsync(
                entityId, userId, count, questionType, bloomLevel);

            return Ok(new
            {
                success = true,
                message = $"{questions.Count} Fragen generiert",
                data = questions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating entity questions for {EntityId}", entityId);
            return StatusCode(500, new { success = false, message = "Fehler bei der Fragengenerierung" });
        }
    }

    // === User Performance ===

    /// <summary>
    /// Submit an answer to a question.
    /// </summary>
    [HttpPost("submit-answer")]
    public async Task<IActionResult> SubmitAnswer([FromBody] DHBWAutomation.Backend.Core.Interfaces.AnswerSubmission submission)
    {
        try
        {
            var userId = GetCurrentUserId();

            var feedback = await _learningEngine.SubmitAnswerAsync(userId, submission);

            return Ok(new { success = true, data = feedback });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer");
            return StatusCode(500, new { success = false, message = "Fehler beim Speichern der Antwort" });
        }
    }

    /// <summary>
    /// Get entities the user needs to practice (weak areas).
    /// </summary>
    [HttpGet("weak-areas")]
    public async Task<IActionResult> GetWeakAreas([FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetCurrentUserId();

            var weakAreas = await _learningEngine.GetWeakAreasAsync(userId, limit);

            return Ok(new { success = true, data = weakAreas });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weak areas");
            return StatusCode(500, new { success = false, message = "Fehler beim Laden der Schwachstellen" });
        }
    }

    /// <summary>
    /// Get entities due for review (spaced repetition).
    /// </summary>
    [HttpGet("due-for-review")]
    public async Task<IActionResult> GetDueForReview([FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetCurrentUserId();

            var entities = await _learningEngine.GetDueForReviewAsync(userId, limit);

            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting due for review");
            return StatusCode(500, new { success = false, message = "Fehler beim Laden der Wiederholungen" });
        }
    }

    /// <summary>
    /// Get user's mastery statistics.
    /// </summary>
    [HttpGet("mastery-stats")]
    public async Task<IActionResult> GetMasteryStats([FromQuery] string? subject = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            var stats = await _learningEngine.GetMasteryStatsAsync(userId, subject);

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mastery stats");
            return StatusCode(500, new { success = false, message = "Fehler beim Laden der Statistiken" });
        }
    }
}

// === Request DTOs ===

public class BatchProcessRequest
{
    public List<int> DocumentIds { get; set; } = new();
    public LearningProcessingOptions? Options { get; set; }
}

public class MergeEntitiesRequest
{
    public List<int> DuplicateIds { get; set; } = new();
}
