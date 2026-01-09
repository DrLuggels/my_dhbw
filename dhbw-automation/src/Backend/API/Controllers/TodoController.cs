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
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? priority = null)
    {
        try
        {
            var query = _context.Todos.Where(t => t.UserId == userId);

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
}

public class CreateTodoRequest
{
    public int UserId { get; set; }
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
