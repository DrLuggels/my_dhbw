using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.InteractiveExercise;

public partial class InteractiveExerciseService
{
    public async Task<Models.InteractiveExercise> UpdateStepProgressAsync(
        int exerciseId,
        string stepId,
        StepValidationResult result)
    {
        var exercise = await _context.InteractiveExercises.FindAsync(exerciseId);
        if (exercise == null)
            throw new ArgumentException($"Exercise {exerciseId} not found");

        // Support both camelCase (new) and PascalCase (legacy) JSON
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var progress = JsonSerializer.Deserialize<StepProgressData>(exercise.StepProgress, jsonOptions) ?? new StepProgressData();

        if (!progress.Steps.TryGetValue(stepId, out var stepProgress))
        {
            stepProgress = new StepProgressEntry();
            progress.Steps[stepId] = stepProgress;
        }

        stepProgress.Attempts++;
        stepProgress.Score = result.Score;

        if (result.IsCorrect && !stepProgress.Completed)
        {
            stepProgress.Completed = true;
            stepProgress.CompletedAt = DateTime.UtcNow;
            exercise.CompletedSteps++;
        }

        exercise.StepProgress = JsonSerializer.Serialize(progress, jsonOptions);
        exercise.Score = progress.Steps.Values.Average(s => s.Score);

        if (exercise.StartedAt == null)
            exercise.StartedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return exercise;
    }

    public async Task<Models.InteractiveExercise> CompleteExerciseAsync(int exerciseId)
    {
        var exercise = await _context.InteractiveExercises.FindAsync(exerciseId);
        if (exercise == null)
            throw new ArgumentException($"Exercise {exerciseId} not found");

        exercise.CompletedAt = DateTime.UtcNow;
        ApplySpacedRepetition(exercise);

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Completed exercise {exerciseId} with score {exercise.Score:F1}");
        return exercise;
    }

    private void ApplySpacedRepetition(Models.InteractiveExercise exercise)
    {
        exercise.ReviewCount++;
        var score = exercise.Score / 100.0;

        if (score >= 0.9)
        {
            var interval = exercise.ReviewCount switch
            {
                1 => 1,
                2 => 6,
                _ => (int)Math.Round(exercise.ReviewCount * exercise.EaseFactor)
            };
            exercise.NextReviewDate = DateTime.UtcNow.AddDays(interval);
            exercise.EaseFactor = Math.Min(3.0, exercise.EaseFactor + 0.1);
        }
        else if (score >= 0.7)
        {
            exercise.NextReviewDate = DateTime.UtcNow.AddDays(3);
        }
        else
        {
            exercise.ReviewCount = 0;
            exercise.NextReviewDate = DateTime.UtcNow.AddDays(1);
            exercise.EaseFactor = Math.Max(1.3, exercise.EaseFactor - 0.2);
        }
    }

    public async Task<List<Models.InteractiveExercise>> GetDueInteractiveExercisesAsync(int userId)
    {
        return await _context.InteractiveExercises
            .Where(e => e.UserId == userId &&
                        e.NextReviewDate <= DateTime.UtcNow &&
                        e.CompletedAt == null)
            .OrderBy(e => e.NextReviewDate)
            .ToListAsync();
    }

    public async Task<List<GeneratedExercise>> GetDueClassicExercisesAsync(int userId, string? exerciseMode = null)
    {
        var query = _context.GeneratedExercises
            .Where(e => e.UserId == userId &&
                        e.NextReviewDate <= DateTime.UtcNow &&
                        e.IsCorrect != true);

        if (exerciseMode != null)
            query = query.Where(e => e.ExerciseMode == exerciseMode);

        return await query.OrderBy(e => e.NextReviewDate).ToListAsync();
    }
}
