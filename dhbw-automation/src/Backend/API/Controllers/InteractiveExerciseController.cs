using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DHBWAutomation.Backend.API.Controllers;

[ApiController]
[Route("api/exercises")]
public class InteractiveExerciseController : ControllerBase
{
    private readonly IInteractiveExerciseService _exerciseService;
    private readonly ILogger<InteractiveExerciseController> _logger;

    public InteractiveExerciseController(
        IInteractiveExerciseService exerciseService,
        ILogger<InteractiveExerciseController> logger)
    {
        _exerciseService = exerciseService;
        _logger = logger;
    }

    #region Interactive Exercises (Brilliant-Style)

    /// <summary>
    /// Generate a new interactive Brilliant-style exercise
    /// </summary>
    [HttpPost("interactive/generate")]
    public async Task<IActionResult> GenerateInteractiveExercise([FromBody] GenerateInteractiveRequest request)
    {
        try
        {
            var exercise = await _exerciseService.GenerateInteractiveExerciseAsync(
                request.UserId,
                request.Subject,
                request.Topic,
                request.Difficulty,
                request.DeficitId,
                request.PreferredComponentTypes);

            return Ok(new { success = true, data = exercise });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating interactive exercise");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get due interactive exercises for a user
    /// </summary>
    [HttpGet("interactive/due/{userId}")]
    public async Task<IActionResult> GetDueInteractiveExercises(int userId)
    {
        try
        {
            var exercises = await _exerciseService.GetDueInteractiveExercisesAsync(userId);
            return Ok(new { success = true, data = exercises });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting due interactive exercises");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Validate a step answer in an interactive exercise
    /// </summary>
    [HttpPost("interactive/{exerciseId}/steps/{stepId}/validate")]
    public async Task<IActionResult> ValidateStep(int exerciseId, string stepId, [FromBody] JsonElement answer)
    {
        try
        {
            var result = await _exerciseService.ValidateStepAsync(exerciseId, stepId, answer);
            return Ok(new { success = true, data = result });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating step");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Submit a step answer and update progress
    /// </summary>
    [HttpPost("interactive/{exerciseId}/steps/{stepId}/submit")]
    public async Task<IActionResult> SubmitStep(int exerciseId, string stepId, [FromBody] JsonElement answer)
    {
        try
        {
            // First validate
            var validationResult = await _exerciseService.ValidateStepAsync(exerciseId, stepId, answer);

            // Then update progress
            var exercise = await _exerciseService.UpdateStepProgressAsync(exerciseId, stepId, validationResult);

            return Ok(new
            {
                success = true,
                data = new
                {
                    validation = validationResult,
                    exercise = new
                    {
                        exercise.Id,
                        exercise.CompletedSteps,
                        exercise.TotalSteps,
                        exercise.Score,
                        IsComplete = exercise.CompletedSteps >= exercise.TotalSteps
                    }
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting step");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Complete an interactive exercise
    /// </summary>
    [HttpPost("interactive/{exerciseId}/complete")]
    public async Task<IActionResult> CompleteInteractiveExercise(int exerciseId)
    {
        try
        {
            var exercise = await _exerciseService.CompleteExerciseAsync(exerciseId);
            return Ok(new { success = true, data = exercise });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing exercise");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Classic Exercises (KA-Style)

    /// <summary>
    /// Generate a classic exam-prep exercise
    /// </summary>
    [HttpPost("classic/generate")]
    public async Task<IActionResult> GenerateClassicExercise([FromBody] GenerateClassicRequest request)
    {
        try
        {
            var exercise = await _exerciseService.GenerateExamPrepExerciseAsync(
                request.UserId,
                request.Subject,
                request.Topic,
                request.ExerciseMode,
                request.Difficulty,
                request.DeficitId,
                request.TimeLimitSeconds);

            return Ok(new { success = true, data = exercise });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating classic exercise");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get due classic exercises for a user
    /// </summary>
    [HttpGet("classic/due/{userId}")]
    public async Task<IActionResult> GetDueClassicExercises(int userId, [FromQuery] string? mode = null)
    {
        try
        {
            var exercises = await _exerciseService.GetDueClassicExercisesAsync(userId, mode);
            return Ok(new { success = true, data = exercises });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting due classic exercises");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Smart Generation (Auto-decides exercise type)

    /// <summary>
    /// Generate an exercise with automatic type selection based on context
    /// </summary>
    [HttpPost("smart/generate")]
    public async Task<IActionResult> GenerateSmartExercise([FromBody] GenerateSmartRequest request)
    {
        try
        {
            var exerciseType = _exerciseService.DetermineExerciseType(
                request.Difficulty,
                request.IsNewConcept,
                request.IsExamPrep);

            if (exerciseType == "interactive")
            {
                var exercise = await _exerciseService.GenerateInteractiveExerciseAsync(
                    request.UserId,
                    request.Subject,
                    request.Topic,
                    request.Difficulty,
                    request.DeficitId,
                    request.PreferredComponentTypes);

                return Ok(new { success = true, exerciseType = "interactive", data = exercise });
            }
            else
            {
                var mode = request.IsExamPrep ? "exam_prep" : "learning";
                var exercise = await _exerciseService.GenerateExamPrepExerciseAsync(
                    request.UserId,
                    request.Subject,
                    request.Topic,
                    mode,
                    request.Difficulty,
                    request.DeficitId,
                    request.TimeLimitSeconds);

                return Ok(new { success = true, exerciseType = "classic", data = exercise });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart exercise");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion
}

#region Request Models

public class GenerateInteractiveRequest
{
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "medium";
    public int? DeficitId { get; set; }
    public string[]? PreferredComponentTypes { get; set; }
}

public class GenerateClassicRequest
{
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string ExerciseMode { get; set; } = "learning"; // "learning", "exam_prep", "exam_simulation"
    public string Difficulty { get; set; } = "medium";
    public int? DeficitId { get; set; }
    public int? TimeLimitSeconds { get; set; }
}

public class GenerateSmartRequest
{
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "medium";
    public bool IsNewConcept { get; set; } = false;
    public bool IsExamPrep { get; set; } = false;
    public int? DeficitId { get; set; }
    public string[]? PreferredComponentTypes { get; set; }
    public int? TimeLimitSeconds { get; set; }
}

#endregion
