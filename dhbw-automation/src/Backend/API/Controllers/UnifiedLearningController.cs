using DHBWAutomation.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Unified Learning API - combines all learning features:
/// - Knowledge Graph with entity extraction
/// - FSRS + Decay spaced repetition
/// - RAG-enhanced exercise generation
/// - 20/40/40 adaptive difficulty
/// - Bloom's Taxonomy progression
/// - Deadline-aware priority recommendations
/// - Prerequisite management
/// </summary>
[ApiController]
[Route("api/unified-learning")]
public class UnifiedLearningController : ControllerBase
{
    private readonly IUnifiedLearningService _learningService;
    private readonly ILogger<UnifiedLearningController> _logger;

    public UnifiedLearningController(
        IUnifiedLearningService learningService,
        ILogger<UnifiedLearningController> logger)
    {
        _learningService = learningService;
        _logger = logger;
    }

    #region Entity Management

    /// <summary>
    /// Get all entities for a user with optional filtering.
    /// </summary>
    [HttpGet("entities/{userId}")]
    public async Task<IActionResult> GetEntities(
        int userId,
        [FromQuery] string? subject = null,
        [FromQuery] string? entityType = null,
        [FromQuery] double? minMastery = null,
        [FromQuery] int? limit = null)
    {
        try
        {
            var filter = new UnifiedEntityFilter
            {
                Subject = subject,
                EntityType = entityType,
                MinMastery = minMastery,
                Limit = limit
            };

            var entities = await _learningService.GetUserEntitiesAsync(userId, filter);
            return Ok(new { success = true, data = entities.Select(e => MapToDto(e)) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entities for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific entity by ID.
    /// </summary>
    [HttpGet("entities/detail/{entityId}")]
    public async Task<IActionResult> GetEntity(int entityId)
    {
        try
        {
            var entity = await _learningService.GetEntityAsync(entityId);
            if (entity == null)
                return NotFound(new { success = false, message = "Entity not found" });

            return Ok(new { success = true, data = MapToDto(entity) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity {EntityId}", entityId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Search entities by name or description.
    /// </summary>
    [HttpGet("entities/{userId}/search")]
    public async Task<IActionResult> SearchEntities(
        int userId,
        [FromQuery] string query,
        [FromQuery] string? entityType = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var entities = await _learningService.SearchEntitiesAsync(userId, query, entityType, limit);
            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching entities for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Entity Extraction

    /// <summary>
    /// Extract entities from a document using Claude.
    /// </summary>
    [HttpPost("extract/{documentId}")]
    public async Task<IActionResult> ExtractEntities(
        int documentId,
        [FromQuery] int userId,
        [FromBody] UnifiedExtractionOptions? options = null)
    {
        try
        {
            var result = await _learningService.ExtractEntitiesFromDocumentAsync(documentId, userId, options);
            return Ok(new { success = result.Success, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting entities from document {DocumentId}", documentId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Process multiple documents in batch.
    /// </summary>
    [HttpPost("extract/batch")]
    public async Task<IActionResult> ExtractEntitiesBatch(
        [FromQuery] int userId,
        [FromBody] BatchExtractionRequest request)
    {
        try
        {
            var results = await _learningService.ProcessDocumentsBatchAsync(
                request.DocumentIds, userId, request.Options);
            return Ok(new { success = true, data = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document batch for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Knowledge Graph

    /// <summary>
    /// Get the knowledge graph for a user.
    /// </summary>
    [HttpGet("knowledge-graph/{userId}")]
    public async Task<IActionResult> GetKnowledgeGraph(
        int userId,
        [FromQuery] string? subject = null,
        [FromQuery] string? topic = null,
        [FromQuery] int? maxEntities = null)
    {
        try
        {
            var options = new UnifiedGraphOptions
            {
                Subject = subject,
                Topic = topic,
                MaxEntities = maxEntities
            };

            var graph = await _learningService.GetKnowledgeGraphAsync(userId, options);
            return Ok(new { success = true, data = graph });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge graph for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get entities related to a specific entity.
    /// </summary>
    [HttpGet("entities/{entityId}/related")]
    public async Task<IActionResult> GetRelatedEntities(
        int entityId,
        [FromQuery] int userId,
        [FromQuery] int depth = 1)
    {
        try
        {
            var entities = await _learningService.GetRelatedEntitiesAsync(entityId, userId, depth);
            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related entities for {EntityId}", entityId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get graph statistics.
    /// </summary>
    [HttpGet("knowledge-graph/{userId}/stats")]
    public async Task<IActionResult> GetGraphStats(int userId)
    {
        try
        {
            var stats = await _learningService.GetGraphStatsAsync(userId);
            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting graph stats for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Exercise Generation

    /// <summary>
    /// Generate exercises with RAG context, adaptive Bloom level, and 20/40/40 difficulty.
    /// </summary>
    [HttpPost("generate-exercise")]
    public async Task<IActionResult> GenerateExercise(
        [FromQuery] int userId,
        [FromBody] UnifiedExerciseRequest request)
    {
        try
        {
            var exercise = await _learningService.GenerateExerciseAsync(userId, request);
            return Ok(new { success = true, data = exercise });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exercise for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Generate multiple exercises for a learning session.
    /// </summary>
    [HttpPost("generate-exercises")]
    public async Task<IActionResult> GenerateExercises(
        [FromQuery] int userId,
        [FromBody] UnifiedExerciseRequest request,
        [FromQuery] int count = 5)
    {
        try
        {
            var exercises = await _learningService.GenerateExercisesAsync(userId, request, count);
            return Ok(new { success = true, data = exercises });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exercises for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Generate questions for a specific entity.
    /// </summary>
    [HttpGet("entities/{entityId}/exercises")]
    public async Task<IActionResult> GenerateEntityExercises(
        int entityId,
        [FromQuery] int userId,
        [FromQuery] int count = 5,
        [FromQuery] string? questionType = null)
    {
        try
        {
            var exercises = await _learningService.GenerateEntityExercisesAsync(entityId, userId, count, questionType);
            return Ok(new { success = true, data = exercises });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating entity exercises for {EntityId}", entityId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Answer Submission

    /// <summary>
    /// Submit an answer and update entity using FSRS + Decay.
    /// </summary>
    [HttpPost("submit-answer")]
    public async Task<IActionResult> SubmitAnswer(
        [FromQuery] int userId,
        [FromBody] UnifiedAnswerSubmission submission)
    {
        try
        {
            var feedback = await _learningService.SubmitAnswerAsync(userId, submission);
            return Ok(new { success = true, data = feedback });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Record an exercise result.
    /// </summary>
    [HttpPost("record-result")]
    public async Task<IActionResult> RecordResult(
        [FromQuery] int userId,
        [FromBody] RecordResultRequest request)
    {
        try
        {
            var impact = await _learningService.RecordExerciseResultAsync(
                userId,
                request.EntityId,
                request.IsCorrect,
                request.Difficulty,
                request.BloomLevel,
                request.ResponseTimeSeconds);

            return Ok(new { success = true, data = impact });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording result for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Spaced Repetition

    /// <summary>
    /// Get entities due for review.
    /// </summary>
    [HttpGet("due-for-review/{userId}")]
    public async Task<IActionResult> GetDueForReview(int userId, [FromQuery] int limit = 10)
    {
        try
        {
            var entities = await _learningService.GetDueForReviewAsync(userId, limit);
            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting due for review for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get fading entities (decaying knowledge).
    /// </summary>
    [HttpGet("fading/{userId}")]
    public async Task<IActionResult> GetFadingEntities(
        int userId,
        [FromQuery] double threshold = 0.5,
        [FromQuery] int limit = 10)
    {
        try
        {
            var entities = await _learningService.GetFadingEntitiesAsync(userId, threshold, limit);
            return Ok(new { success = true, data = entities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fading entities for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Recommendations

    /// <summary>
    /// Get prioritized learning recommendations.
    /// </summary>
    [HttpGet("recommendations/{userId}")]
    public async Task<IActionResult> GetRecommendations(int userId, [FromQuery] int limit = 10)
    {
        try
        {
            var priorities = await _learningService.GetPrioritizedRecommendationsAsync(userId, limit);
            return Ok(new { success = true, data = priorities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get the next best thing to learn.
    /// </summary>
    [HttpGet("next/{userId}")]
    public async Task<IActionResult> GetNextRecommendation(int userId)
    {
        try
        {
            var priority = await _learningService.GetNextRecommendationAsync(userId);
            return Ok(new { success = true, data = priority });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next recommendation for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Recalculate all priorities for a user.
    /// </summary>
    [HttpPost("recalculate-priorities/{userId}")]
    public async Task<IActionResult> RecalculatePriorities(int userId)
    {
        try
        {
            await _learningService.RecalculatePrioritiesAsync(userId);
            return Ok(new { success = true, message = "Priorities recalculated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating priorities for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Prerequisites

    /// <summary>
    /// Check prerequisites for an entity.
    /// </summary>
    [HttpGet("prerequisites/{entityId}/check")]
    public async Task<IActionResult> CheckPrerequisites(int entityId, [FromQuery] int userId)
    {
        try
        {
            var result = await _learningService.CheckPrerequisitesAsync(entityId, userId);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking prerequisites for entity {EntityId}", entityId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get the learning path to unlock an entity.
    /// </summary>
    [HttpGet("learning-path/{entityId}")]
    public async Task<IActionResult> GetLearningPath(int entityId, [FromQuery] int userId)
    {
        try
        {
            var path = await _learningService.GetLearningPathAsync(entityId, userId);
            return Ok(new { success = true, data = path });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning path for entity {EntityId}", entityId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Create a prerequisite relationship.
    /// </summary>
    [HttpPost("prerequisites")]
    public async Task<IActionResult> CreatePrerequisite(
        [FromQuery] int userId,
        [FromBody] CreatePrerequisiteRequest request)
    {
        try
        {
            var relationship = await _learningService.CreatePrerequisiteAsync(
                userId,
                request.PrerequisiteEntityId,
                request.DependentEntityId,
                request.RequiredMasteryLevel,
                request.IsStrict);

            return Ok(new { success = true, data = relationship });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prerequisite for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get comprehensive mastery statistics.
    /// </summary>
    [HttpGet("stats/{userId}")]
    public async Task<IActionResult> GetMasteryStats(int userId, [FromQuery] string? subject = null)
    {
        try
        {
            var stats = await _learningService.GetMasteryStatsAsync(userId, subject);
            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mastery stats for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get weak areas to practice.
    /// </summary>
    [HttpGet("weak-areas/{userId}")]
    public async Task<IActionResult> GetWeakAreas(int userId, [FromQuery] int limit = 10)
    {
        try
        {
            var weakAreas = await _learningService.GetWeakAreasAsync(userId, limit);
            return Ok(new { success = true, data = weakAreas });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weak areas for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get learning summary for dashboard.
    /// </summary>
    [HttpGet("summary/{userId}")]
    public async Task<IActionResult> GetLearningSummary(int userId)
    {
        try
        {
            var summary = await _learningService.GetLearningSummaryAsync(userId);
            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning summary for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Helper Methods

    private object MapToDto(DHBWAutomation.Backend.Core.Models.UnifiedKnowledgeEntity entity)
    {
        return new
        {
            entity.Id,
            entity.EntityType,
            entity.Name,
            entity.Description,
            entity.Subject,
            entity.Topic,
            entity.Subtopic,
            entity.ConfidenceScore,
            entity.ImportanceScore,
            entity.MasteryScore,
            EffectiveKnowledge = entity.EffectiveKnowledge,
            entity.CurrentBloomLevel,
            entity.TotalAttempts,
            entity.TotalCorrect,
            SuccessRate = entity.SuccessRate,
            entity.NextReview,
            entity.FsrsState,
            entity.LastInteraction
        };
    }

    #endregion
}

#region Request DTOs

public class BatchExtractionRequest
{
    public List<int> DocumentIds { get; set; } = new();
    public UnifiedExtractionOptions? Options { get; set; }
}

public class RecordResultRequest
{
    public int EntityId { get; set; }
    public bool IsCorrect { get; set; }
    public string Difficulty { get; set; } = "medium";
    public int BloomLevel { get; set; } = 2;
    public double? ResponseTimeSeconds { get; set; }
}

public class CreatePrerequisiteRequest
{
    public int PrerequisiteEntityId { get; set; }
    public int DependentEntityId { get; set; }
    public double RequiredMasteryLevel { get; set; } = 0.6;
    public bool IsStrict { get; set; } = true;
}

#endregion
