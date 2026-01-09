using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface ILearningAnalyticsService
{
    /// <summary>
    /// Analyzes errors in a document and creates/updates LearningDeficits
    /// </summary>
    Task AnalyzeDocumentErrorsAsync(int documentId);

    /// <summary>
    /// Gets all active learning deficits for a user
    /// </summary>
    Task<List<LearningDeficit>> GetActiveDeficitsAsync(int userId);

    /// <summary>
    /// Determines if a user needs tutoring based on error frequency
    /// </summary>
    Task<bool> ShouldScheduleTutoringAsync(int userId, string subject);

    /// <summary>
    /// Generates a practice exercise for a specific deficit using Claude Sonnet 4.5
    /// </summary>
    Task<GeneratedExercise> GenerateExerciseForDeficitAsync(int deficitId);

    /// <summary>
    /// Plans a complete learning schedule for the user
    /// </summary>
    Task<List<LearningSession>> PlanLearningScheduleAsync(int userId);

    /// <summary>
    /// Updates exercise based on user answer using SM-2 spaced repetition algorithm
    /// </summary>
    Task UpdateExerciseProgressAsync(int exerciseId, string userAnswer, bool isCorrect);
}
