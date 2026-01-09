using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DHBWAutomation.Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InteractionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<InteractionController> _logger;

    public InteractionController(AppDbContext context, ILogger<InteractionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all pending interactions for a user
    /// </summary>
    [HttpGet("pending/{userId}")]
    public async Task<IActionResult> GetPendingInteractions(int userId)
    {
        try
        {
            var interactions = await _context.UserInteractions
                .Where(i => i.UserId == userId && i.Status == "pending")
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();

            return Ok(new { success = true, data = interactions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting pending interactions for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get snoozed interactions that should be shown again
    /// </summary>
    [HttpGet("snoozed/{userId}")]
    public async Task<IActionResult> GetSnoozedInteractions(int userId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var interactions = await _context.UserInteractions
                .Where(i => i.UserId == userId &&
                           i.Status == "snoozed" &&
                           i.SnoozeUntil <= now)
                .OrderBy(i => i.SnoozeUntil)
                .ToListAsync();

            // Reactivate snoozed interactions
            foreach (var interaction in interactions)
            {
                interaction.Status = "pending";
                interaction.SnoozeUntil = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = interactions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting snoozed interactions for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Respond to an interaction
    /// </summary>
    [HttpPost("{interactionId}/respond")]
    public async Task<IActionResult> RespondToInteraction(
        int interactionId,
        [FromBody] InteractionResponse response)
    {
        try
        {
            var interaction = await _context.UserInteractions.FindAsync(interactionId);
            if (interaction == null)
            {
                return NotFound(new { success = false, message = "Interaktion nicht gefunden" });
            }

            // Handle different response types
            if (response.Action == "snooze")
            {
                interaction.Status = "snoozed";
                interaction.SnoozeUntil = DateTime.UtcNow.AddDays(response.SnoozeDays ?? 1);
                _logger.LogInformation($"Snoozed interaction {interactionId} until {interaction.SnoozeUntil}");
            }
            else if (response.Action == "dismiss")
            {
                interaction.Status = "dismissed";
                _logger.LogInformation($"Dismissed interaction {interactionId}");
            }
            else if (response.Action == "answer")
            {
                interaction.UserResponse = response.Answer;
                interaction.RespondedAt = DateTime.UtcNow;
                interaction.Status = "answered";

                // Process the response based on interaction type
                await ProcessInteractionResponseAsync(interaction, response.Answer);

                _logger.LogInformation($"Processed answer for interaction {interactionId}: {interaction.InteractionType}");
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Antwort gespeichert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error responding to interaction {interactionId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    private async Task ProcessInteractionResponseAsync(UserInteraction interaction, string? answer)
    {
        if (string.IsNullOrEmpty(answer))
            return;

        try
        {
            switch (interaction.InteractionType)
            {
                case "schedule_meeting":
                    // Parse meeting context and create calendar event
                    var meeting = JsonSerializer.Deserialize<ExtractedMeeting>(interaction.Context);
                    if (meeting != null)
                    {
                        // Would integrate with SchedulingService to create calendar event
                        _logger.LogInformation($"Meeting scheduling requested: {meeting.PersonName}");
                    }
                    break;

                case "new_project":
                    // Parse project context and create project
                    var project = JsonSerializer.Deserialize<ExtractedProject>(interaction.Context);
                    if (project != null && !answer.Contains("nicht so wichtig", StringComparison.OrdinalIgnoreCase))
                    {
                        var weeklyMinutes = answer.Contains("Hohe Priorität") ? 480 : // 8h
                                          answer.Contains("Mittlere Priorität") ? 360 : // 6h
                                          answer.Contains("Niedrige Priorität") ? 120 : // 2h
                                          answer.Contains("Spaß") ? 600 : // 10h
                                          240; // Default 4h

                        var newProject = new Project
                        {
                            UserId = interaction.UserId,
                            Name = project.Name,
                            Description = project.Description,
                            Priority = answer.Contains("Hohe") ? "high" : answer.Contains("Niedrige") ? "low" : "medium",
                            Interest = answer.Contains("Spaß") ? "high" : "medium",
                            Importance = answer.Contains("Hohe") ? "high" : "medium",
                            WeeklyMinutes = weeklyMinutes,
                            Status = "planning",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Projects.Add(newProject);
                        _logger.LogInformation($"Created new project: {project.Name}");
                    }
                    break;

                case "acknowledge_deficit":
                    // User wants exercises generated
                    if (answer.Contains("Ja", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("User requested exercise generation - LearningAnalyticsService would handle this");
                    }
                    break;

                default:
                    _logger.LogWarning($"Unknown interaction type: {interaction.InteractionType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing interaction response for {interaction.InteractionType}");
            // Don't throw - we already saved the response
        }
    }

    /// <summary>
    /// Get interaction history for a user
    /// </summary>
    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetInteractionHistory(
        int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var interactions = await _context.UserInteractions
                .Where(i => i.UserId == userId && i.Status != "pending")
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.UserInteractions
                .CountAsync(i => i.UserId == userId && i.Status != "pending");

            return Ok(new
            {
                success = true,
                data = interactions,
                pagination = new
                {
                    page,
                    pageSize,
                    total,
                    totalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting interaction history for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }
}

public class InteractionResponse
{
    public string Action { get; set; } = string.Empty; // "answer", "snooze", "dismiss"
    public string? Answer { get; set; }
    public int? SnoozeDays { get; set; } = 1;
}
