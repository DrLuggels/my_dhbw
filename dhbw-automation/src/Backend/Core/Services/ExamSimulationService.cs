using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for exam simulation with time pressure and no hints.
/// Provides realistic exam preparation experience.
/// </summary>
public class ExamSimulationService : IExamSimulationService
{
    private readonly AppDbContext _dbContext;
    private readonly IRagExerciseService _exerciseService;
    private readonly IPersonalKnowledgeGraphService _pkgService;
    private readonly AnthropicClient _anthropicClient;
    private readonly ILogger<ExamSimulationService> _logger;

    public ExamSimulationService(
        AppDbContext dbContext,
        IRagExerciseService exerciseService,
        IPersonalKnowledgeGraphService pkgService,
        AnthropicClient anthropicClient,
        ILogger<ExamSimulationService> logger)
    {
        _dbContext = dbContext;
        _exerciseService = exerciseService;
        _pkgService = pkgService;
        _anthropicClient = anthropicClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ExamSimulation> CreateExamAsync(
        int userId,
        string subject,
        int totalQuestions = 20,
        int timeLimitMinutes = 60,
        int? moodleAssignmentId = null)
    {
        // Calculate 20/40/40 distribution
        var easyCount = (int)(totalQuestions * 0.20);
        var mediumCount = (int)(totalQuestions * 0.40);
        var hardCount = totalQuestions - easyCount - mediumCount;

        var exam = new ExamSimulation
        {
            UserId = userId,
            Subject = subject,
            MoodleAssignmentId = moodleAssignmentId,
            TotalQuestions = totalQuestions,
            TimeLimitMinutes = timeLimitMinutes,
            EasyQuestions = easyCount,
            MediumQuestions = mediumCount,
            HardQuestions = hardCount,
            Status = ExamStatus.NotStarted
        };

        _dbContext.ExamSimulations.Add(exam);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created exam {ExamId} for user {UserId}: {Subject} ({Easy}/{Medium}/{Hard})",
            exam.Id, userId, subject, easyCount, mediumCount, hardCount);

        return exam;
    }

    /// <inheritdoc />
    public async Task<ExamSimulation> StartExamAsync(int examId)
    {
        var exam = await _dbContext.ExamSimulations.FindAsync(examId);
        if (exam == null)
        {
            throw new ArgumentException($"Exam {examId} not found");
        }

        if (exam.Status != ExamStatus.NotStarted && exam.Status != ExamStatus.Paused)
        {
            throw new InvalidOperationException($"Exam cannot be started (status: {exam.Status})");
        }

        // Generate all exam questions
        var exercises = await _exerciseService.GenerateExamExercisesAsync(
            exam.UserId,
            exam.Subject,
            exam.EasyQuestions,
            exam.MediumQuestions,
            exam.HardQuestions);

        // Store question IDs
        exam.QuestionIds = JsonSerializer.Serialize(exercises.Select(e => e.Id).ToList());

        // Store questions in a temporary cache (would be better in a separate table)
        // For now, we'll regenerate them as needed

        exam.Status = ExamStatus.InProgress;
        exam.StartedAt = DateTime.UtcNow;
        exam.RemainingSeconds = exam.TimeLimitMinutes * 60;
        exam.CurrentQuestionIndex = 0;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Started exam {ExamId} with {Count} questions", examId, exercises.Count);
        return exam;
    }

    /// <inheritdoc />
    public async Task<ExamQuestionDto?> GetCurrentQuestionAsync(int examId)
    {
        var exam = await _dbContext.ExamSimulations.FindAsync(examId);
        if (exam == null || exam.Status != ExamStatus.InProgress)
        {
            return null;
        }

        // Check if exam has expired
        if (exam.IsExpired)
        {
            await CompleteExamAsync(examId);
            return null;
        }

        // Get question IDs
        var questionIds = JsonSerializer.Deserialize<List<string>>(exam.QuestionIds) ?? new List<string>();

        if (exam.CurrentQuestionIndex >= questionIds.Count)
        {
            return null; // No more questions
        }

        // Calculate remaining time
        var elapsed = DateTime.UtcNow - exam.StartedAt!.Value;
        var remainingSeconds = Math.Max(0, exam.TimeLimitMinutes * 60 - (int)elapsed.TotalSeconds);

        // For simplicity, regenerate the question (in production, cache these)
        // This is a simplified implementation - in reality you'd store generated questions
        var nodes = await _dbContext.UserKnowledgeNodes
            .Where(n => n.UserId == exam.UserId && n.Subject == exam.Subject)
            .ToListAsync();

        if (nodes.Count == 0)
        {
            return null;
        }

        var nodeIndex = exam.CurrentQuestionIndex % nodes.Count;
        var node = nodes[nodeIndex];
        var difficulty = DetermineQuestionDifficulty(exam.CurrentQuestionIndex, exam);

        var exercise = await _exerciseService.GenerateExerciseAsync(
            exam.UserId,
            node.Id,
            difficulty,
            new ExerciseGenerationOptions
            {
                UseRag = true,
                IncludeHints = false, // No hints in exam mode!
                IncludeExplanation = false
            });

        return new ExamQuestionDto
        {
            QuestionNumber = exam.CurrentQuestionIndex + 1,
            TotalQuestions = exam.TotalQuestions,
            Question = exercise.Question,
            Options = exercise.Options,
            Difficulty = difficulty,
            RemainingSeconds = remainingSeconds,
            Subject = node.Subject,
            Topic = node.Topic
        };
    }

