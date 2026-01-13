using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for exam simulation with time pressure and no hints.
/// Provides realistic exam preparation experience.
/// </summary>
public interface IExamSimulationService
{
    /// <summary>
    /// Creates a new exam simulation.
    /// </summary>
    Task<ExamSimulation> CreateExamAsync(
        int userId,
        string subject,
        int totalQuestions = 20,
        int timeLimitMinutes = 60,
        int? moodleAssignmentId = null);

    /// <summary>
    /// Starts an exam simulation.
    /// </summary>
    Task<ExamSimulation> StartExamAsync(int examId);

    /// <summary>
    /// Gets the current question for an exam.
    /// </summary>
    Task<ExamQuestionDto?> GetCurrentQuestionAsync(int examId);

    /// <summary>
    /// Submits an answer for the current question.
    /// </summary>
    Task<ExamAnswerResult> SubmitAnswerAsync(int examId, string answer);

    /// <summary>
    /// Skips the current question.
    /// </summary>
    Task<ExamAnswerResult> SkipQuestionAsync(int examId);

    /// <summary>
    /// Completes an exam and generates results.
    /// </summary>
    Task<ExamResult> CompleteExamAsync(int examId);

    /// <summary>
    /// Gets exam status and progress.
    /// </summary>
    Task<ExamProgress> GetExamProgressAsync(int examId);

    /// <summary>
    /// Gets all exams for a user.
    /// </summary>
    Task<List<ExamSimulation>> GetUserExamsAsync(int userId, string? subject = null);

    /// <summary>
    /// Cancels an in-progress exam.
    /// </summary>
    Task<bool> CancelExamAsync(int examId);

    /// <summary>
    /// Gets the full exam result with question reviews.
    /// </summary>
    Task<ExamResult?> GetExamResultAsync(int examId);
}

/// <summary>
/// An exam question without hints.
/// </summary>
public class ExamQuestionDto
{
    public int QuestionNumber { get; set; }
    public int TotalQuestions { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public int RemainingSeconds { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}

/// <summary>
/// Current exam progress.
/// </summary>
public class ExamProgress
{
    public int ExamId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentQuestion { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public int SkippedAnswers { get; set; }
    public int RemainingSeconds { get; set; }
    public double CurrentScore { get; set; }
    public bool IsExpired { get; set; }
}
