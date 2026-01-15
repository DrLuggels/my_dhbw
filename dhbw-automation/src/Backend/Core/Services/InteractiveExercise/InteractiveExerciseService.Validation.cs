using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.InteractiveExercise;

public partial class InteractiveExerciseService
{
    public async Task<StepValidationResult> ValidateStepAsync(int exerciseId, string stepId, object userAnswer)
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
        var content = JsonSerializer.Deserialize<InteractiveExerciseContent>(exercise.ExerciseContent, jsonOptions);
        var step = content?.Steps.FirstOrDefault(s => s.Id == stepId);

        if (step == null)
            throw new ArgumentException($"Step {stepId} not found in exercise {exerciseId}");

        return ValidateStepAnswer(step, userAnswer);
    }

    private StepValidationResult ValidateStepAnswer(ExerciseStep step, object userAnswer)
    {
        var component = step.Component;
        var answerJson = userAnswer is JsonElement je
            ? je
            : JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(userAnswer));

        return component.Type switch
        {
            "multiple_choice" => ValidateMultipleChoice(component, answerJson, step.Feedback),
            "drag_drop" => ValidateDragDrop(component, answerJson, step.Feedback),
            "fill_blank" => ValidateFillBlank(component, answerJson, step.Feedback),
            "slider_range" => ValidateSliderRange(component, answerJson, step.Feedback),
            "text_input" => ValidateTextInput(component, answerJson, step.Feedback),
            _ => new StepValidationResult { IsCorrect = false, Feedback = "Unbekannter Komponententyp" }
        };
    }

    private StepValidationResult ValidateMultipleChoice(ExerciseComponent comp, JsonElement answer, FeedbackConfig feedback)
    {
        var selectedIds = new List<string>();
        if (answer.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in answer.EnumerateArray())
                selectedIds.Add(item.GetString() ?? "");
        }
        else if (answer.ValueKind == JsonValueKind.String)
        {
            selectedIds.Add(answer.GetString() ?? "");
        }

        var correctIds = comp.Options?.Where(o => o.IsCorrect).Select(o => o.Id).ToList() ?? new List<string>();
        var isCorrect = selectedIds.OrderBy(x => x).SequenceEqual(correctIds.OrderBy(x => x));
        var selectedOption = comp.Options?.FirstOrDefault(o => selectedIds.Contains(o.Id));

        return new StepValidationResult
        {
            IsCorrect = isCorrect,
            Score = isCorrect ? 100 : 0,
            Feedback = isCorrect ? feedback.OnCorrect.Message : feedback.OnIncorrect.Message,
            Explanation = selectedOption?.Explanation
        };
    }

    private StepValidationResult ValidateDragDrop(ExerciseComponent comp, JsonElement answer, FeedbackConfig feedback)
    {
        if (comp.DropZones == null)
            return new StepValidationResult { IsCorrect = false, Score = 0 };

        int totalZones = comp.DropZones.Count;
        int correctZones = 0;

        foreach (var zone in comp.DropZones)
        {
            if (answer.TryGetProperty(zone.Id, out var zoneItems))
            {
                var placedItems = new List<string>();
                foreach (var item in zoneItems.EnumerateArray())
                    placedItems.Add(item.GetString() ?? "");

                var isZoneCorrect = placedItems.OrderBy(x => x)
                    .SequenceEqual(zone.AcceptedItems.OrderBy(x => x));
                if (isZoneCorrect) correctZones++;
            }
        }

        var score = totalZones > 0 ? (double)correctZones / totalZones * 100 : 0;
        var isCorrect = correctZones == totalZones;

        return new StepValidationResult
        {
            IsCorrect = isCorrect,
            IsPartiallyCorrect = correctZones > 0 && !isCorrect,
            Score = score,
            Feedback = isCorrect ? feedback.OnCorrect.Message : feedback.OnIncorrect.Message,
            Details = new Dictionary<string, object>
            {
                { "correctZones", correctZones },
                { "totalZones", totalZones }
            }
        };
    }

    private StepValidationResult ValidateFillBlank(ExerciseComponent comp, JsonElement answer, FeedbackConfig feedback)
    {
        var isCorrect = comp.CorrectAnswer != null &&
            answer.GetString()?.Trim().Equals(comp.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase) == true;

        return new StepValidationResult
        {
            IsCorrect = isCorrect,
            Score = isCorrect ? 100 : 0,
            Feedback = isCorrect ? feedback.OnCorrect.Message : feedback.OnIncorrect.Message
        };
    }

    private StepValidationResult ValidateSliderRange(ExerciseComponent comp, JsonElement answer, FeedbackConfig feedback)
    {
        if (!comp.Config.TryGetValue("correctValue", out var correctObj) ||
            !comp.Config.TryGetValue("tolerance", out var toleranceObj))
            return new StepValidationResult { IsCorrect = false, Score = 0 };

        var correctValue = Convert.ToDouble(correctObj);
        var tolerance = Convert.ToDouble(toleranceObj);
        var userValue = answer.GetDouble();

        var isCorrect = Math.Abs(userValue - correctValue) <= tolerance;
        var score = isCorrect ? 100 : Math.Max(0, 100 - Math.Abs(userValue - correctValue) / tolerance * 50);

        return new StepValidationResult
        {
            IsCorrect = isCorrect,
            IsPartiallyCorrect = score > 50 && !isCorrect,
            Score = score,
            Feedback = isCorrect ? feedback.OnCorrect.Message : feedback.OnIncorrect.Message
        };
    }

    private StepValidationResult ValidateTextInput(ExerciseComponent comp, JsonElement answer, FeedbackConfig feedback)
    {
        var userAnswer = answer.GetString()?.Trim() ?? "";
        var correctAnswer = comp.CorrectAnswer?.Trim() ?? "";
        var isCorrect = userAnswer.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase);

        return new StepValidationResult
        {
            IsCorrect = isCorrect,
            Score = isCorrect ? 100 : 0,
            Feedback = isCorrect ? feedback.OnCorrect.Message : feedback.OnIncorrect.Message
        };
    }
}
