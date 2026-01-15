using System.Security.Claims;
using DHBWAutomation.Backend.Core.Services.OmniLearning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Alias to avoid conflict with GenerateExerciseRequest from LearningController
using OmniGenerateExerciseRequest = DHBWAutomation.Backend.Core.Services.OmniLearning.GenerateExerciseRequest;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// OmniLearning Controller - Omnifunktionales Lernsystem API
///
/// Konsolidiert alle Lern-Endpoints in einer einheitlichen, deutschen API:
/// - Dokumentenverarbeitung
/// - Wissens-Entitäten und Knowledge Graph
/// - Adaptive Übungsgenerierung
/// - Spaced Repetition mit FSRS + Decay
/// - Lern-Prioritäten und Analytics
/// </summary>
[Route("api/omni")]
[ApiController]
[Authorize]
public class OmniLearningController : ControllerBase
{
    private readonly IOmniLearningEngineService _omniService;
    private readonly ILogger<OmniLearningController> _logger;

    public OmniLearningController(
        IOmniLearningEngineService omniService,
        ILogger<OmniLearningController> logger)
    {
        _omniService = omniService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    #region Dokumentenverarbeitung

    /// <summary>
    /// Verarbeitet ein Dokument: Chunking → Embedding → Entity-Extraktion → Knowledge Graph
    /// </summary>
    [HttpPost("dokumente/{documentId}/verarbeiten")]
    public async Task<ActionResult<DocumentProcessingResult>> ProcessDocument(
        int documentId, [FromBody] ProcessingOptions? options = null)
    {
        var result = await _omniService.ProcessDocumentAsync(documentId, GetUserId(), options);
        return Ok(result);
    }

    /// <summary>
    /// Verarbeitet mehrere Dokumente im Batch
    /// </summary>
    [HttpPost("dokumente/batch-verarbeiten")]
    public async Task<ActionResult<BatchProcessingResult>> ProcessDocumentsBatch(
        [FromBody] OmniBatchProcessRequest request)
    {
        var result = await _omniService.ProcessDocumentsBatchAsync(
            request.DocumentIds, GetUserId(), request.Options);
        return Ok(result);
    }

    #endregion

    #region Entitäten

    /// <summary>
    /// Holt alle Entitäten des Users
    /// </summary>
    [HttpGet("entitaeten")]
    public async Task<ActionResult<List<UnifiedEntityDto>>> GetEntities(
        [FromQuery] EntityListFilters? filters = null)
    {
        var entities = await _omniService.GetUserEntitiesAsync(GetUserId(), filters);
        return Ok(entities);
    }

    /// <summary>
    /// Sucht Entitäten (semantisch oder textbasiert)
    /// </summary>
    [HttpPost("entitaeten/suche")]
    public async Task<ActionResult<List<UnifiedEntityDto>>> SearchEntities(
        [FromBody] OmniEntitySearchRequest request)
    {
        var results = await _omniService.SearchEntitiesAsync(GetUserId(), request.Query, request.Filters);
        return Ok(results);
    }

    /// <summary>
    /// Holt eine einzelne Entität mit Details
    /// </summary>
    [HttpGet("entitaeten/{entityId}")]
    public async Task<ActionResult<UnifiedEntityDto>> GetEntity(int entityId)
    {
        var entity = await _omniService.GetEntityAsync(entityId, GetUserId());
        if (entity == null)
            return NotFound();
        return Ok(entity.ToDto());
    }

    /// <summary>
    /// Erstellt eine neue Entität
    /// </summary>
    [HttpPost("entitaeten")]
    public async Task<ActionResult<UnifiedEntityDto>> CreateEntity([FromBody] CreateEntityDto dto)
    {
        var entity = await _omniService.CreateEntityAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetEntity), new { entityId = entity.Id }, entity.ToDto());
    }

    /// <summary>
    /// Holt verwandte Entitäten
    /// </summary>
    [HttpGet("entitaeten/{entityId}/verwandt")]
    public async Task<ActionResult<List<UnifiedEntityDto>>> GetRelatedEntities(
        int entityId, [FromQuery] int depth = 2)
    {
        var related = await _omniService.GetRelatedEntitiesAsync(entityId, GetUserId(), depth);
        return Ok(related);
    }

    /// <summary>
    /// Merged doppelte Entitäten
    /// </summary>
    [HttpPost("entitaeten/zusammenfuehren")]
    public async Task<ActionResult<UnifiedEntityDto>> MergeEntities([FromBody] OmniMergeEntitiesRequest request)
    {
        var merged = await _omniService.MergeEntitiesAsync(request.EntityIds, GetUserId());
        return Ok(merged.ToDto());
    }

    #endregion

    #region Beziehungen

    /// <summary>
    /// Erstellt eine neue Beziehung
    /// </summary>
    [HttpPost("beziehungen")]
    public async Task<ActionResult<UnifiedRelationshipDto>> CreateRelationship(
        [FromBody] CreateRelationshipDto dto)
    {
        var relationship = await _omniService.CreateRelationshipAsync(GetUserId(), dto);
        return Ok(relationship.ToDto());
    }

    /// <summary>
    /// Generiert automatisch Beziehungen für eine Entität
    /// </summary>
    [HttpPost("entitaeten/{entityId}/beziehungen-generieren")]
    public async Task<ActionResult<List<UnifiedRelationshipDto>>> GenerateRelationships(int entityId)
    {
        var relationships = await _omniService.GenerateRelationshipsAsync(entityId, GetUserId());
        return Ok(relationships.Select(r => r.ToDto()));
    }

    /// <summary>
    /// Prüft Prerequisites für eine Entität
    /// </summary>
    [HttpGet("entitaeten/{entityId}/voraussetzungen")]
    public async Task<ActionResult<PrerequisiteCheckResult>> CheckPrerequisites(int entityId)
    {
        var result = await _omniService.CheckPrerequisitesAsync(GetUserId(), entityId);
        return Ok(result);
    }

    /// <summary>
    /// Holt die Prerequisite-Kette
    /// </summary>
    [HttpGet("entitaeten/{entityId}/voraussetzungs-kette")]
    public async Task<ActionResult<List<PrerequisiteChainDto>>> GetPrerequisiteChain(int entityId)
    {
        var chain = await _omniService.GetPrerequisiteChainAsync(entityId, GetUserId());
        return Ok(chain);
    }

    #endregion

    #region Übungen

    /// <summary>
    /// Generiert eine einzelne Übung
    /// </summary>
    [HttpPost("uebungen/generieren")]
    public async Task<ActionResult<OmniExerciseDto>> GenerateExercise(
        [FromBody] OmniGenerateExerciseRequest request)
    {
        var exercise = await _omniService.GenerateExerciseAsync(GetUserId(), request);
        return Ok(exercise);
    }

    /// <summary>
    /// Generiert eine Lern-Session mit mehreren Übungen
    /// </summary>
    [HttpPost("uebungen/session")]
    public async Task<ActionResult<List<OmniExerciseDto>>> GenerateSession(
        [FromBody] GenerateSessionRequest request)
    {
        var exercises = await _omniService.GenerateSessionAsync(GetUserId(), request);
        return Ok(exercises);
    }

    /// <summary>
    /// Reicht eine Antwort ein
    /// </summary>
    [HttpPost("uebungen/{exerciseId}/antwort")]
    public async Task<ActionResult<ExerciseSubmissionResult>> SubmitAnswer(
        int exerciseId, [FromBody] AnswerSubmissionDto submission)
    {
        var result = await _omniService.SubmitAnswerAsync(exerciseId, GetUserId(), submission);
        return Ok(result);
    }

    /// <summary>
    /// Holt fällige Übungen
    /// </summary>
    [HttpGet("uebungen/faellig")]
    public async Task<ActionResult<List<OmniExerciseDto>>> GetDueExercises([FromQuery] int limit = 10)
    {
        var exercises = await _omniService.GetDueExercisesAsync(GetUserId(), limit);
        return Ok(exercises);
    }

    /// <summary>
    /// Holt die nächste empfohlene Übung
    /// </summary>
    [HttpGet("uebungen/naechste")]
    public async Task<ActionResult<OmniExerciseDto?>> GetNextExercise()
    {
        var exercise = await _omniService.GetNextExerciseAsync(GetUserId());
        if (exercise == null)
            return NoContent();
        return Ok(exercise);
    }

    #endregion

    #region Prioritäten & Scheduling

    /// <summary>
    /// Berechnet Lern-Prioritäten
    /// </summary>
    [HttpPost("prioritaeten/berechnen")]
    public async Task<ActionResult<List<OmniPriorityDto>>> CalculatePriorities(
        [FromBody] PriorityCalculationOptions? options = null)
    {
        var priorities = await _omniService.CalculatePrioritiesAsync(GetUserId(), options);
        return Ok(priorities.Select(p => new OmniPriorityDto
        {
            EntityId = p.UnifiedEntityId,
            EntityName = p.EntityName,
            Subject = p.Subject,
            Topic = p.Topic,
            CompositeScore = p.CompositeScore,
            Rank = p.Rank ?? 0,
            IsBlocked = p.IsBlocked,
            BlockReason = p.BlockReason,
            RecommendedAction = p.RecommendedAction,
            Deadline = p.Deadline
        }));
    }

    /// <summary>
    /// Holt Schwachstellen
    /// </summary>
    [HttpGet("schwachstellen")]
    public async Task<ActionResult<List<WeakAreaDto>>> GetWeakAreas([FromQuery] int limit = 10)
    {
        var weakAreas = await _omniService.GetWeakAreasAsync(GetUserId(), limit);
        return Ok(weakAreas);
    }

    /// <summary>
    /// Holt überfällige Wiederholungen
    /// </summary>
    [HttpGet("ueberfaellig")]
    public async Task<ActionResult<List<OverdueItemDto>>> GetOverdueItems()
    {
        var overdue = await _omniService.GetOverdueItemsAsync(GetUserId());
        return Ok(overdue);
    }

    #endregion

    #region Visualisierung

    /// <summary>
    /// Holt den Knowledge Graph für Visualisierung
    /// </summary>
    [HttpGet("graph")]
    public async Task<ActionResult<KnowledgeGraphDto>> GetKnowledgeGraph(
        [FromQuery] GraphVisualizationFilters? filters = null)
    {
        var graph = await _omniService.GetKnowledgeGraphAsync(GetUserId(), filters);
        return Ok(graph);
    }

    /// <summary>
    /// Holt Cluster-Visualisierung
    /// </summary>
    [HttpGet("cluster")]
    public async Task<ActionResult<ClusterVisualizationDto>> GetClusterVisualization(
        [FromQuery] string? subject = null)
    {
        var clusters = await _omniService.GetClusterVisualizationAsync(GetUserId(), subject);
        return Ok(clusters);
    }

    #endregion

    #region Analytics

    /// <summary>
    /// Holt Mastery-Statistiken
    /// </summary>
    [HttpGet("statistiken")]
    public async Task<ActionResult<MasteryStatsDto>> GetMasteryStats()
    {
        var stats = await _omniService.GetMasteryStatsAsync(GetUserId());
        return Ok(stats);
    }

    /// <summary>
    /// Holt Lern-Streak
    /// </summary>
    [HttpGet("streak")]
    public async Task<ActionResult<LearningStreakDto>> GetStreak()
    {
        var streak = await _omniService.GetStreakAsync(GetUserId());
        return Ok(streak);
    }

    /// <summary>
    /// Holt Schwierigkeitsverteilung (20/40/40)
    /// </summary>
    [HttpGet("schwierigkeitsverteilung")]
    public async Task<ActionResult<DifficultyDistributionDto>> GetDifficultyDistribution(
        [FromQuery] string? subject = null)
    {
        var distribution = await _omniService.GetDifficultyDistributionAsync(GetUserId(), subject);
        return Ok(distribution);
    }

    /// <summary>
    /// Holt Bloom-Progression
    /// </summary>
    [HttpGet("bloom-progression")]
    public async Task<ActionResult<BloomProgressionDto>> GetBloomProgression(
        [FromQuery] string? subject = null)
    {
        var progression = await _omniService.GetBloomProgressionAsync(GetUserId(), subject);
        return Ok(progression);
    }

    #endregion
}

#region Request DTOs

public class OmniBatchProcessRequest
{
    public int[] DocumentIds { get; set; } = Array.Empty<int>();
    public ProcessingOptions? Options { get; set; }
}

public class OmniEntitySearchRequest
{
    public string Query { get; set; } = string.Empty;
    public EntitySearchFilters? Filters { get; set; }
}

public class OmniMergeEntitiesRequest
{
    public int[] EntityIds { get; set; } = Array.Empty<int>();
}

public class OmniPriorityDto
{
    public int? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public double CompositeScore { get; set; }
    public int Rank { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public string? RecommendedAction { get; set; }
    public DateTime? Deadline { get; set; }
}

#endregion
