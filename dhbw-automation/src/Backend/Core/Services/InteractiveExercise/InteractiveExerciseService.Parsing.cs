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

        // Parse fill_blank: template from either "template" or "config.text"
        if (component.Type == "fill_blank")
        {
            component.Template = GetStringOrDefault(compEl, "template", null);
            if (string.IsNullOrEmpty(component.Template) && compEl.TryGetProperty("config", out var configForTemplate))
            {
                component.Template = GetStringOrDefault(configForTemplate, "text", null);
            }

            // Parse blanks array if present
            if (compEl.TryGetProperty("blanks", out var blanksEl))
            {
                component.Blanks = ParseBlanks(blanksEl);
            }
        }

        // Parse draggables from either "draggables" or "config.items"
        if (compEl.TryGetProperty("draggables", out var draggablesEl))
        {
            component.Draggables = ParseDraggables(draggablesEl);

            // Also check config.items for targetId mapping (AI sometimes generates both)
            if (compEl.TryGetProperty("config", out var configForTargetIds) &&
                configForTargetIds.TryGetProperty("items", out var itemsWithTargets))
            {
                MergeTargetIdsFromConfigItems(component.Draggables, itemsWithTargets);
            }
        }
        else if (compEl.TryGetProperty("config", out var configForDrag) &&
                 configForDrag.TryGetProperty("items", out var itemsEl))
        {
            component.Draggables = ParseDraggablesFromItems(itemsEl);
        }

        // Parse dropZones from "dropZones", "config.categories", or "config.targets"
        if (compEl.TryGetProperty("dropZones", out var zonesEl))
        {
            component.DropZones = ParseDropZones(zonesEl);
        }
        else if (compEl.TryGetProperty("config", out var configForZones))
        {
            if (configForZones.TryGetProperty("categories", out var categoriesEl))
            {
                component.DropZones = ParseDropZonesFromCategories(categoriesEl);
            }
            else if (configForZones.TryGetProperty("targets", out var targetsEl))
            {
                component.DropZones = ParseDropZonesFromTargets(targetsEl);
            }
        }

        // For drag_drop with items that have targetId, build AcceptedItems for each zone
        if (component.Type == "drag_drop" && component.DropZones != null && component.Draggables != null)
        {
            BuildAcceptedItemsFromTargetIds(component);
        }

        return component;
    }

    private List<BlankDefinition> ParseBlanks(JsonElement blanksEl)
    {
        var blanks = new List<BlankDefinition>();
        foreach (var blankEl in blanksEl.EnumerateArray())
        {
            var blank = new BlankDefinition
            {
                Id = GetStringOrDefault(blankEl, "id", ""),
                Hint = GetStringOrDefault(blankEl, "hint", null)
            };

            if (blankEl.TryGetProperty("correctAnswers", out var answersEl))
            {
                blank.CorrectAnswers = GetStringArrayOrDefault(blankEl, "correctAnswers");
            }

            blanks.Add(blank);
        }
        return blanks;
    }

    private List<DropZone> ParseDropZonesFromTargets(JsonElement targetsEl)
    {
        var zones = new List<DropZone>();
        foreach (var targetEl in targetsEl.EnumerateArray())
        {
            // AI generates targets with "id" and "label"
            zones.Add(new DropZone
            {
                Id = GetStringOrDefault(targetEl, "id", ""),
                Label = SanitizeHtml(GetStringOrDefault(targetEl, "label", "")),
                AcceptedItems = new List<string>(),
                MaxItems = GetIntOrNull(targetEl, "maxItems")
            });
        }
        return zones;
    }

    private void MergeTargetIdsFromConfigItems(List<DraggableItem> draggables, JsonElement itemsEl)
    {
        // Build a lookup from item id to targetId
        var targetIdMap = new Dictionary<string, string>();
        foreach (var itemEl in itemsEl.EnumerateArray())
        {
            var id = GetStringOrDefault(itemEl, "id", "");
            var targetId = GetStringOrDefault(itemEl, "targetId", null);
            var categoryId = GetStringOrDefault(itemEl, "categoryId", null);
            var target = targetId ?? categoryId;

            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
            {
                targetIdMap[id] = target;
            }
        }

        // Merge into draggables
        foreach (var draggable in draggables)
        {
            if (string.IsNullOrEmpty(draggable.Category) && targetIdMap.TryGetValue(draggable.Id, out var target))
            {
                draggable.Category = target;
            }
        }
    }

    private void BuildAcceptedItemsFromTargetIds(ExerciseComponent component)
    {
        // If draggables have targetId (which zone they belong to),
        // populate AcceptedItems for each dropZone
        if (component.Draggables == null || component.DropZones == null) return;

        // Check if any draggable has a Category (which represents targetId)
        var hasTargetMapping = component.Draggables.Any(d => !string.IsNullOrEmpty(d.Category));
        if (!hasTargetMapping) return;

        // Group draggables by their target zone
        foreach (var zone in component.DropZones)
        {
            var itemsForZone = component.Draggables
                .Where(d => d.Category == zone.Id)
                .Select(d => d.Id)
                .ToList();

            if (itemsForZone.Any())
            {
                zone.AcceptedItems = itemsForZone;
            }
        }
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
            // AI generates items with "id", "label", and optionally "categoryId" or "targetId"
            var id = GetStringOrDefault(itemEl, "id", "");
            var label = GetStringOrDefault(itemEl, "label", "");
            var categoryId = GetStringOrDefault(itemEl, "categoryId", null);
            var targetId = GetStringOrDefault(itemEl, "targetId", null);

            draggables.Add(new DraggableItem
            {
                Id = id,
                Content = SanitizeHtml(label),
                Category = categoryId ?? targetId // Use either categoryId or targetId
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
