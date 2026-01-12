using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DHBWAutomation.Backend.Core.Services;
using DHBWAutomation.Backend.Infrastructure.Database;
using System.Security.Claims;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for Moodle integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MoodleController : ControllerBase
{
    private readonly IMoodleSyncService _syncService;
    private readonly AppDbContext _context;
    private readonly ILogger<MoodleController> _logger;

    public MoodleController(
        IMoodleSyncService syncService,
        AppDbContext context,
        ILogger<MoodleController> logger)
    {
        _syncService = syncService;
        _context = context;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Login to Moodle with username and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] MoodleLoginDto dto)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _syncService.LoginAsync(userId, dto.Username, dto.Password);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = "Login erfolgreich",
                moodleUserId = result.MoodleUserId,
                moodleUsername = result.MoodleUsername,
                moodleFullname = result.MoodleFullname
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Moodle login");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Test Moodle connection
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _syncService.TestConnectionAsync(userId);

            return Ok(new
            {
                success = result.Success,
                message = result.Success ? "Verbindung erfolgreich" : result.ErrorMessage,
                data = result.Success ? new
                {
                    siteName = result.SiteName,
                    username = result.Username,
                    fullname = result.Fullname,
                    moodleUserId = result.UserId
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Moodle connection");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Trigger full sync
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _syncService.FullSyncAsync(userId);

            return Ok(new
            {
                success = result.Success,
                message = result.Success ? "Synchronisation erfolgreich" : result.ErrorMessage,
                data = new
                {
                    courses = result.CoursesResult,
                    assignments = result.AssignmentsResult,
                    resources = result.ResourcesResult,
                    calendarEvents = result.CalendarEventsResult
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Moodle sync");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get sync status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var status = await _syncService.GetSyncStatusAsync(userId);

            return Ok(new
            {
                success = true,
                data = status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Moodle status");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get all courses
    /// </summary>
    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var courses = await _context.MoodleCourses
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Fullname)
                .Select(c => new
                {
                    c.Id,
                    c.MoodleCourseId,
                    c.Shortname,
                    c.Fullname,
                    c.StartDate,
                    c.EndDate,
                    c.Progress,
                    c.LastSynced
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = courses
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Moodle courses");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get assignments for a course or all
    /// </summary>
    [HttpGet("courses/{courseId}/assignments")]
    public async Task<IActionResult> GetAssignments(int courseId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var query = _context.MoodleAssignments
                .Where(a => a.UserId == userId);

            if (courseId > 0)
            {
                query = query.Where(a => a.CourseId == courseId);
            }

            var assignments = await query
                .OrderBy(a => a.DueDate)
                .Select(a => new
                {
                    a.Id,
                    a.MoodleAssignmentId,
                    a.CourseId,
                    a.CourseName,
                    a.Title,
                    a.Description,
                    a.DueDate,
                    a.CutoffDate,
                    a.MaxGrade,
                    a.Grade,
                    a.IsSubmitted,
                    a.SubmissionStatus,
                    a.SyncedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = assignments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Moodle assignments");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get all assignments
    /// </summary>
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAllAssignments([FromQuery] bool? pendingOnly = null)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var query = _context.MoodleAssignments
                .Where(a => a.UserId == userId);

            if (pendingOnly == true)
            {
                query = query.Where(a => !a.IsSubmitted && a.DueDate > DateTime.UtcNow);
            }

            var assignments = await query
                .OrderBy(a => a.DueDate)
                .Select(a => new
                {
                    a.Id,
                    a.MoodleAssignmentId,
                    a.CourseId,
                    a.CourseName,
                    a.Title,
                    a.Description,
                    a.DueDate,
                    a.CutoffDate,
                    a.MaxGrade,
                    a.Grade,
                    a.IsSubmitted,
                    a.SubmissionStatus,
                    a.SyncedAt,
                    daysUntilDue = a.DueDate.HasValue
                        ? (int)(a.DueDate.Value - DateTime.UtcNow).TotalDays
                        : (int?)null
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = assignments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Moodle assignments");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get resources for a course
    /// </summary>
    [HttpGet("courses/{courseId}/resources")]
    public async Task<IActionResult> GetResources(int courseId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var query = _context.MoodleResources
                .Where(r => r.UserId == userId);

            if (courseId > 0)
            {
                query = query.Where(r => r.CourseId == courseId);
            }

            var resources = await query
                .OrderBy(r => r.SectionNumber)
                .ThenBy(r => r.Title)
                .Select(r => new
                {
                    r.Id,
                    r.MoodleResourceId,
                    r.CourseId,
                    r.CourseName,
                    r.ResourceType,
                    r.Title,
                    r.Description,
                    r.DownloadUrl,
                    r.ExternalUrl,
                    r.FileType,
                    r.FileSize,
                    r.SectionNumber,
                    r.SectionName,
                    r.IsDownloaded,
                    r.SyncedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = resources
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Moodle resources");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get all resources
    /// </summary>
    [HttpGet("resources")]
    public async Task<IActionResult> GetAllResources([FromQuery] string? resourceType = null)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var query = _context.MoodleResources
                .Where(r => r.UserId == userId);

            if (!string.IsNullOrEmpty(resourceType))
            {
                query = query.Where(r => r.ResourceType == resourceType);
            }

            var resources = await query
                .OrderBy(r => r.CourseName)
                .ThenBy(r => r.SectionNumber)
                .ThenBy(r => r.Title)
                .Select(r => new
                {
                    r.Id,
                    r.MoodleResourceId,
                    r.CourseId,
                    r.CourseName,
                    r.ResourceType,
                    r.Title,
                    r.FileType,
                    r.FileSize,
                    r.SectionName,
                    r.IsDownloaded,
                    r.SyncedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = resources
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Moodle resources");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get calendar events
    /// </summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendarEvents([FromQuery] int? days = 30)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(days ?? 30);

            var events = await _context.MoodleCalendarEvents
                .Where(e => e.UserId == userId && e.TimeStart >= startDate && e.TimeStart <= endDate)
                .OrderBy(e => e.TimeStart)
                .Select(e => new
                {
                    e.Id,
                    e.MoodleEventId,
                    e.CourseId,
                    e.CourseName,
                    e.Name,
                    e.Description,
                    e.EventType,
                    e.ModuleName,
                    e.TimeStart,
                    e.TimeDuration,
                    e.CalendarEventId,
                    e.SyncedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Moodle calendar events");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Disable Moodle sync
    /// </summary>
    [HttpPost("disable")]
    public async Task<IActionResult> DisableSync()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.MoodleSyncEnabled = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Moodle-Sync deaktiviert"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling Moodle sync");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Enable Moodle sync
    /// </summary>
    [HttpPost("enable")]
    public async Task<IActionResult> EnableSync()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(user.MoodleToken))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bitte zuerst mit Moodle anmelden"
                });
            }

            user.MoodleSyncEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Moodle-Sync aktiviert"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling Moodle sync");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

#region DTOs

public class MoodleLoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

#endregion
