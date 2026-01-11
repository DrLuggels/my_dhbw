using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TodoController> _logger;

    public TodoController(AppDbContext context, ILogger<TodoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all todos for a user with optional filtering
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserTodos(
        int userId,
        [FromQuery] int? listId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? priority = null,
        [FromQuery] bool includeArchived = false)
    {
        try
        {
            var query = _context.Todos.Where(t => t.UserId == userId);

            // Filter by list
            if (listId.HasValue)
                query = query.Where(t => t.ListId == listId.Value);

            // Filter archived
            if (!includeArchived)
                query = query.Where(t => t.ArchivedAt == null);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category == category);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(t => t.Priority == priority);

            var todos = await query
                .OrderBy(t => t.Status) // pending first
                .ThenByDescending(t => t.Priority == "urgent")
                .ThenByDescending(t => t.Priority == "high")
                .ThenByDescending(t => t.Priority == "medium")
                .ThenBy(t => t.DueDate)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();

            return Ok(new { success = true, data = todos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting todos for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get a specific todo by ID
    /// </summary>
    [HttpGet("{todoId}")]
    public async Task<IActionResult> GetTodo(int todoId, [FromQuery] int userId)
    {
        try
        {
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (todo == null)
                return NotFound(new { success = false, message = "TODO nicht gefunden" });

            return Ok(new { success = true, data = todo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting todo {todoId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Create a new todo
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoRequest request)
    {
        try
        {
            var todo = new Todo
            {
                UserId = request.UserId,
                ListId = request.ListId,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category ?? "general",
                Priority = request.Priority ?? "medium",
                Status = "pending",
                DueDate = request.DueDate,
                EstimatedMinutes = request.EstimatedMinutes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Todos.Add(todo);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created new todo {todo.Id}: {todo.Title}");

            return Ok(new { success = true, data = todo, message = "TODO erstellt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating todo");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Update todo status
    /// </summary>
    [HttpPatch("{todoId}/status")]
    public async Task<IActionResult> UpdateTodoStatus(
        int todoId,
        [FromBody] UpdateTodoStatusRequest request)
    {
        try
        {
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == request.UserId);

            if (todo == null)
                return NotFound(new { success = false, message = "TODO nicht gefunden" });

            var oldStatus = todo.Status;
            todo.Status = request.Status;

            if (request.Status == "completed")
            {
                todo.CompletedAt = DateTime.UtcNow;
            }
            else if (request.Status == "pending")
            {
                todo.CompletedAt = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated todo {todoId} status: {oldStatus} -> {request.Status}");

            return Ok(new { success = true, data = todo, message = "Status aktualisiert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating todo status {todoId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Update a todo
    /// </summary>
    [HttpPut("{todoId}")]
    public async Task<IActionResult> UpdateTodo(
        int todoId,
        [FromBody] UpdateTodoRequest request)
    {
        try
        {
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == request.UserId);

            if (todo == null)
                return NotFound(new { success = false, message = "TODO nicht gefunden" });

            if (request.Title != null) todo.Title = request.Title;
            if (request.Description != null) todo.Description = request.Description;
            if (request.Category != null) todo.Category = request.Category;
            if (request.Priority != null) todo.Priority = request.Priority;
            if (request.DueDate.HasValue) todo.DueDate = request.DueDate;
            if (request.EstimatedMinutes.HasValue) todo.EstimatedMinutes = request.EstimatedMinutes;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated todo {todoId}");

            return Ok(new { success = true, data = todo, message = "TODO aktualisiert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating todo {todoId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Delete a todo
    /// </summary>
    [HttpDelete("{todoId}")]
    public async Task<IActionResult> DeleteTodo(int todoId, [FromQuery] int userId)
    {
        try
        {
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (todo == null)
                return NotFound(new { success = false, message = "TODO nicht gefunden" });

            _context.Todos.Remove(todo);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted todo {todoId}");

            return Ok(new { success = true, message = "TODO gelöscht" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting todo {todoId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get todo statistics for a user
    /// </summary>
    [HttpGet("user/{userId}/stats")]
    public async Task<IActionResult> GetTodoStats(int userId)
    {
        try
        {
            var todos = await _context.Todos
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var stats = new
            {
                total = todos.Count,
                pending = todos.Count(t => t.Status == "pending"),
                inProgress = todos.Count(t => t.Status == "in_progress"),
                completed = todos.Count(t => t.Status == "completed"),
                cancelled = todos.Count(t => t.Status == "cancelled"),
                urgent = todos.Count(t => t.Priority == "urgent" && t.Status == "pending"),
                overdue = todos.Count(t => t.DueDate < DateTime.UtcNow && t.Status == "pending")
            };

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting todo stats for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get todos for a specific list
    /// </summary>
    [HttpGet("list/{listId}")]
    public async Task<IActionResult> GetTodosByList(int listId, [FromQuery] int userId)
    {
        try
        {
            var todos = await _context.Todos
                .Where(t => t.UserId == userId && t.ListId == listId && t.ArchivedAt == null)
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.Priority == "urgent")
                .ThenByDescending(t => t.Priority == "high")
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            return Ok(new { success = true, data = todos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting todos for list {listId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Move a todo to a different list
    /// </summary>
    [HttpPatch("{todoId}/move")]
    public async Task<IActionResult> MoveTodoToList(int todoId, [FromBody] MoveTodoRequest request)
    {
        try
        {
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == request.UserId);

            if (todo == null)
                return NotFound(new { success = false, message = "TODO nicht gefunden" });

            // Verify target list exists and belongs to user
            if (request.ListId.HasValue)
            {
                var targetList = await _context.TodoLists
                    .FirstOrDefaultAsync(l => l.Id == request.ListId && l.UserId == request.UserId);

                if (targetList == null)
                    return BadRequest(new { success = false, message = "Ziel-Liste nicht gefunden" });
            }

            todo.ListId = request.ListId;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Moved todo {todoId} to list {request.ListId}");

            return Ok(new { success = true, data = todo, message = "TODO verschoben" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error moving todo {todoId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Archive a todo
    /// </summary>
    [HttpPost("{todoId}/archive")]
    public async Task<IActionResult> ArchiveTodo(int todoId, [FromQuery] int userId)
    {
        try
        {
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (todo == null)
                return NotFound(new { success = false, message = "TODO nicht gefunden" });

            if (todo.ArchivedAt != null)
                return BadRequest(new { success = false, message = "TODO bereits archiviert" });

            todo.ArchivedAt = DateTime.UtcNow;

            // Move to archive list if exists
            var archiveList = await _context.TodoLists
                .FirstOrDefaultAsync(l => l.UserId == userId && l.IsArchiveList);

            if (archiveList != null)
                todo.ListId = archiveList.Id;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Archived todo {todoId}");

            return Ok(new { success = true, data = todo, message = "TODO archiviert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error archiving todo {todoId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Unarchive a todo
    /// </summary>
    [HttpPost("{todoId}/unarchive")]
    public async Task<IActionResult> UnarchiveTodo(int todoId, [FromQuery] int userId, [FromQuery] int? targetListId = null)
    {
        try
        {
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (todo == null)
                return NotFound(new { success = false, message = "TODO nicht gefunden" });

            if (todo.ArchivedAt == null)
                return BadRequest(new { success = false, message = "TODO ist nicht archiviert" });

            todo.ArchivedAt = null;

            // Move to target list or default list
            if (targetListId.HasValue)
            {
                todo.ListId = targetListId.Value;
            }
            else
            {
                var defaultList = await _context.TodoLists
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.IsDefault);

                if (defaultList != null)
                    todo.ListId = defaultList.Id;
                else
                    todo.ListId = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Unarchived todo {todoId}");

            return Ok(new { success = true, data = todo, message = "TODO wiederhergestellt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error unarchiving todo {todoId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get archived todos for a user
    /// </summary>
    [HttpGet("user/{userId}/archived")]
    public async Task<IActionResult> GetArchivedTodos(int userId)
    {
        try
        {
            var todos = await _context.Todos
                .Where(t => t.UserId == userId && t.ArchivedAt != null)
                .OrderByDescending(t => t.ArchivedAt)
                .ToListAsync();

            return Ok(new { success = true, data = todos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting archived todos for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get overdue todos for a user (for reminders)
    /// </summary>
    [HttpGet("user/{userId}/overdue")]
    public async Task<IActionResult> GetOverdueTodos(int userId, [FromQuery] int daysOld = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

            var todos = await _context.Todos
                .Where(t => t.UserId == userId
                    && t.ArchivedAt == null
                    && t.Status == "pending"
                    && t.CreatedAt < cutoffDate)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            return Ok(new { success = true, data = todos, count = todos.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting overdue todos for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get related todos based on keywords
    /// </summary>
    [HttpGet("{todoId}/related")]
    public async Task<IActionResult> GetRelatedTodos(int todoId, [FromQuery] int userId)
    {
        try
        {
            var todo = await _context.Todos
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (todo == null)
                return NotFound(new { success = false, message = "TODO nicht gefunden" });

            // Find related by keywords
            var relatedTodos = new List<Todo>();

            if (!string.IsNullOrEmpty(todo.RelatedKeywords))
            {
                var keywords = todo.RelatedKeywords.Split(',').Select(k => k.Trim().ToLower()).ToList();

                relatedTodos = await _context.Todos
                    .Where(t => t.UserId == userId
                        && t.Id != todoId
                        && t.ArchivedAt == null
                        && t.RelatedKeywords != null)
                    .ToListAsync();

                relatedTodos = relatedTodos
                    .Where(t => t.RelatedKeywords!.Split(',')
                        .Select(k => k.Trim().ToLower())
                        .Any(k => keywords.Contains(k)))
                    .ToList();
            }

            // Also find by parent/child relationship
            if (todo.ParentTodoId.HasValue)
            {
                var siblings = await _context.Todos
                    .Where(t => t.UserId == userId
                        && t.ParentTodoId == todo.ParentTodoId
                        && t.Id != todoId
                        && t.ArchivedAt == null)
                    .ToListAsync();

                relatedTodos.AddRange(siblings.Where(s => !relatedTodos.Any(r => r.Id == s.Id)));
            }

            var children = await _context.Todos
                .Where(t => t.UserId == userId && t.ParentTodoId == todoId && t.ArchivedAt == null)
                .ToListAsync();

            relatedTodos.AddRange(children.Where(c => !relatedTodos.Any(r => r.Id == c.Id)));

            return Ok(new { success = true, data = relatedTodos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting related todos for {todoId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }
}

public class CreateTodoRequest
{
    public int UserId { get; set; }
    public int? ListId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }
}

public class UpdateTodoStatusRequest
{
    public int UserId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class UpdateTodoRequest
{
    public int UserId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }
}

public class MoveTodoRequest
{
    public int UserId { get; set; }
    public int? ListId { get; set; }
}
