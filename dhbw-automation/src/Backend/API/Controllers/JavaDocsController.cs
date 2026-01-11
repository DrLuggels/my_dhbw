using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Services;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for Java-Docs exercises
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JavaDocsController : ControllerBase
{
    private readonly IJavaDocsScraperService _scraperService;
    private readonly ILogger<JavaDocsController> _logger;

    public JavaDocsController(
        IJavaDocsScraperService scraperService,
        ILogger<JavaDocsController> logger)
    {
        _scraperService = scraperService;
        _logger = logger;
    }

    /// <summary>
    /// Sync exercises from GitHub repository
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncExercises()
    {
        try
        {
            var result = await _scraperService.SyncExercisesToDatabaseAsync();

            return Ok(new
            {
                success = result.Success,
                added = result.Added,
                updated = result.Updated,
                unchanged = result.Unchanged,
                embeddingsGenerated = result.EmbeddingsGenerated,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Java-Docs exercises");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get all exercises with optional filtering
    /// </summary>
    [HttpGet("exercises")]
    public async Task<IActionResult> GetExercises(
        [FromQuery] string? topic = null,
        [FromQuery] string? difficulty = null,
        [FromQuery] string? q = null)
    {
        try
        {
            var exercises = await _scraperService.GetExercisesAsync(topic, difficulty, q);

            return Ok(exercises.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                topic = e.Topic,
                subtopic = e.Subtopic,
                difficulty = e.Difficulty,
                exerciseType = e.ExerciseType,
                practiceCount = e.PracticeCount,
                averageScore = e.AverageScore,
                hasEmbedding = e.HasEmbedding
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exercises");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific exercise by ID
    /// </summary>
    [HttpGet("exercises/{id}")]
    public async Task<IActionResult> GetExercise(int id)
    {
        try
        {
            var exercises = await _scraperService.GetExercisesAsync();
            var exercise = exercises.FirstOrDefault(e => e.Id == id);

            if (exercise == null)
            {
                return NotFound(new { message = "Exercise not found" });
            }

            return Ok(new
            {
                id = exercise.Id,
                title = exercise.Title,
                topic = exercise.Topic,
                subtopic = exercise.Subtopic,
                difficulty = exercise.Difficulty,
                exerciseType = exercise.ExerciseType,
                parsedContent = exercise.ParsedContent,
                codeSnippets = exercise.CodeSnippets,
                solutionCode = exercise.SolutionCode,
                tags = exercise.Tags,
                practiceCount = exercise.PracticeCount,
                averageScore = exercise.AverageScore
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exercise {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all available topics
    /// </summary>
    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics()
    {
        try
        {
            var topics = await _scraperService.GetTopicsAsync();
            return Ok(topics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting topics");
            return BadRequest(new { message = ex.Message });
        }
    }
}
