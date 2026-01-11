using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Services;
using DHBWAutomation.Backend.Core.Models;
using System.Security.Claims;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for Knowledge Network ("Spinnennetz") operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KnowledgeNetworkController : ControllerBase
{
    private readonly IKnowledgeNetworkService _networkService;
    private readonly ILogger<KnowledgeNetworkController> _logger;

    public KnowledgeNetworkController(
        IKnowledgeNetworkService networkService,
        ILogger<KnowledgeNetworkController> logger)
    {
        _networkService = networkService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Create a link between two entities
    /// </summary>
    [HttpPost("links")]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkDto dto)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var link = await _networkService.CreateLinkAsync(
                userId,
                dto.SourceType,
                dto.SourceId,
                dto.TargetType,
                dto.TargetId,
                dto.LinkType ?? KnowledgeLinkTypes.Related,
                dto.Description
            );

            return Ok(new { success = true, linkId = link.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating link");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a link
    /// </summary>
    [HttpDelete("links/{linkId}")]
    public async Task<IActionResult> DeleteLink(int linkId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var success = await _networkService.DeleteLinkAsync(linkId, userId);
        return Ok(new { success });
    }

    /// <summary>
    /// Get related content for an entity
    /// </summary>
    [HttpGet("related/{entityType}/{entityId}")]
    public async Task<IActionResult> GetRelatedContent(
        string entityType,
        int entityId,
        [FromQuery] int maxResults = 20)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var related = await _networkService.FindRelatedContentAsync(
                entityType, entityId, userId, maxResults);

            return Ok(related);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related content");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Semantic search across all knowledge
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int maxResults = 20)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { message = "Query parameter 'q' is required" });
        }

        try
        {
            var results = await _networkService.SearchAsync(q, userId, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in semantic search");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get network graph for visualization
    /// </summary>
    [HttpGet("graph")]
    public async Task<IActionResult> GetGraph([FromQuery] int maxNodes = 100)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var graph = await _networkService.GetNetworkGraphAsync(userId, maxNodes);
            return Ok(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting network graph");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Generate automatic links based on semantic similarity
    /// </summary>
    [HttpPost("generate-links")]
    public async Task<IActionResult> GenerateLinks([FromQuery] double threshold = 0.85)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var linksCreated = await _networkService.GenerateSemanticLinksAsync(userId, threshold);
            return Ok(new { success = true, linksCreated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating links");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Confirm an auto-generated link
    /// </summary>
    [HttpPost("links/{linkId}/confirm")]
    public async Task<IActionResult> ConfirmLink(int linkId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var success = await _networkService.ConfirmLinkAsync(linkId, userId);
        return Ok(new { success });
    }

    /// <summary>
    /// Reject an auto-generated link
    /// </summary>
    [HttpPost("links/{linkId}/reject")]
    public async Task<IActionResult> RejectLink(int linkId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var success = await _networkService.RejectLinkAsync(linkId, userId);
        return Ok(new { success });
    }

    /// <summary>
    /// Get pending auto-generated links for review
    /// </summary>
    [HttpGet("links/pending")]
    public async Task<IActionResult> GetPendingLinks()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var pendingLinks = await _networkService.GetPendingLinksAsync(userId);
            return Ok(new { success = true, data = pendingLinks });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending links");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Index all existing documents and exercises (create embeddings)
    /// </summary>
    [HttpPost("index-all")]
    public async Task<IActionResult> IndexAllContent()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _networkService.IndexAllUserContentAsync(userId);
            return Ok(new
            {
                success = true,
                documentsProcessed = result.DocumentsProcessed,
                exercisesProcessed = result.ExercisesProcessed,
                knowledgeItemsProcessed = result.KnowledgeItemsProcessed,
                totalProcessed = result.TotalProcessed,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing content");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

/// <summary>
/// DTO for creating a link
/// </summary>
public class CreateLinkDto
{
    public string SourceType { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string? LinkType { get; set; }
    public string? Description { get; set; }
}
