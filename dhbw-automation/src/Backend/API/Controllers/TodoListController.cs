using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TodoListController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TodoListController> _logger;

    public TodoListController(AppDbContext context, ILogger<TodoListController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all todo lists for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserLists(int userId)
    {
        try
        {
            var lists = await _context.TodoLists
                .Where(l => l.UserId == userId)
                .OrderBy(l => l.SortOrder)
                .Select(l => new
                {
                    l.Id,
                    l.UserId,
                    l.Name,
                    l.Icon,
                    l.Color,
                    l.SortOrder,
                    l.IsDefault,
                    l.IsArchiveList,
                    l.CreatedAt,
                    l.UpdatedAt,
                    TodoCount = l.Todos.Count(t => t.Status != "completed" && t.ArchivedAt == null)
                })
                .ToListAsync();

            return Ok(new { success = true, data = lists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting todo lists for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get a specific list with its todos
    /// </summary>
    [HttpGet("{listId}")]
    public async Task<IActionResult> GetList(int listId, [FromQuery] int userId)
    {
        try
        {
            var list = await _context.TodoLists
                .Include(l => l.Todos.Where(t => t.ArchivedAt == null))
                .FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId);

            if (list == null)
                return NotFound(new { success = false, message = "Liste nicht gefunden" });

            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting list {listId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Create a new todo list
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateList([FromBody] CreateTodoListRequest request)
    {
        try
        {
            // Check if list name already exists for user
            var existingList = await _context.TodoLists
                .FirstOrDefaultAsync(l => l.UserId == request.UserId && l.Name == request.Name);

            if (existingList != null)
                return BadRequest(new { success = false, message = "Liste mit diesem Namen existiert bereits" });

            // Get max sort order
            var maxSortOrder = await _context.TodoLists
                .Where(l => l.UserId == request.UserId)
                .MaxAsync(l => (int?)l.SortOrder) ?? -1;

            var list = new TodoList
            {
                UserId = request.UserId,
                Name = request.Name,
                Icon = request.Icon ?? "mdi-checkbox-marked-circle-outline",
                Color = request.Color ?? "#1976D2",
                SortOrder = maxSortOrder + 1,
                IsDefault = false,
                IsArchiveList = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.TodoLists.Add(list);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created new todo list {list.Id}: {list.Name}");

            return Ok(new { success = true, data = list, message = "Liste erstellt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating todo list");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Update a todo list
    /// </summary>
    [HttpPut("{listId}")]
    public async Task<IActionResult> UpdateList(int listId, [FromBody] UpdateTodoListRequest request)
    {
        try
        {
            var list = await _context.TodoLists
                .FirstOrDefaultAsync(l => l.Id == listId && l.UserId == request.UserId);

            if (list == null)
                return NotFound(new { success = false, message = "Liste nicht gefunden" });

            if (list.IsArchiveList)
                return BadRequest(new { success = false, message = "Archiv-Liste kann nicht bearbeitet werden" });

            // Check name uniqueness if changing name
            if (request.Name != null && request.Name != list.Name)
            {
                var existingList = await _context.TodoLists
                    .FirstOrDefaultAsync(l => l.UserId == request.UserId && l.Name == request.Name);

                if (existingList != null)
                    return BadRequest(new { success = false, message = "Liste mit diesem Namen existiert bereits" });

                list.Name = request.Name;
            }

            if (request.Icon != null) list.Icon = request.Icon;
            if (request.Color != null) list.Color = request.Color;
            list.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated todo list {listId}");

            return Ok(new { success = true, data = list, message = "Liste aktualisiert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating todo list {listId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Delete a todo list (todos are not deleted, just unassigned)
    /// </summary>
    [HttpDelete("{listId}")]
    public async Task<IActionResult> DeleteList(int listId, [FromQuery] int userId)
    {
        try
        {
            var list = await _context.TodoLists
                .FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId);

            if (list == null)
                return NotFound(new { success = false, message = "Liste nicht gefunden" });

            if (list.IsArchiveList)
                return BadRequest(new { success = false, message = "Archiv-Liste kann nicht gelöscht werden" });

            if (list.IsDefault)
                return BadRequest(new { success = false, message = "Standard-Liste kann nicht gelöscht werden" });

            // Todos will be unassigned automatically due to SetNull delete behavior
            _context.TodoLists.Remove(list);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted todo list {listId}");

            return Ok(new { success = true, message = "Liste gelöscht" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting todo list {listId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Set a list as the default list
    /// </summary>
    [HttpPatch("{listId}/default")]
    public async Task<IActionResult> SetDefaultList(int listId, [FromBody] SetDefaultListRequest request)
    {
        try
        {
            var list = await _context.TodoLists
                .FirstOrDefaultAsync(l => l.Id == listId && l.UserId == request.UserId);

            if (list == null)
                return NotFound(new { success = false, message = "Liste nicht gefunden" });

            if (list.IsArchiveList)
                return BadRequest(new { success = false, message = "Archiv-Liste kann nicht als Standard gesetzt werden" });

            // Remove default from all other lists
            var otherLists = await _context.TodoLists
                .Where(l => l.UserId == request.UserId && l.IsDefault)
                .ToListAsync();

            foreach (var otherList in otherLists)
            {
                otherList.IsDefault = false;
            }

            list.IsDefault = true;
            list.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Set todo list {listId} as default");

            return Ok(new { success = true, data = list, message = "Standard-Liste gesetzt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting default list {listId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Reorder todo lists
    /// </summary>
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderLists([FromBody] ReorderListsRequest request)
    {
        try
        {
            var lists = await _context.TodoLists
                .Where(l => l.UserId == request.UserId && request.ListIds.Contains(l.Id))
                .ToListAsync();

            for (int i = 0; i < request.ListIds.Count; i++)
            {
                var list = lists.FirstOrDefault(l => l.Id == request.ListIds[i]);
                if (list != null)
                {
                    list.SortOrder = i;
                    list.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Reordered todo lists for user {request.UserId}");

            return Ok(new { success = true, message = "Reihenfolge aktualisiert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reordering lists for user {request.UserId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Initialize default lists for a new user (called after registration)
    /// </summary>
    [HttpPost("initialize/{userId}")]
    public async Task<IActionResult> InitializeDefaultLists(int userId)
    {
        try
        {
            // Check if user already has lists
            var existingLists = await _context.TodoLists
                .AnyAsync(l => l.UserId == userId);

            if (existingLists)
                return Ok(new { success = true, message = "Listen bereits vorhanden" });

            // Create default lists
            var defaultLists = new List<TodoList>
            {
                new TodoList
                {
                    UserId = userId,
                    Name = "Allgemein",
                    Icon = "mdi-checkbox-marked-circle-outline",
                    Color = "#1976D2",
                    SortOrder = 0,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                },
                new TodoList
                {
                    UserId = userId,
                    Name = "Studium",
                    Icon = "mdi-school",
                    Color = "#4CAF50",
                    SortOrder = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new TodoList
                {
                    UserId = userId,
                    Name = "Arbeit",
                    Icon = "mdi-briefcase",
                    Color = "#FF9800",
                    SortOrder = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new TodoList
                {
                    UserId = userId,
                    Name = "Archiv",
                    Icon = "mdi-archive",
                    Color = "#9E9E9E",
                    SortOrder = 99,
                    IsArchiveList = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.TodoLists.AddRange(defaultLists);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Initialized default lists for user {userId}");

            return Ok(new { success = true, data = defaultLists, message = "Standard-Listen erstellt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error initializing lists for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }
}

public class CreateTodoListRequest
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class UpdateTodoListRequest
{
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class SetDefaultListRequest
{
    public int UserId { get; set; }
}

public class ReorderListsRequest
{
    public int UserId { get; set; }
    public List<int> ListIds { get; set; } = new();
}
