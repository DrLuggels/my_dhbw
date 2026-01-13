using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Ganss.Xss;

namespace DHBWAutomation.Backend.Core.Services;

public class InteractiveExerciseService : IInteractiveExerciseService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InteractiveExerciseService> _logger;
    private readonly IAIService _aiService;
    private readonly AiMetrics _aiMetrics;
    private readonly HtmlSanitizer _htmlSanitizer;
    private readonly EncryptionHelper _encryptionHelper;

    private const string GeminiModel = "gemini-3-flash";

    public InteractiveExerciseService(
        AppDbContext context,
        ILogger<InteractiveExerciseService> logger,
        IAIService aiService,
        AiMetrics aiMetrics,
        EncryptionHelper encryptionHelper)
    {
        _context = context;
        _logger = logger;
        _aiService = aiService;
        _aiMetrics = aiMetrics;
        _encryptionHelper = encryptionHelper;

        _htmlSanitizer = new HtmlSanitizer();
        ConfigureHtmlSanitizer();
    }

    #region Get Exercise

    public async Task<InteractiveExercise?> GetInteractiveExerciseAsync(int exerciseId)
    {
        return await _context.InteractiveExercises.FindAsync(exerciseId);
    }

    #endregion

    #region Exercise Type Decision

    public string DetermineExerciseType(string difficulty, bool isNewConcept, bool isExamPrep)
    {
        // Exam preparation always uses classic exercises
        if (isExamPrep)
            return "classic";

        // New concepts or easy difficulty prefer interactive
        if (isNewConcept || difficulty == "easy")
            return "interactive";

        // Hard exercises use classic for KA-like practice
        if (difficulty == "hard")
            return "classic";

        // Medium difficulty: mix (could randomize or use other heuristics)
        return "interactive";
    }

    #endregion

    #region Interactive Exercise Generation (Brilliant-Style)

    public async Task<InteractiveExercise> GenerateInteractiveExerciseAsync(
        int userId,
        string subject,
        string topic,
        string difficulty = "medium",
        int? deficitId = null,
        string[]? preferredComponentTypes = null)
    {
        return await _aiMetrics.TrackAsync("GenerateInteractiveExercise", "Gemini", GeminiModel, async () =>
        {
            _logger.LogInformation($"Generating interactive exercise: {subject}/{topic} ({difficulty})");

            var systemPrompt = BuildInteractiveSystemPrompt();
            var userPrompt = BuildInteractiveUserPrompt(subject, topic, difficulty, preferredComponentTypes);

            var responseDoc = await _aiService.GenerateJsonWithGeminiAsync(
                systemPrompt,
                userPrompt,
                userId);

            if (responseDoc == null)
            {
                throw new InvalidOperationException("Failed to generate exercise with Gemini");
            }

            var content = ParseInteractiveExerciseContent(responseDoc);
            var totalSteps = content.Steps.Count;

            var exercise = new InteractiveExercise
            {
                UserId = userId,
                DeficitId = deficitId,
                Subject = subject,
                Topic = topic,
                Difficulty = difficulty,
                ExerciseContent = JsonSerializer.Serialize(content),
                StepProgress = JsonSerializer.Serialize(new StepProgressData()),
                TotalSteps = totalSteps,
                CompletedSteps = 0,
                NextReviewDate = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };

            _context.InteractiveExercises.Add(exercise);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created interactive exercise {exercise.Id} with {totalSteps} steps");
            return exercise;
        });
    }

    private string BuildInteractiveSystemPrompt()
    {
        return @"Du bist ein Experte fur interaktive Lernaufgaben im Brilliant-Stil.

WICHTIGE REGELN:
1. Jede Aufgabe hat 3-5 aufeinander aufbauende Schritte
2. Jeder Schritt fokussiert auf EINEN Lernaspekt
3. Fruhe Schritte bauen Grundlagen auf, spatere wenden an
4. Verwende verschiedene Interaktionstypen fur Abwechslung
5. Gib ermutigendes, erklarendes Feedback
6. Erklare WARUM eine Antwort richtig/falsch ist

VERFUGBARE KOMPONENTEN:
- multiple_choice: Auswahlfragen (single/multiple selection)
- drag_drop: Sortieren, Kategorisieren, Zuordnen (mode: sort/categorize/match)
- fill_blank: Luckentexte mit {{blank:id}} Platzhaltern
- slider_range: Numerische Werte schätzen (min, max, correctValue, tolerance)
- code_editor: Code schreiben/vervollstandigen (language, starterCode)
- text_input: Freitext-Antworten

OUTPUT FORMAT - Antworte ausschliesslich mit validem JSON:
{
  ""version"": ""2.0"",
  ""metadata"": {
    ""subject"": ""Fach"",
    ""topic"": ""Thema"",
    ""difficulty"": ""easy|medium|hard"",
    ""estimatedMinutes"": 10,
    ""learningObjectives"": [""Ziel 1"", ""Ziel 2""]
  },
  ""steps"": [
    {
      ""id"": ""step-1"",
      ""order"": 1,
      ""title"": ""Schritt-Titel"",
      ""instruction"": ""HTML-sichere Anweisung"",
      ""component"": {
        ""type"": ""multiple_choice"",
        ""config"": { ""allowMultiple"": false },
        ""options"": [
          { ""id"": ""a"", ""label"": ""Option A"", ""isCorrect"": true, ""explanation"": ""Warum richtig"" },
          { ""id"": ""b"", ""label"": ""Option B"", ""isCorrect"": false, ""explanation"": ""Warum falsch"" }
        ]
      },
      ""validation"": { ""type"": ""exact"", ""realTimeValidation"": false },
      ""feedback"": {
        ""onCorrect"": { ""message"": ""Richtig!"", ""showExplanation"": true },
        ""onIncorrect"": { ""message"": ""Nicht ganz."", ""allowRetry"": true }
      },
      ""hints"": [{ ""order"": 1, ""content"": ""Tipp..."" }]
    }
  ]
}

SICHERHEIT:
- Kein JavaScript in HTML
- Nur erlaubte HTML-Tags: p, br, strong, em, code, pre, ul, ol, li, span, sub, sup";
    }

    private string BuildInteractiveUserPrompt(string subject, string topic, string difficulty, string[]? componentTypes)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Erstelle eine interaktive Brilliant-Aufgabe:");
        sb.AppendLine($"- Fach: {subject}");
        sb.AppendLine($"- Thema: {topic}");
        sb.AppendLine($"- Schwierigkeit: {difficulty}");

        int stepCount = difficulty switch
        {
            "easy" => 3,
            "medium" => 4,
            "hard" => 5,
            _ => 4
        };
        sb.AppendLine($"- Anzahl Schritte: {stepCount}");

        if (componentTypes?.Length > 0)
        {
            sb.AppendLine($"- Bevorzugte Komponenten: {string.Join(", ", componentTypes)}");
        }

        sb.AppendLine();
        sb.AppendLine("PROGRESSION:");
        sb.AppendLine("1. Schritt: Grundkonzept verstehen (einfach, einfuhrend)");
        sb.AppendLine("2. Schritt: Konzept anwenden (mittel)");
        sb.AppendLine("3. Schritt: Problemlosung (anspruchsvoller)");
        if (stepCount >= 4)
            sb.AppendLine("4-5. Schritt: Transfer/Vertiefung");

        return sb.ToString();
    }

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

            // Sanitize all HTML content
            foreach (var step in content.Steps)
            {
                step.Instruction = SanitizeHtml(step.Instruction);
                step.Title = SanitizeHtml(step.Title);
                foreach (var hint in step.Hints)
                {
                    hint.Content = SanitizeHtml(hint.Content);
                }
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
            var step = new ExerciseStep
            {
                Id = GetStringOrDefault(stepEl, "id", $"step-{steps.Count + 1}"),
                Order = GetIntOrDefault(stepEl, "order", steps.Count + 1),
                Title = GetStringOrDefault(stepEl, "title", ""),
                Instruction = GetStringOrDefault(stepEl, "instruction", ""),
                Component = ParseComponent(stepEl),
                Validation = ParseValidation(stepEl),
                Feedback = ParseFeedback(stepEl),
                Hints = ParseHints(stepEl)
            };
            steps.Add(step);
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

        // Parse config
        if (compEl.TryGetProperty("config", out var configEl))
        {
            component.Config = JsonSerializer.Deserialize<Dictionary<string, object>>(configEl.GetRawText())
                ?? new Dictionary<string, object>();
        }

        // Parse options for multiple_choice
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

        // Parse draggables and dropZones for drag_drop
        if (compEl.TryGetProperty("draggables", out var draggablesEl))
        {
            component.Draggables = new List<DraggableItem>();
            foreach (var dragEl in draggablesEl.EnumerateArray())
            {
                component.Draggables.Add(new DraggableItem
                {
                    Id = GetStringOrDefault(dragEl, "id", ""),
                    Content = SanitizeHtml(GetStringOrDefault(dragEl, "content", "")),
                    Category = GetStringOrDefault(dragEl, "category", null)
                });
            }
        }

        if (compEl.TryGetProperty("dropZones", out var zonesEl))
        {
            component.DropZones = new List<DropZone>();
            foreach (var zoneEl in zonesEl.EnumerateArray())
            {
                component.DropZones.Add(new DropZone
                {
                    Id = GetStringOrDefault(zoneEl, "id", ""),
                    Label = SanitizeHtml(GetStringOrDefault(zoneEl, "label", "")),
                    AcceptedItems = GetStringArrayOrDefault(zoneEl, "acceptedItems"),
                    MaxItems = GetIntOrNull(zoneEl, "maxItems")
                });
            }
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

    #endregion

    #region Classic Exercise Generation (KA-Style)

    public async Task<GeneratedExercise> GenerateExamPrepExerciseAsync(
        int userId,
        string subject,
        string topic,
        string exerciseMode,
        string difficulty = "medium",
        int? deficitId = null,
        int? timeLimitSeconds = null)
    {
        return await _aiMetrics.TrackAsync("GenerateExamPrepExercise", "Gemini", GeminiModel, async () =>
        {
            _logger.LogInformation($"Generating exam prep exercise: {subject}/{topic} ({exerciseMode}, {difficulty})");

            var systemPrompt = BuildExamPrepSystemPrompt(exerciseMode);
            var userPrompt = BuildExamPrepUserPrompt(subject, topic, difficulty, exerciseMode);

            var responseDoc = await _aiService.GenerateJsonWithGeminiAsync(
                systemPrompt,
                userPrompt,
                userId);

            if (responseDoc == null)
            {
                throw new InvalidOperationException("Failed to generate exam prep exercise with Gemini");
            }

            var (question, correctAnswer, explanation, helpText, subQuestions) = ParseExamPrepResponse(responseDoc);

            // Determine max attempts based on mode
            int? maxAttempts = exerciseMode switch
            {
                "exam_simulation" => 1,
                "exam_prep" => 3,
                _ => null // unlimited for learning
            };

            var exercise = new GeneratedExercise
            {
                UserId = userId,
                DeficitId = deficitId,
                Subject = subject,
                Topic = topic,
                ExerciseType = "text_answer",
                Question = question,
                CorrectAnswer = correctAnswer,
                Explanation = explanation,
                HelpText = exerciseMode == "exam_simulation" ? null : helpText, // No help in exam simulation
                Difficulty = difficulty,
                ExerciseMode = exerciseMode,
                TimeLimitSeconds = timeLimitSeconds,
                SubQuestions = subQuestions,
                MaxAttempts = maxAttempts,
                NextReviewDate = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };

            _context.GeneratedExercises.Add(exercise);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created exam prep exercise {exercise.Id} in mode {exerciseMode}");
            return exercise;
        });
    }

    private string BuildExamPrepSystemPrompt(string exerciseMode)
    {
        var basePrompt = @"Du bist ein Experte fur Klausuraufgaben an der DHBW.

AUFGABENFORMAT:
- Formuliere Aufgaben wie in einer echten Klausur
- Verwende formale, prázise Sprache
- Strukturiere komplexe Aufgaben mit Teilfragen (a, b, c)
- Gib Punkte pro Teilfrage an

OUTPUT FORMAT (JSON):
{
  ""question"": ""HTML-formatierte Aufgabenstellung"",
  ""correctAnswer"": ""Musterlosung"",
  ""explanation"": ""Ausfuhrliche Erklarung des Losungswegs"",
  ""helpText"": ""Hinweise fur Studierende"",
  ""subQuestions"": [
    { ""id"": ""a"", ""question"": ""Teilfrage a)"", ""points"": 2, ""answer"": ""Antwort a"" },
    { ""id"": ""b"", ""question"": ""Teilfrage b)"", ""points"": 3, ""answer"": ""Antwort b"" }
  ]
}";

        if (exerciseMode == "exam_simulation")
        {
            basePrompt += @"

EXAM SIMULATION MODUS:
- Keine Hilfestellung geben (helpText leer lassen)
- Aufgabe soll realistisch schwer sein
- Zeitdruck simulieren (kompakte Formulierung)";
        }

        return basePrompt;
    }

    private string BuildExamPrepUserPrompt(string subject, string topic, string difficulty, string exerciseMode)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Erstelle eine Klausuraufgabe:");
        sb.AppendLine($"- Fach: {subject}");
        sb.AppendLine($"- Thema: {topic}");
        sb.AppendLine($"- Schwierigkeit: {difficulty}");
        sb.AppendLine($"- Modus: {exerciseMode}");

        if (difficulty == "hard")
        {
            sb.AppendLine("- Erstelle eine komplexe Aufgabe mit 2-3 Teilfragen");
        }

        return sb.ToString();
    }

    private (string question, string correctAnswer, string explanation, string? helpText, string? subQuestions)
        ParseExamPrepResponse(JsonDocument doc)
    {
        var root = doc.RootElement;

        var question = SanitizeHtml(GetStringOrDefault(root, "question", ""));
        var correctAnswer = GetStringOrDefault(root, "correctAnswer", "");
        var explanation = SanitizeHtml(GetStringOrDefault(root, "explanation", ""));
        var helpText = SanitizeHtml(GetStringOrDefault(root, "helpText", null));

        string? subQuestions = null;
        if (root.TryGetProperty("subQuestions", out var subQEl) && subQEl.ValueKind == JsonValueKind.Array)
        {
            subQuestions = subQEl.GetRawText();
        }

        return (question, correctAnswer, explanation, helpText, subQuestions);
    }

    #endregion

    #region Step Validation

    public async Task<StepValidationResult> ValidateStepAsync(
        int exerciseId,
        string stepId,
        object userAnswer)
    {
        var exercise = await _context.InteractiveExercises.FindAsync(exerciseId);
        if (exercise == null)
            throw new ArgumentException($"Exercise {exerciseId} not found");

        var content = JsonSerializer.Deserialize<InteractiveExerciseContent>(exercise.ExerciseContent);
        var step = content?.Steps.FirstOrDefault(s => s.Id == stepId);

        if (step == null)
            throw new ArgumentException($"Step {stepId} not found in exercise {exerciseId}");

        var result = ValidateStepAnswer(step, userAnswer);
        return result;
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
        var explanation = selectedOption?.Explanation;

        return new StepValidationResult
        {
            IsCorrect = isCorrect,
            Score = isCorrect ? 100 : 0,
            Feedback = isCorrect ? feedback.OnCorrect.Message : feedback.OnIncorrect.Message,
            Explanation = explanation
        };
    }

    private StepValidationResult ValidateDragDrop(ExerciseComponent comp, JsonElement answer, FeedbackConfig feedback)
    {
        // Answer format: { "zoneId": ["itemId1", "itemId2"], ... }
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
        // Simplified fill-blank validation
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

    #endregion

    #region Progress Tracking

    public async Task<InteractiveExercise> UpdateStepProgressAsync(
        int exerciseId,
        string stepId,
        StepValidationResult result)
    {
        var exercise = await _context.InteractiveExercises.FindAsync(exerciseId);
        if (exercise == null)
            throw new ArgumentException($"Exercise {exerciseId} not found");

        var progress = JsonSerializer.Deserialize<StepProgressData>(exercise.StepProgress) ?? new StepProgressData();

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

        exercise.StepProgress = JsonSerializer.Serialize(progress);

        // Update overall score
        exercise.Score = progress.Steps.Values.Average(s => s.Score);

        // Mark started if first interaction
        if (exercise.StartedAt == null)
            exercise.StartedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return exercise;
    }

    public async Task<InteractiveExercise> CompleteExerciseAsync(int exerciseId)
    {
        var exercise = await _context.InteractiveExercises.FindAsync(exerciseId);
        if (exercise == null)
            throw new ArgumentException($"Exercise {exerciseId} not found");

        exercise.CompletedAt = DateTime.UtcNow;

        // Apply spaced repetition based on score
        ApplySpacedRepetition(exercise);

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Completed exercise {exerciseId} with score {exercise.Score:F1}");
        return exercise;
    }

    private void ApplySpacedRepetition(InteractiveExercise exercise)
    {
        exercise.ReviewCount++;
        var score = exercise.Score / 100.0; // Convert to 0-1

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

    #endregion

    #region Retrieval

    public async Task<List<InteractiveExercise>> GetDueInteractiveExercisesAsync(int userId)
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

        return await query
            .OrderBy(e => e.NextReviewDate)
            .ToListAsync();
    }

    #endregion

    #region Helpers

    private void ConfigureHtmlSanitizer()
    {
        _htmlSanitizer.AllowedTags.Clear();
        _htmlSanitizer.AllowedTags.Add("p");
        _htmlSanitizer.AllowedTags.Add("br");
        _htmlSanitizer.AllowedTags.Add("strong");
        _htmlSanitizer.AllowedTags.Add("b");
        _htmlSanitizer.AllowedTags.Add("em");
        _htmlSanitizer.AllowedTags.Add("i");
        _htmlSanitizer.AllowedTags.Add("u");
        _htmlSanitizer.AllowedTags.Add("ul");
        _htmlSanitizer.AllowedTags.Add("ol");
        _htmlSanitizer.AllowedTags.Add("li");
        _htmlSanitizer.AllowedTags.Add("code");
        _htmlSanitizer.AllowedTags.Add("pre");
        _htmlSanitizer.AllowedTags.Add("span");
        _htmlSanitizer.AllowedTags.Add("sub");
        _htmlSanitizer.AllowedTags.Add("sup");

        _htmlSanitizer.AllowedAttributes.Clear();
        _htmlSanitizer.AllowedAttributes.Add("class");
    }

    private string SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        return _htmlSanitizer.Sanitize(html);
    }

    private static string GetStringOrDefault(JsonElement el, string prop, string? defaultValue)
    {
        return el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? defaultValue ?? ""
            : defaultValue ?? "";
    }

    private static int GetIntOrDefault(JsonElement el, string prop, int defaultValue)
    {
        return el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number
            ? p.GetInt32()
            : defaultValue;
    }

    private static int? GetIntOrNull(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number
            ? p.GetInt32()
            : null;
    }

    private static bool GetBoolOrDefault(JsonElement el, string prop, bool defaultValue)
    {
        if (!el.TryGetProperty(prop, out var p)) return defaultValue;
        return p.ValueKind == JsonValueKind.True || (p.ValueKind == JsonValueKind.False ? false : defaultValue);
    }

    private static List<string> GetStringArrayOrDefault(JsonElement el, string prop)
    {
        var list = new List<string>();
        if (el.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String)
                    list.Add(item.GetString() ?? "");
        }
        return list;
    }

    #endregion
}
