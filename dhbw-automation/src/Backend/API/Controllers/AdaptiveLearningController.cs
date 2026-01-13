using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for adaptive learning operations.
/// Handles exercise generation, priority-based recommendations, and difficulty adaptation.
/// </summary>
[Authorize]
[ApiController]
[Route("api/adaptive")]
public class AdaptiveLearningController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPersonalKnowledgeGraphService _pkgService;
    private readonly IAdaptiveDifficultyService _difficultyService;
    private readonly IDeadlinePriorityService _priorityService;
    private readonly IRagExerciseService _exerciseService;
    private readonly ILogger<AdaptiveLearningController> _logger;

    public AdaptiveLearningController(
        AppDbContext context,
        IPersonalKnowledgeGraphService pkgService,
        IAdaptiveDifficultyService difficultyService,
        IDeadlinePriorityService priorityService,
        IRagExerciseService exerciseService,
        ILogger<AdaptiveLearningController> logger)
    {
        _context = context;
        _pkgService = pkgService;
        _difficultyService = difficultyService;
        _priorityService = priorityService;
        _exerciseService = exerciseService;
        _logger = logger;
    }

    /// <summary>
    /// Get the next recommended exercise based on priorities.
    /// </summary>
    [HttpGet("{userId}/next")]
    public async Task<IActionResult> GetNextExercise(int userId)
    {
        try
        {
            // Get top priority recommendation
            var recommendations = await _priorityService.GetRecommendedTopicsAsync(userId, 1);
            if (recommendations.Count == 0)
            {
                return Ok(new { success = false, message = "Keine Lernthemen gefunden" });
            }

            var recommendation = recommendations[0];

            // Select adaptive difficulty
            var difficultySelection = await _difficultyService.SelectDifficultyAsync(userId, recommendation.NodeId);

            // Generate exercise
            var exercise = await _exerciseService.GenerateExerciseAsync(
                userId,
                recommendation.NodeId,
                difficultySelection.Difficulty,
                new ExerciseGenerationOptions { UseRag = true, IncludeHints = true });

            return Ok(new
            {
                success = true,
                data = new
                {
                    exercise,
                    recommendation,
                    difficultySelection
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next exercise for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Generieren der Übung" });
        }
    }

    /// <summary>
    /// Start a learning session with multiple exercises.
    /// </summary>
    [HttpGet("{userId}/session")]
    public async Task<IActionResult> StartSession(int userId, [FromQuery] int count = 5)
    {
        try
        {
            var exercises = await _exerciseService.GenerateSessionExercisesAsync(userId, count);

            return Ok(new
            {
                success = true,
                data = new
                {
                    sessionId = Guid.NewGuid().ToString(),
                    exerciseCount = exercises.Count,
                    exercises,
                    startedAt = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting session for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Starten der Lernsession" });
        }
    }

    /// <summary>
    /// Submit an answer and get feedback with graph reinforcement.
    /// </summary>
    [HttpPost("{userId}/answer")]
    public async Task<IActionResult> SubmitAnswer(int userId, [FromBody] AnswerSubmission submission)
    {
        try
        {
            // Record exercise result in knowledge graph
            var impact = await _pkgService.RecordExerciseResultAsync(
                userId,
                submission.NodeId,
                submission.IsCorrect,
                submission.Difficulty,
                submission.ResponseTimeSeconds);

            // Update difficulty statistics
            await _difficultyService.UpdateDifficultyStatsAsync(
                submission.NodeId,
                submission.Difficulty,
                submission.IsCorrect,
                submission.ResponseTimeSeconds);

            return Ok(new
            {
                success = true,
                data = new
                {
                    isCorrect = submission.IsCorrect,
                    impact,
                    message = impact.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Speichern der Antwort" });
        }
    }

    /// <summary>
    /// Get current learning priorities.
    /// </summary>
    [HttpGet("{userId}/priorities")]
    public async Task<IActionResult> GetPriorities(int userId, [FromQuery] int topN = 10)
    {
        try
        {
            var recommendations = await _priorityService.GetRecommendedTopicsAsync(userId, topN);
            return Ok(new { success = true, data = recommendations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting priorities for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der Prioritäten" });
        }
    }

    /// <summary>
    /// Get upcoming deadlines with linked topics.
    /// </summary>
    [HttpGet("{userId}/deadlines")]
    public async Task<IActionResult> GetDeadlines(int userId, [FromQuery] int days = 30)
    {
        try
        {
            var deadlines = await _priorityService.GetUpcomingDeadlinesAsync(userId, days);
            return Ok(new { success = true, data = deadlines });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deadlines for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der Deadlines" });
        }
    }

    /// <summary>
    /// Get current difficulty distribution.
    /// </summary>
    [HttpGet("{userId}/distribution")]
    public async Task<IActionResult> GetDifficultyDistribution(int userId, [FromQuery] string? subject = null)
    {
        try
        {
            var distribution = await _difficultyService.GetDistributionAsync(userId, subject);

            return Ok(new
            {
                success = true,
                data = new
                {
                    distribution,
                    target = new { easy = 0.20, medium = 0.40, hard = 0.40 },
                    needsRebalancing = await _difficultyService.NeedsRebalancingAsync(userId, subject)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distribution for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der Verteilung" });
        }
    }

    /// <summary>
    /// Manually select difficulty for an exercise.
    /// </summary>
    [HttpGet("{userId}/difficulty/{nodeId}")]
    public async Task<IActionResult> SelectDifficulty(int userId, int nodeId)
    {
        try
        {
            var selection = await _difficultyService.SelectDifficultyAsync(userId, nodeId);
            return Ok(new { success = true, data = selection });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting difficulty for node {NodeId}", nodeId);
            return StatusCode(500, new { success = false, message = "Fehler bei der Schwierigkeitsauswahl" });
        }
    }

    /// <summary>
    /// Refresh all priorities for a user.
    /// </summary>
    [HttpPost("{userId}/refresh-priorities")]
    public async Task<IActionResult> RefreshPriorities(int userId)
    {
        try
        {
            await _priorityService.RefreshPrioritiesAsync(userId);
            var priorities = await _priorityService.GetRecommendedTopicsAsync(userId, 10);

            return Ok(new
            {
                success = true,
                message = "Prioritäten wurden aktualisiert",
                data = priorities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing priorities for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Aktualisieren der Prioritäten" });
        }
    }

    /// <summary>
    /// Generate a specific exercise for a topic.
    /// </summary>
    [HttpPost("{userId}/exercise")]
    public async Task<IActionResult> GenerateExercise(int userId, [FromBody] AdaptiveExerciseRequest request)
    {
        try
        {
            var options = new ExerciseGenerationOptions
            {
                UseRag = request.UseRag,
                ExerciseType = request.ExerciseType ?? "multiple_choice",
                IncludeHints = request.IncludeHints,
                IncludeExplanation = request.IncludeExplanation,
                MaxContextChunks = request.MaxContextChunks ?? 5
            };

            RagExerciseResult exercise;
            if (request.NodeId.HasValue)
            {
                exercise = await _exerciseService.GenerateExerciseAsync(
                    userId, request.NodeId.Value, request.Difficulty, options);
            }
            else
            {
                exercise = await _exerciseService.GenerateExerciseForTopicAsync(
                    userId, request.Subject, request.Topic, request.Difficulty, options);
            }

            return Ok(new { success = true, data = exercise });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exercise for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Generieren der Übung" });
        }
    }

    /// <summary>
    /// Get the learning streak for a user.
    /// </summary>
    [HttpGet("{userId}/streak")]
    public async Task<IActionResult> GetStreak(int userId)
    {
        try
        {
            var streak = await _context.LearningStreaks
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (streak == null)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        currentStreak = 0,
                        longestStreak = 0,
                        multiplier = 1.0,
                        freezesAvailable = 1,
                        totalExercises = 0,
                        totalActiveDays = 0
                    }
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    currentStreak = streak.CurrentStreak,
                    longestStreak = streak.LongestStreak,
                    multiplier = streak.StreakMultiplier,
                    freezesAvailable = streak.StreakFreezes,
                    lastActivity = streak.LastActivityDate,
                    totalExercises = streak.TotalExercisesCompleted,
                    totalActiveDays = streak.TotalActiveDays
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting streak for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen des Streaks" });
        }
    }
}

/// <summary>
/// Request to submit an answer.
/// </summary>
public class AnswerSubmission
{
    public int NodeId { get; set; }
    public bool IsCorrect { get; set; }
    public string Difficulty { get; set; } = "medium";
    public double? ResponseTimeSeconds { get; set; }
    public string? UserAnswer { get; set; }
}

/// <summary>
/// Request to generate an exercise.
/// </summary>
public class AdaptiveExerciseRequest
{
    public int? NodeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "medium";
    public string? ExerciseType { get; set; }
    public bool UseRag { get; set; } = true;
    public bool IncludeHints { get; set; } = true;
    public bool IncludeExplanation { get; set; } = true;
    public int? MaxContextChunks { get; set; }
}