    /// <inheritdoc />
    public async Task<ExamAnswerResult> SubmitAnswerAsync(int examId, string answer)
    {
        var exam = await _dbContext.ExamSimulations.FindAsync(examId);
        if (exam == null || exam.Status != ExamStatus.InProgress)
        {
            throw new InvalidOperationException("Exam not in progress");
        }

        if (exam.IsExpired)
        {
            var result = await CompleteExamAsync(examId);
            return new ExamAnswerResult
            {
                IsCorrect = false,
                QuestionNumber = exam.CurrentQuestionIndex + 1,
                TotalQuestions = exam.TotalQuestions,
                CorrectSoFar = exam.CorrectAnswers,
                RemainingSeconds = 0,
                IsLastQuestion = true
            };
        }

        // Store user answer
        var userAnswers = JsonSerializer.Deserialize<List<string>>(exam.UserAnswers) ?? new List<string>();
        userAnswers.Add(answer);
        exam.UserAnswers = JsonSerializer.Serialize(userAnswers);

        // Simplified: Assume correct if answer is not empty (in reality, compare with stored correct answer)
        // This is a placeholder - in production, you'd compare against the stored correct answer
        var isCorrect = !string.IsNullOrWhiteSpace(answer) && answer.Length > 0;

        if (isCorrect)
        {
            exam.CorrectAnswers++;
        }
        else
        {
            exam.IncorrectAnswers++;
        }

        exam.CurrentQuestionIndex++;

        // Calculate remaining time
        var elapsed = DateTime.UtcNow - exam.StartedAt!.Value;
        var remainingSeconds = Math.Max(0, exam.TimeLimitMinutes * 60 - (int)elapsed.TotalSeconds);
        exam.RemainingSeconds = remainingSeconds;

        var isLastQuestion = exam.CurrentQuestionIndex >= exam.TotalQuestions;

        if (isLastQuestion)
        {
            await CompleteExamAsync(examId);
        }
        else
        {
            await _dbContext.SaveChangesAsync();
        }

        return new ExamAnswerResult
        {
            IsCorrect = isCorrect,
            QuestionNumber = exam.CurrentQuestionIndex,
            TotalQuestions = exam.TotalQuestions,
            CorrectSoFar = exam.CorrectAnswers,
            RemainingSeconds = remainingSeconds,
            IsLastQuestion = isLastQuestion
        };
    }

    /// <inheritdoc />
    public async Task<ExamAnswerResult> SkipQuestionAsync(int examId)
    {
        var exam = await _dbContext.ExamSimulations.FindAsync(examId);
        if (exam == null || exam.Status != ExamStatus.InProgress)
        {
            throw new InvalidOperationException("Exam not in progress");
        }

        // Store skipped answer
        var userAnswers = JsonSerializer.Deserialize<List<string>>(exam.UserAnswers) ?? new List<string>();
        userAnswers.Add("[SKIPPED]");
        exam.UserAnswers = JsonSerializer.Serialize(userAnswers);

        exam.SkippedAnswers++;
        exam.CurrentQuestionIndex++;

        var elapsed = DateTime.UtcNow - exam.StartedAt!.Value;
        var remainingSeconds = Math.Max(0, exam.TimeLimitMinutes * 60 - (int)elapsed.TotalSeconds);
        exam.RemainingSeconds = remainingSeconds;

        var isLastQuestion = exam.CurrentQuestionIndex >= exam.TotalQuestions;

        if (isLastQuestion)
        {
            await CompleteExamAsync(examId);
        }
        else
        {
            await _dbContext.SaveChangesAsync();
        }

        return new ExamAnswerResult
        {
            IsCorrect = false,
            QuestionNumber = exam.CurrentQuestionIndex,
            TotalQuestions = exam.TotalQuestions,
            CorrectSoFar = exam.CorrectAnswers,
            RemainingSeconds = remainingSeconds,
            IsLastQuestion = isLastQuestion
        };
    }

