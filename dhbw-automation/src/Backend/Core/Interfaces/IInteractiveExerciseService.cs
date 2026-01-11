using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IInteractiveExerciseService
{
    /// <summary>
    /// Gets a specific interactive exercise by ID
    /// </summary>
    Task<InteractiveExercise?> GetInteractiveExerciseAsync(int exerciseId);

    /// <summary>
    /// Generates an interactive Brilliant-style exercise for learning new concepts
    /// </summary>
    Task<InteractiveExercise> GenerateInteractiveExerciseAsync(
        int userId,
        string subject,
        string topic,
        string difficulty = "medium",
        int? deficitId = null,
        string[]? preferredComponentTypes = null);

    /// <summary>
    /// Generates a classic text-based exercise for exam preparation
    /// </summary>
    Task<GeneratedExercise> GenerateExamPrepExerciseAsync(
        int userId,
        string subject,
        string topic,
        string exerciseMode, // "learning", "exam_prep", "exam_simulation"
        string difficulty = "medium",
        int? deficitId = null,
        int? timeLimitSeconds = null);

    /// <summary>
    /// Validates a step answer in an interactive exercise
    /// </summary>
    Task<StepValidationResult> ValidateStepAsync(
        int exerciseId,
        string stepId,
        object userAnswer);

    /// <summary>
    /// Updates progress after completing a step
    /// </summary>
    Task<InteractiveExercise> UpdateStepProgressAsync(
        int exerciseId,
        string stepId,
        StepValidationResult result);

    /// <summary>
    /// Marks an interactive exercise as completed and calculates final score
    /// </summary>
    Task<InteractiveExercise> CompleteExerciseAsync(int exerciseId);

    /// <summary>
    /// Gets due interactive exercises for a user (spaced repetition)
    /// </summary>
    Task<List<InteractiveExercise>> GetDueInteractiveExercisesAsync(int userId);

    /// <summary>
    /// Gets due classic exercises for a user
    /// </summary>
    Task<List<GeneratedExercise>> GetDueClassicExercisesAsync(int userId, string? exerciseMode = null);

    /// <summary>
    /// Decides which exercise type to generate based on context
    /// Returns "interactive" or "classic"
    /// </summary>
    string DetermineExerciseType(string difficulty, bool isNewConcept, bool isExamPrep);
}

public class StepValidationResult
{
    public bool IsCorrect { get; set; }
    public double Score { get; set; } // 0-100
    public bool IsPartiallyCorrect { get; set; }
    public string? Feedback { get; set; }
    public string? Explanation { get; set; }
    public Dictionary<string, object>? Details { get; set; } // Component-specific validation details
}
