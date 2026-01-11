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
    /// <param name="deficitId">The deficit to generate an exercise for</param>
    /// <param name="difficulty">Optional difficulty override (easy, medium, hard). If null, defaults based on deficit severity.</param>
    Task<GeneratedExercise> GenerateExerciseForDeficitAsync(int deficitId, string? difficulty = null);

    /// <summary>
    /// Plans a complete learning schedule for the user
    /// </summary>
    Task<List<LearningSession>> PlanLearningScheduleAsync(int userId);

    /// <summary>
    /// Updates exercise based on user answer using SM-2 spaced repetition algorithm
    /// </summary>
    Task UpdateExerciseProgressAsync(int exerciseId, string userAnswer, bool isCorrect);

    // Knowledge Base Methods - for periodic review of fundamentals

    /// <summary>
    /// Gets knowledge base items that are due for review (last tested >30 days ago)
    /// </summary>
    Task<List<KnowledgeBaseItem>> GetStaleKnowledgeItemsAsync(int userId, int daysSinceLastTest = 30);

    /// <summary>
    /// Generates exercises for fundamental knowledge items that haven't been tested recently
    /// </summary>
    Task<List<GeneratedExercise>> GeneratePeriodicReviewExercisesAsync(int userId, int count = 5);

    /// <summary>
    /// Creates or updates a knowledge base item from a subject/topic
    /// </summary>
    Task<KnowledgeBaseItem> UpsertKnowledgeBaseItemAsync(
        int userId,
        string subject,
        string topic,
        string category = "grundlagen",
        string importance = "medium"
    );

    /// <summary>
    /// Updates knowledge base item after an exercise is completed
    /// </summary>
    Task UpdateKnowledgeBaseScoreAsync(int knowledgeBaseItemId, double score);
}
