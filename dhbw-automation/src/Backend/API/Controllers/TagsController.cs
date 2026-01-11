using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Core.Models;
using System.Security.Claims;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for content tag management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TagsController> _logger;

    public TagsController(AppDbContext context, ILogger<TagsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get all tags for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var tags = await _context.ContentTags
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    color = t.Color,
                    description = t.Description,
                    assignmentCount = t.Assignments.Count
                })
                .ToListAsync();

            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new tag
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            // Check if tag with same name exists
            var exists = await _context.ContentTags
                .AnyAsync(t => t.UserId == userId && t.Name == request.Name);

            if (exists)
            {
                return BadRequest(new { message = "Ein Tag mit diesem Namen existiert bereits" });
            }

            var tag = new ContentTag
            {
                UserId = userId,
                Name = request.Name,
                Color = request.Color ?? "#1976D2",
                Description = request.Description
            };

            _context.ContentTags.Add(tag);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = tag.Id,
                name = tag.Name,
                color = tag.Color,
                description = tag.Description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a tag
    /// </summary>
    [HttpPut("{tagId}")]
    public async Task<IActionResult> UpdateTag(int tagId, [FromBody] UpdateTagRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var tag = await _context.ContentTags
                .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

            if (tag == null)
            {
                return NotFound(new { message = "Tag nicht gefunden" });
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                // Check if new name conflicts
                var nameExists = await _context.ContentTags
                    .AnyAsync(t => t.UserId == userId && t.Name == request.Name && t.Id != tagId);

                if (nameExists)
                {
                    return BadRequest(new { message = "Ein Tag mit diesem Namen existiert bereits" });
                }

                tag.Name = request.Name;
            }

            if (request.Color != null) tag.Color = request.Color;
            if (request.Description != null) tag.Description = request.Description;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = tag.Id,
                name = tag.Name,
                color = tag.Color,
                description = tag.Description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag {TagId}", tagId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a tag
    /// </summary>
    [HttpDelete("{tagId}")]
    public async Task<IActionResult> DeleteTag(int tagId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var tag = await _context.ContentTags
                .Include(t => t.Assignments)
                .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

            if (tag == null)
            {
                return NotFound(new { message = "Tag nicht gefunden" });
            }

            _context.ContentTags.Remove(tag);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag {TagId}", tagId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assign a tag to an entity
    /// </summary>
    [HttpPost("{tagId}/assign")]
    public async Task<IActionResult> AssignTag(int tagId, [FromBody] AssignTagRequest request)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var tag = await _context.ContentTags
                .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

            if (tag == null)
            {
                return NotFound(new { message = "Tag nicht gefunden" });
            }

            // Check if assignment already exists
            var exists = await _context.ContentTagAssignments
                .AnyAsync(a => a.TagId == tagId &&
                              a.EntityType == request.EntityType &&
                              a.EntityId == request.EntityId);

            if (exists)
            {
                return BadRequest(new { message = "Tag ist bereits zugewiesen" });
            }

            var assignment = new ContentTagAssignment
            {
                TagId = tagId,
                EntityType = request.EntityType,
                EntityId = request.EntityId
            };

            _context.ContentTagAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning tag {TagId}", tagId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a tag from an entity
    /// </summary>
    [HttpDelete("{tagId}/assign")]
    public async Task<IActionResult> RemoveTagAssignment(int tagId, [FromQuery] string entityType, [FromQuery] int entityId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var assignment = await _context.ContentTagAssignments
                .Include(a => a.Tag)
                .FirstOrDefaultAsync(a => a.TagId == tagId &&
                                         a.EntityType == entityType &&
                                         a.EntityId == entityId &&
                                         a.Tag.UserId == userId);

            if (assignment == null)
            {
                return NotFound(new { message = "Zuweisung nicht gefunden" });
            }

            _context.ContentTagAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag assignment");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get tags for a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetEntityTags(string entityType, int entityId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var tags = await _context.ContentTagAssignments
                .Where(a => a.EntityType == entityType &&
                           a.EntityId == entityId &&
                           a.Tag.UserId == userId)
                .Select(a => new
                {
                    id = a.Tag.Id,
                    name = a.Tag.Name,
                    color = a.Tag.Color
                })
                .ToListAsync();

            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags for entity");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all entities with a specific tag
    /// </summary>
    [HttpGet("{tagId}/entities")]
    public async Task<IActionResult> GetTagEntities(int tagId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var assignments = await _context.ContentTagAssignments
                .Where(a => a.TagId == tagId && a.Tag.UserId == userId)
                .Select(a => new
                {
                    entityType = a.EntityType,
                    entityId = a.EntityId,
                    assignedAt = a.AssignedAt
                })
                .ToListAsync();

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entities for tag {TagId}", tagId);
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
}

public class UpdateTagRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
}

public class AssignTagRequest
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
}