    /// <inheritdoc />
    public async Task<ExamResult> CompleteExamAsync(int examId)
    {
        var exam = await _dbContext.ExamSimulations.FindAsync(examId);
        if (exam == null)
        {
            throw new ArgumentException($"Exam {examId} not found");
        }

        exam.Status = ExamStatus.Completed;
        exam.CompletedAt = DateTime.UtcNow;
        exam.Score = exam.TotalQuestions > 0
            ? (double)exam.CorrectAnswers / exam.TotalQuestions * 100
            : 0;

        // Generate AI feedback
        exam.Feedback = await GenerateFeedbackAsync(exam);

        await _dbContext.SaveChangesAsync();

        // Calculate time taken
        var timeTaken = exam.CompletedAt.HasValue && exam.StartedAt.HasValue
            ? (int)(exam.CompletedAt.Value - exam.StartedAt.Value).TotalMinutes
            : 0;

        _logger.LogInformation(
            "Completed exam {ExamId}: Score {Score:F1}%, Time {Time}min",
            examId, exam.Score, timeTaken);

        return new ExamResult
        {
            ExamId = examId,
            Score = exam.Score,
            CorrectAnswers = exam.CorrectAnswers,
            TotalQuestions = exam.TotalQuestions,
            TimeTakenMinutes = timeTaken,
            Grade = CalculateGrade(exam.Score),
            Feedback = exam.Feedback ?? string.Empty
        };
    }

    /// <inheritdoc />
    public async Task<ExamProgress> GetExamProgressAsync(int examId)
    {
        var exam = await _dbContext.ExamSimulations.FindAsync(examId);
        if (exam == null)
        {
            throw new ArgumentException($"Exam {examId} not found");
        }

        var remainingSeconds = 0;
        if (exam.Status == ExamStatus.InProgress && exam.StartedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - exam.StartedAt.Value;
            remainingSeconds = Math.Max(0, exam.TimeLimitMinutes * 60 - (int)elapsed.TotalSeconds);
        }

        return new ExamProgress
        {
            ExamId = examId,
            Status = exam.Status,
            CurrentQuestion = exam.CurrentQuestionIndex + 1,
            TotalQuestions = exam.TotalQuestions,
            CorrectAnswers = exam.CorrectAnswers,
            IncorrectAnswers = exam.IncorrectAnswers,
            SkippedAnswers = exam.SkippedAnswers,
            RemainingSeconds = remainingSeconds,
            CurrentScore = exam.CorrectPercentage,
            IsExpired = exam.IsExpired
        };
    }

    /// <inheritdoc />
    public async Task<List<ExamSimulation>> GetUserExamsAsync(int userId, string? subject = null)
    {
        var query = _dbContext.ExamSimulations.Where(e => e.UserId == userId);

        if (!string.IsNullOrEmpty(subject))
        {
            query = query.Where(e => e.Subject == subject);
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CancelExamAsync(int examId)
    {
        var exam = await _dbContext.ExamSimulations.FindAsync(examId);
        if (exam == null)
        {
            return false;
        }

        exam.Status = ExamStatus.Cancelled;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<ExamResult?> GetExamResultAsync(int examId)
    {
        var exam = await _dbContext.ExamSimulations.FindAsync(examId);
        if (exam == null || exam.Status != ExamStatus.Completed)
        {
            return null;
        }

        var timeTaken = exam.CompletedAt.HasValue && exam.StartedAt.HasValue
            ? (int)(exam.CompletedAt.Value - exam.StartedAt.Value).TotalMinutes
            : 0;

        return new ExamResult
        {
            ExamId = examId,
            Score = exam.Score,
            CorrectAnswers = exam.CorrectAnswers,
            TotalQuestions = exam.TotalQuestions,
            TimeTakenMinutes = timeTaken,
            Grade = CalculateGrade(exam.Score),
            Feedback = exam.Feedback ?? string.Empty
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Determines difficulty for a question based on 20/40/40 distribution.
    /// </summary>
    private string DetermineQuestionDifficulty(int questionIndex, ExamSimulation exam)
    {
        if (questionIndex < exam.EasyQuestions)
            return "easy";
        if (questionIndex < exam.EasyQuestions + exam.MediumQuestions)
            return "medium";
        return "hard";
    }

    /// <summary>
    /// Calculates letter grade from percentage.
    /// </summary>
    private string CalculateGrade(double score)
    {
        return score switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            _ => "F"
        };
    }

    /// <summary>
    /// Generates AI feedback for the exam result.
    /// </summary>
    private async Task<string> GenerateFeedbackAsync(ExamSimulation exam)
    {
        try
        {
            var prompt = $@"Ein Student hat eine Prüfungssimulation im Fach '{exam.Subject}' absolviert.

Ergebnis:
- Richtige Antworten: {exam.CorrectAnswers} von {exam.TotalQuestions}
- Prozent: {exam.Score:F1}%
- Note: {CalculateGrade(exam.Score)}
- Zeitlimit: {exam.TimeLimitMinutes} Minuten
- Übersprungen: {exam.SkippedAnswers}

Erstelle ein kurzes, motivierendes Feedback (2-3 Sätze) mit:
1. Bewertung der Leistung
2. Einem konkreten Verbesserungsvorschlag
3. Ermutigung für weiteres Lernen

Antworte auf Deutsch.";

            return await _anthropicClient.ChatAsync(
                "Du bist ein freundlicher Tutor, der konstruktives Feedback gibt.",
                prompt,
                maxTokens: 256,
                temperature: 0.7);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exam feedback");
            return exam.Score >= 70
                ? "Gut gemacht! Du hast die Prüfung bestanden. Weiter so!"
                : "Du hast es versucht - übe weiter und du wirst dich verbessern!";
        }
    }

    #endregion
}
