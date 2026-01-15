using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.InteractiveExercise;

public partial class InteractiveExerciseService
{
    private InteractiveExerciseContent ParseInteractiveExerciseContent(JsonDocument doc)
    {
        try
        {
            var root = doc.RootElement;
            var content = new InteractiveExerciseContent
            {
                Version = GetStringOrDefault(root, "version", "2.0"),
                Metadata = ParseMetadata(root),
                Steps = ParseSteps(root)
            };

            foreach (var step in content.Steps)
            {
                step.Instruction = SanitizeHtml(step.Instruction);
                step.Title = SanitizeHtml(step.Title);
                foreach (var hint in step.Hints)
                    hint.Content = SanitizeHtml(hint.Content);
                if (step.Feedback.OnCorrect != null)
                    step.Feedback.OnCorrect.Message = SanitizeHtml(step.Feedback.OnCorrect.Message);
                if (step.Feedback.OnIncorrect != null)
                    step.Feedback.OnIncorrect.Message = SanitizeHtml(step.Feedback.OnIncorrect.Message);
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing interactive exercise content");
            throw;
        }
    }

    private ExerciseMetadata ParseMetadata(JsonElement root)
    {
        if (!root.TryGetProperty("metadata", out var meta))
            return new ExerciseMetadata();

        return new ExerciseMetadata
        {
            Subject = GetStringOrDefault(meta, "subject", ""),
            Topic = GetStringOrDefault(meta, "topic", ""),
            Difficulty = GetStringOrDefault(meta, "difficulty", "medium"),
            EstimatedMinutes = GetIntOrDefault(meta, "estimatedMinutes", 10),
            LearningObjectives = GetStringArrayOrDefault(meta, "learningObjectives"),
            Prerequisites = GetStringArrayOrDefault(meta, "prerequisites")
        };
    }

    private List<ExerciseStep> ParseSteps(JsonElement root)
    {
        var steps = new List<ExerciseStep>();
        if (!root.TryGetProperty("steps", out var stepsArray))
            return steps;

        foreach (var stepEl in stepsArray.EnumerateArray())
        {
            steps.Add(new ExerciseStep
            {
                Id = GetStringOrDefault(stepEl, "id", $"step-{steps.Count + 1}"),
                Order = GetIntOrDefault(stepEl, "order", steps.Count + 1),
                Title = GetStringOrDefault(stepEl, "title", ""),
                Instruction = GetStringOrDefault(stepEl, "instruction", ""),
                Component = ParseComponent(stepEl),
                Validation = ParseValidation(stepEl),
                Feedback = ParseFeedback(stepEl),
                Hints = ParseHints(stepEl)
            });
        }

        return steps;
    }

    private ExerciseComponent ParseComponent(JsonElement stepEl)
    {
        if (!stepEl.TryGetProperty("component", out var compEl))
            return new ExerciseComponent { Type = "text_input" };

        var component = new ExerciseComponent
        {
            Type = GetStringOrDefault(compEl, "type", "text_input"),
            CorrectAnswer = GetStringOrDefault(compEl, "correctAnswer", null)
        };

        if (compEl.TryGetProperty("config", out var configEl))
        {
            component.Config = JsonSerializer.Deserialize<Dictionary<string, object>>(configEl.GetRawText())
                ?? new Dictionary<string, object>();
        }

        if (compEl.TryGetProperty("options", out var optionsEl))
        {
            component.Options = new List<ComponentOption>();
            foreach (var optEl in optionsEl.EnumerateArray())
            {
                component.Options.Add(new ComponentOption
                {
                    Id = GetStringOrDefault(optEl, "id", ""),
                    Label = SanitizeHtml(GetStringOrDefault(optEl, "label", "")),
                    IsCorrect = GetBoolOrDefault(optEl, "isCorrect", false),
                    Explanation = SanitizeHtml(GetStringOrDefault(optEl, "explanation", null))
                });
            }
        }

        // Parse draggables from either "draggables" or "config.items"
        if (compEl.TryGetProperty("draggables", out var draggablesEl))
        {
            component.Draggables = ParseDraggables(draggablesEl);
        }
        else if (compEl.TryGetProperty("config", out var configForDrag) &&
                 configForDrag.TryGetProperty("items", out var itemsEl))
        {
            component.Draggables = ParseDraggablesFromItems(itemsEl);
        }

        // Parse dropZones from either "dropZones" or "config.categories"
        if (compEl.TryGetProperty("dropZones", out var zonesEl))
        {
            component.DropZones = ParseDropZones(zonesEl);
        }
        else if (compEl.TryGetProperty("config", out var configForZones) &&
                 configForZones.TryGetProperty("categories", out var categoriesEl))
        {
            component.DropZones = ParseDropZonesFromCategories(categoriesEl);
        }

        return component;
    }

    private ValidationRule ParseValidation(JsonElement stepEl)
    {
        if (!stepEl.TryGetProperty("validation", out var valEl))
            return new ValidationRule();

        return new ValidationRule
        {
            Type = GetStringOrDefault(valEl, "type", "exact"),
            RealTimeValidation = GetBoolOrDefault(valEl, "realTimeValidation", false),
            PartialCredit = GetBoolOrDefault(valEl, "partialCredit", false)
        };
    }

    private FeedbackConfig ParseFeedback(JsonElement stepEl)
    {
        if (!stepEl.TryGetProperty("feedback", out var fbEl))
            return new FeedbackConfig();

        var config = new FeedbackConfig();

        if (fbEl.TryGetProperty("onCorrect", out var correctEl))
        {
            config.OnCorrect = new FeedbackMessage
            {
                Message = GetStringOrDefault(correctEl, "message", "Richtig!"),
                Animation = GetStringOrDefault(correctEl, "animation", null),
                ShowExplanation = GetBoolOrDefault(correctEl, "showExplanation", true)
            };
        }

        if (fbEl.TryGetProperty("onIncorrect", out var incorrectEl))
        {
            config.OnIncorrect = new FeedbackMessage
            {
                Message = GetStringOrDefault(incorrectEl, "message", "Nicht ganz richtig."),
                AllowRetry = GetBoolOrDefault(incorrectEl, "allowRetry", true),
                MaxRetries = GetIntOrNull(incorrectEl, "maxRetries")
            };
        }

        return config;
    }

    private List<Hint> ParseHints(JsonElement stepEl)
    {
        var hints = new List<Hint>();
        if (!stepEl.TryGetProperty("hints", out var hintsEl))
            return hints;

        foreach (var hintEl in hintsEl.EnumerateArray())
        {
            hints.Add(new Hint
            {
                Order = GetIntOrDefault(hintEl, "order", hints.Count + 1),
                Content = GetStringOrDefault(hintEl, "content", ""),
                Cost = GetIntOrNull(hintEl, "cost")
            });
        }

        return hints;
    }

    private List<DraggableItem> ParseDraggables(JsonElement draggablesEl)
    {
        var draggables = new List<DraggableItem>();
        foreach (var dragEl in draggablesEl.EnumerateArray())
        {
            draggables.Add(new DraggableItem
            {
                Id = GetStringOrDefault(dragEl, "id", ""),
                Content = SanitizeHtml(GetStringOrDefault(dragEl, "content", "")),
                Category = GetStringOrDefault(dragEl, "category", null)
            });
        }
        return draggables;
    }

    private List<DraggableItem> ParseDraggablesFromItems(JsonElement itemsEl)
    {
        var draggables = new List<DraggableItem>();
        foreach (var itemEl in itemsEl.EnumerateArray())
        {
            // Gemini generates items with "id", "label", and optionally "categoryId"
            var id = GetStringOrDefault(itemEl, "id", "");
            var label = GetStringOrDefault(itemEl, "label", "");
            var categoryId = GetStringOrDefault(itemEl, "categoryId", null);

            draggables.Add(new DraggableItem
            {
                Id = id,
                Content = SanitizeHtml(label),
                Category = categoryId
            });
        }
        return draggables;
    }

    private List<DropZone> ParseDropZones(JsonElement zonesEl)
    {
        var zones = new List<DropZone>();
        foreach (var zoneEl in zonesEl.EnumerateArray())
        {
            zones.Add(new DropZone
            {
                Id = GetStringOrDefault(zoneEl, "id", ""),
                Label = SanitizeHtml(GetStringOrDefault(zoneEl, "label", "")),
                AcceptedItems = GetStringArrayOrDefault(zoneEl, "acceptedItems"),
                MaxItems = GetIntOrNull(zoneEl, "maxItems")
            });
        }
        return zones;
    }

    private List<DropZone> ParseDropZonesFromCategories(JsonElement categoriesEl)
    {
        var zones = new List<DropZone>();
        foreach (var catEl in categoriesEl.EnumerateArray())
        {
            // Gemini generates categories with "id" and "label"
            zones.Add(new DropZone
            {
                Id = GetStringOrDefault(catEl, "id", ""),
                Label = SanitizeHtml(GetStringOrDefault(catEl, "label", "")),
                AcceptedItems = new List<string>(),
                MaxItems = null
            });
        }
        return zones;
    }
}
