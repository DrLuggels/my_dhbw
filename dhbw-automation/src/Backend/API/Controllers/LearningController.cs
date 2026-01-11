using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LearningController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILearningAnalyticsService _learningService;
    private readonly ISchedulingService _schedulingService;
    private readonly ILogger<LearningController> _logger;

    public LearningController(
        AppDbContext context,
        ILearningAnalyticsService learningService,
        ISchedulingService schedulingService,
        ILogger<LearningController> logger)
    {
        _context = context;
        _learningService = learningService;
        _schedulingService = schedulingService;
        _logger = logger;
    }

    /// <summary>
    /// Get all learning deficits for a user
    /// </summary>
    [HttpGet("deficits/{userId}")]
    public async Task<IActionResult> GetLearningDeficits(int userId)
    {
        try
        {
            var deficits = await _learningService.GetActiveDeficitsAsync(userId);

            return Ok(new { success = true, data = deficits });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting learning deficits for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get exercises due for review
    /// </summary>
    [HttpGet("exercises/due/{userId}")]
    public async Task<IActionResult> GetDueExercises(int userId)
    {
        try
        {
            var exercises = await _context.GeneratedExercises
                .Where(e => e.UserId == userId &&
                           e.NextReviewDate <= DateTime.UtcNow &&
                           e.IsCorrect != true)
                .OrderBy(e => e.NextReviewDate)
                .ToListAsync();

            return Ok(new { success = true, data = exercises });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting due exercises for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get all exercises for a user
    /// </summary>
    [HttpGet("exercises/user/{userId}")]
    public async Task<IActionResult> GetUserExercises(
        int userId,
        [FromQuery] string? subject = null,
        [FromQuery] bool? correctOnly = null)
    {
        try
        {
            var query = _context.GeneratedExercises.Where(e => e.UserId == userId);

            if (!string.IsNullOrEmpty(subject))
                query = query.Where(e => e.Subject == subject);

            if (correctOnly.HasValue)
                query = query.Where(e => e.IsCorrect == correctOnly.Value);

            var exercises = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Ok(new { success = true, data = exercises });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting exercises for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Submit answer to an exercise
    /// </summary>
    [HttpPost("exercises/{exerciseId}/answer")]
    public async Task<IActionResult> SubmitExerciseAnswer(
        int exerciseId,
        [FromBody] ExerciseAnswerRequest request)
    {
        try
        {
            var exercise = await _context.GeneratedExercises.FindAsync(exerciseId);
            if (exercise == null)
                return NotFound(new { success = false, message = "Übung nicht gefunden" });

            if (exercise.UserId != request.UserId)
                return Forbid();

            // Check if answer is correct (compare user answer with stored correct answer)
            var userAnswer = request.Answer?.Trim().ToLowerInvariant() ?? "";
            var correctAnswer = exercise.CorrectAnswer?.Trim().ToLowerInvariant() ?? "";

            // Remove JSON quotes if present (CorrectAnswer is stored as JSON string)
            if (correctAnswer.StartsWith("\"") && correctAnswer.EndsWith("\""))
                correctAnswer = correctAnswer[1..^1];

            // Simple comparison - check if user answer contains the correct answer or vice versa
            var isCorrect = userAnswer == correctAnswer ||
                            correctAnswer.Contains(userAnswer) ||
                            userAnswer.Contains(correctAnswer);

            // Update exercise progress using SM-2 algorithm
            await _learningService.UpdateExerciseProgressAsync(exerciseId, request.Answer, isCorrect);

            // Reload exercise to get updated values
            await _context.Entry(exercise).ReloadAsync();

            var responseData = new
            {
                isCorrect = exercise.IsCorrect,
                explanation = exercise.Explanation,
                nextReviewDate = exercise.NextReviewDate,
                reviewCount = exercise.ReviewCount
            };

            return Ok(new
            {
                success = true,
                data = responseData,
                message = exercise.IsCorrect == true ? "Richtig! 🎉" : "Nicht ganz richtig. Versuch's nochmal!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error submitting exercise answer {exerciseId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Schedule tutoring sessions for a deficit
    /// </summary>
    [HttpPost("schedule-tutoring/{deficitId}")]
    public async Task<IActionResult> ScheduleTutoringSession(
        int deficitId,
        [FromQuery] int userId)
    {
        try
        {
            var deficit = await _context.LearningDeficits.FindAsync(deficitId);
            if (deficit == null)
                return NotFound(new { success = false, message = "Lerndefizit nicht gefunden" });

            if (deficit.UserId != userId)
                return Forbid();

            _logger.LogInformation($"Scheduling tutoring for deficit {deficitId}");

            // Generate 20 exercises with difficulty progression
            // Mix: 5 easy (foundations) -> 10 medium (practice) -> 5 hard (mastery)
            var exercises = new List<GeneratedExercise>();

            _logger.LogInformation("Generating 20 exercises: 5 easy, 10 medium, 5 hard");

            // Total: 20 exercises for comprehensive practice
            for (int i = 0; i < 20; i++)
            {
                // Determine difficulty based on progression
                string difficulty;
                if (i < 5)
                    difficulty = "easy";       // First 5: build foundations
                else if (i < 15)
                    difficulty = "medium";     // Next 10: practice and reinforce
                else
                    difficulty = "hard";       // Last 5: challenge and test mastery

                var exercise = await _learningService.GenerateExerciseForDeficitAsync(deficitId, difficulty);
                exercises.Add(exercise);

                _logger.LogInformation($"Generated {difficulty} exercise {i + 1}/20");
            }

            // Plan learning sessions (2 hours total for 20 exercises)
            var learningSessions = await _schedulingService.ScheduleLearningSessionsAsync(
                userId,
                deficit.Subject,
                totalMinutes: 120,
                deadline: DateTime.UtcNow.AddDays(7)
            );

            // Create calendar events for learning sessions
            foreach (var session in learningSessions)
            {
                var calendarEvent = new CalendarEvent
                {
                    UserId = userId,
                    Title = $"Lernen: {deficit.Subject} - {deficit.Topic}",
                    Description = $"Übungen zu: {deficit.ErrorDescription}",
                    StartTime = session.Start,
                    EndTime = session.End,
                    EventType = "learning",
                    Subject = deficit.Subject,
                    Source = "ai_generated",
                    CreatedAt = DateTime.UtcNow
                };
                _context.CalendarEvents.Add(calendarEvent);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created {exercises.Count} exercises and {learningSessions.Count} learning sessions");

            return Ok(new
            {
                success = true,
                exercises = exercises.Count,
                sessions = learningSessions.Count,
                message = $"{exercises.Count} Übungen generiert und {learningSessions.Count} Lernzeiten eingeplant!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error scheduling tutoring for deficit {deficitId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Mark a deficit as resolved
    /// </summary>
    [HttpPatch("deficits/{deficitId}/resolve")]
    public async Task<IActionResult> ResolveDeficit(
        int deficitId,
        [FromQuery] int userId)
    {
        try
        {
            var deficit = await _context.LearningDeficits.FindAsync(deficitId);
            if (deficit == null)
                return NotFound(new { success = false, message = "Lerndefizit nicht gefunden" });

            if (deficit.UserId != userId)
                return Forbid();

            deficit.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Resolved deficit {deficitId}");

            return Ok(new { success = true, message = "Defizit als behoben markiert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error resolving deficit {deficitId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Get learning statistics for a user
    /// </summary>
    [HttpGet("stats/{userId}")]
    public async Task<IActionResult> GetLearningStats(int userId)
    {
        try
        {
            var deficits = await _context.LearningDeficits
                .Where(d => d.UserId == userId)
                .ToListAsync();

            var exercises = await _context.GeneratedExercises
                .Where(e => e.UserId == userId)
                .ToListAsync();

            var stats = new
            {
                totalDeficits = deficits.Count,
                activeDeficits = deficits.Count(d => d.ResolvedAt == null),
                resolvedDeficits = deficits.Count(d => d.ResolvedAt != null),
                highSeverityDeficits = deficits.Count(d => d.Severity == "high" && d.ResolvedAt == null),
                totalExercises = exercises.Count,
                completedExercises = exercises.Count(e => e.IsCorrect == true),
                pendingExercises = exercises.Count(e => e.IsCorrect != true),
                dueExercises = exercises.Count(e => e.NextReviewDate <= DateTime.UtcNow && e.IsCorrect != true),
                averageEaseFactor = exercises.Any() ? exercises.Average(e => e.EaseFactor) : 2.5
            };

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting learning stats for user {userId}");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }

    /// <summary>
    /// Generate a single exercise for a subject/topic
    /// </summary>
    [HttpPost("exercises/generate")]
    public async Task<IActionResult> GenerateExercise([FromBody] GenerateExerciseRequest request)
    {
        try
        {
            // Find or create deficit
            var deficit = await _context.LearningDeficits
                .FirstOrDefaultAsync(d =>
                    d.UserId == request.UserId &&
                    d.Subject == request.Subject &&
                    d.Topic == request.Topic &&
                    d.ResolvedAt == null);

            if (deficit == null)
            {
                // Create temporary deficit
                deficit = new LearningDeficit
                {
                    UserId = request.UserId,
                    Subject = request.Subject,
                    Topic = request.Topic,
                    ErrorType = "general",
                    ErrorDescription = $"Übung für {request.Topic}",
                    OccurrenceCount = 1,
                    FirstOccurrence = DateTime.UtcNow,
                    LastOccurrence = DateTime.UtcNow,
                    Severity = "low",
                    RelatedDocumentIds = "[]"
                };
                _context.LearningDeficits.Add(deficit);
                await _context.SaveChangesAsync();
            }

            var exercise = await _learningService.GenerateExerciseForDeficitAsync(deficit.Id);

            return Ok(new
            {
                success = true,
                data = exercise,
                message = "Übung generiert"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exercise");
            return StatusCode(500, new { success = false, message = "Interner Fehler" });
        }
    }
}

public class ExerciseAnswerRequest
{
    public int UserId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class GenerateExerciseRequest
{
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}
