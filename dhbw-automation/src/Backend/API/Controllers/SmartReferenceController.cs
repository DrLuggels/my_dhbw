using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.API.DTOs;
using System.Security.Claims;
using System.Diagnostics;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for Smart Reference operations - analyzing and linking notes to calendar events
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SmartReferenceController : ControllerBase
{
    private readonly IContextualLinkService _contextualLinkService;
    private readonly ITemporalReferenceService _temporalService;
    private readonly ILogger<SmartReferenceController> _logger;

    public SmartReferenceController(
        IContextualLinkService contextualLinkService,
        ITemporalReferenceService temporalService,
        ILogger<SmartReferenceController> logger)
    {
        _contextualLinkService = contextualLinkService;
        _temporalService = temporalService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Resolves natural language references in text to calendar events
    /// </summary>
    /// <remarks>
    /// Example: "der Prof von heute morgen" resolves to the professor and event from this morning
    /// </remarks>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(SmartReferenceResult), 200)]
    public async Task<IActionResult> ResolveReferences([FromBody] ResolveReferencesRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var sw = Stopwatch.StartNew();

        try
        {
            var analysisResult = await _contextualLinkService.AnalyzeTextAsync(
                request.Text,
                userId,
                request.ReferenceDate);

            var result = new SmartReferenceResult
            {
                Success = analysisResult.Success,
                OverallConfidence = analysisResult.OverallConfidence,
                ProcessingTimeMs = sw.ElapsedMilliseconds,
                References = analysisResult.References.Select(r => new SmartReference
                {
                    OriginalText = r.OriginalText,
                    ReferenceType = MapReferenceType(r.Type),
                    Confidence = r.Confidence,
                    StartPosition = r.StartPosition,
                    EndPosition = r.EndPosition,
                    ResolvedTo = r.ResolvedEntityId.HasValue ? new ResolvedEntity
                    {
                        EntityType = r.ResolvedEntityType ?? "",
                        EntityId = r.ResolvedEntityId.Value,
                        DisplayName = r.NormalizedValue,
                        Metadata = r.Metadata ?? new Dictionary<string, object>()
                    } : null
                }).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving smart references");
            return BadRequest(new SmartReferenceResult
            {
                Success = false,
                Error = ex.Message,
                ProcessingTimeMs = sw.ElapsedMilliseconds
            });
        }
    }

    /// <summary>
    /// Automatically links a note to related calendar events
    /// </summary>
    [HttpPost("auto-link")]
    [ProducesResponseType(typeof(AutoLinkResponse), 200)]
    public async Task<IActionResult> AutoLinkContent([FromBody] AutoLinkRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var linksCreated = await _contextualLinkService.AutoLinkNoteToEventsAsync(
                userId,
                request.EventId,
                request.NoteContent,
                request.AutoConfirmHighConfidence);

            return Ok(new AutoLinkResponse
            {
                Success = true,
                LinksCreated = linksCreated
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-linking content");
            return BadRequest(new AutoLinkResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets suggested links for note content without creating them
    /// </summary>
    [HttpPost("suggestions")]
    [ProducesResponseType(typeof(List<SuggestedLink>), 200)]
    public async Task<IActionResult> GetSuggestions([FromBody] GetSuggestionsRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var suggestions = await _contextualLinkService.GetSuggestedLinksForNoteAsync(
                userId,
                request.NoteContent,
                request.SourceEventId);

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting link suggestions");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Confirms a suggested link (creates it as a KnowledgeLink)
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ConfirmLinkResponse), 200)]
    public async Task<IActionResult> ConfirmLink([FromBody] ConfirmLinkRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var suggestion = new SuggestedLink
            {
                SourceType = request.SourceType,
                SourceId = request.SourceId,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                LinkType = request.LinkType,
                Confidence = request.Confidence,
                Reason = request.Reason ?? ""
            };

            var link = await _contextualLinkService.ConfirmSuggestedLinkAsync(userId, suggestion);

            return Ok(new ConfirmLinkResponse
            {
                Success = true,
                LinkId = link.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming link");
            return BadRequest(new ConfirmLinkResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets all links related to a specific calendar event
    /// </summary>
    [HttpGet("event/{eventId}/links")]
    [ProducesResponseType(typeof(List<KnowledgeLink>), 200)]
    public async Task<IActionResult> GetLinksForEvent(int eventId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var links = await _contextualLinkService.GetLinksForEventAsync(eventId, userId);
            return Ok(links);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting links for event {EventId}", eventId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Parses a temporal expression to a date/time range
    /// </summary>
    [HttpPost("parse-temporal")]
    [ProducesResponseType(typeof(TemporalParseResult), 200)]
    public async Task<IActionResult> ParseTemporal([FromBody] ParseTemporalRequest request)
    {
        try
        {
            var result = await _temporalService.ParseTemporalExpressionAsync(
                request.Expression,
                request.ReferenceDate);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing temporal expression: {Expression}", request.Expression);
            return BadRequest(new TemporalParseResult
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Finds calendar events by professor name
    /// </summary>
    [HttpGet("professor/{professorName}/events")]
    [ProducesResponseType(typeof(List<CalendarEvent>), 200)]
    public async Task<IActionResult> FindEventsByProfessor(string professorName)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var events = await _contextualLinkService.FindEventsByProfessorAsync(userId, professorName);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding events by professor: {ProfessorName}", professorName);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Finds calendar events by subject
    /// </summary>
    [HttpGet("subject/{subject}/events")]
    [ProducesResponseType(typeof(List<CalendarEvent>), 200)]
    public async Task<IActionResult> FindEventsBySubject(string subject)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var events = await _contextualLinkService.FindEventsBySubjectAsync(userId, subject);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding events by subject: {Subject}", subject);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the professor index for the current user
    /// </summary>
    [HttpGet("professors")]
    [ProducesResponseType(typeof(Dictionary<string, ProfessorInfo>), 200)]
    public async Task<IActionResult> GetProfessorIndex()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var index = await _contextualLinkService.BuildProfessorIndexAsync(userId);
            return Ok(index);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building professor index");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the subject index for the current user
    /// </summary>
    [HttpGet("subjects")]
    [ProducesResponseType(typeof(Dictionary<string, SubjectInfo>), 200)]
    public async Task<IActionResult> GetSubjectIndex()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var index = await _contextualLinkService.BuildSubjectIndexAsync(userId);
            return Ok(index);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building subject index");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private SmartReferenceType MapReferenceType(ContextualReferenceType type)
    {
        return type switch
        {
            ContextualReferenceType.Professor => SmartReferenceType.Professor,
            ContextualReferenceType.Subject => SmartReferenceType.Subject,
            ContextualReferenceType.CourseCode => SmartReferenceType.Subject,
            ContextualReferenceType.TemporalEvent => SmartReferenceType.Event,
            ContextualReferenceType.Location => SmartReferenceType.Location,
            ContextualReferenceType.Document => SmartReferenceType.Document,
            ContextualReferenceType.ProfessorTemporal => SmartReferenceType.ProfessorTemporal,
            _ => SmartReferenceType.Event
        };
    }
}
