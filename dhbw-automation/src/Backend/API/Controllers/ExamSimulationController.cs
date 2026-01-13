using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for exam simulation operations.
/// Provides timed exam experience with no hints.
/// </summary>
[Authorize]
[ApiController]
[Route("api/exam")]
public class ExamSimulationController : ControllerBase
{
    private readonly IExamSimulationService _examService;
    private readonly ILogger<ExamSimulationController> _logger;

    public ExamSimulationController(
        IExamSimulationService examService,
        ILogger<ExamSimulationController> logger)
    {
        _examService = examService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new exam simulation.
    /// </summary>
    [HttpPost("{userId}/create")]
    public async Task<IActionResult> CreateExam(int userId, [FromBody] CreateExamRequest request)
    {
        try
        {
            var exam = await _examService.CreateExamAsync(
                userId,
                request.Subject,
                request.TotalQuestions,
                request.TimeLimitMinutes,
                request.MoodleAssignmentId);

            return Ok(new { success = true, data = exam });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exam for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Erstellen der Prüfung" });
        }
    }

    /// <summary>
    /// Start an exam simulation.
    /// </summary>
    [HttpPost("{examId}/start")]
    public async Task<IActionResult> StartExam(int examId)
    {
        try
        {
            var exam = await _examService.StartExamAsync(examId);
            return Ok(new { success = true, data = exam });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting exam {ExamId}", examId);
            return StatusCode(500, new { success = false, message = "Fehler beim Starten der Prüfung" });
        }
    }

    /// <summary>
    /// Get the current question.
    /// </summary>
    [HttpGet("{examId}/question")]
    public async Task<IActionResult> GetCurrentQuestion(int examId)
    {
        try
        {
            var question = await _examService.GetCurrentQuestionAsync(examId);
            if (question == null)
            {
                return Ok(new { success = false, message = "Keine weiteren Fragen" });
            }
            return Ok(new { success = true, data = question });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question for exam {ExamId}", examId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der Frage" });
        }
    }

    /// <summary>
    /// Submit an answer.
    /// </summary>
    [HttpPost("{examId}/answer")]
    public async Task<IActionResult> SubmitAnswer(int examId, [FromBody] SubmitExamAnswerRequest request)
    {
        try
        {
            var result = await _examService.SubmitAnswerAsync(examId, request.Answer);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for exam {ExamId}", examId);
            return StatusCode(500, new { success = false, message = "Fehler beim Einreichen der Antwort" });
        }
    }

    /// <summary>
    /// Skip the current question.
    /// </summary>
    [HttpPost("{examId}/skip")]
    public async Task<IActionResult> SkipQuestion(int examId)
    {
        try
        {
            var result = await _examService.SkipQuestionAsync(examId);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error skipping question for exam {ExamId}", examId);
            return StatusCode(500, new { success = false, message = "Fehler beim Überspringen der Frage" });
        }
    }

    /// <summary>
    /// Get exam progress.
    /// </summary>
    [HttpGet("{examId}/progress")]
    public async Task<IActionResult> GetProgress(int examId)
    {
        try
        {
            var progress = await _examService.GetExamProgressAsync(examId);
            return Ok(new { success = true, data = progress });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting progress for exam {ExamId}", examId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen des Fortschritts" });
        }
    }

    /// <summary>
    /// Complete the exam manually.
    /// </summary>
    [HttpPost("{examId}/complete")]
    public async Task<IActionResult> CompleteExam(int examId)
    {
        try
        {
            var result = await _examService.CompleteExamAsync(examId);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing exam {ExamId}", examId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abschließen der Prüfung" });
        }
    }

    /// <summary>
    /// Get exam result.
    /// </summary>
    [HttpGet("{examId}/result")]
    public async Task<IActionResult> GetResult(int examId)
    {
        try
        {
            var result = await _examService.GetExamResultAsync(examId);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Prüfungsergebnis nicht gefunden" });
            }
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting result for exam {ExamId}", examId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen des Ergebnisses" });
        }
    }

    /// <summary>
    /// Get all exams for a user.
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserExams(int userId, [FromQuery] string? subject = null)
    {
        try
        {
            var exams = await _examService.GetUserExamsAsync(userId, subject);
            return Ok(new { success = true, data = exams });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exams for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abrufen der Prüfungen" });
        }
    }

    /// <summary>
    /// Cancel an exam.
    /// </summary>
    [HttpPost("{examId}/cancel")]
    public async Task<IActionResult> CancelExam(int examId)
    {
        try
        {
            var success = await _examService.CancelExamAsync(examId);
            if (!success)
            {
                return NotFound(new { success = false, message = "Prüfung nicht gefunden" });
            }
            return Ok(new { success = true, message = "Prüfung abgebrochen" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling exam {ExamId}", examId);
            return StatusCode(500, new { success = false, message = "Fehler beim Abbrechen der Prüfung" });
        }
    }
}

/// <summary>
/// Request to create an exam.
/// </summary>
public class CreateExamRequest
{
    public string Subject { get; set; } = string.Empty;
    public int TotalQuestions { get; set; } = 20;
    public int TimeLimitMinutes { get; set; } = 60;
    public int? MoodleAssignmentId { get; set; }
}

/// <summary>
/// Request to submit an exam answer.
/// </summary>
public class SubmitExamAnswerRequest
{
    public string Answer { get; set; } = string.Empty;
}
