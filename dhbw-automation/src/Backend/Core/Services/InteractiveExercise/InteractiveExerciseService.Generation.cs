using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.InteractiveExercise;

public partial class InteractiveExerciseService
{
    public async Task<Models.InteractiveExercise> GenerateInteractiveExerciseAsync(
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

            var responseDoc = await _aiService.GenerateJsonWithGeminiAsync(systemPrompt, userPrompt, userId);

            if (responseDoc == null)
                throw new InvalidOperationException("Failed to generate exercise with Gemini");

            var content = ParseInteractiveExerciseContent(responseDoc);
            var totalSteps = content.Steps.Count;

            var exercise = new Models.InteractiveExercise
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
            sb.AppendLine($"- Bevorzugte Komponenten: {string.Join(", ", componentTypes)}");

        sb.AppendLine();
        sb.AppendLine("PROGRESSION:");
        sb.AppendLine("1. Schritt: Grundkonzept verstehen (einfach, einfuhrend)");
        sb.AppendLine("2. Schritt: Konzept anwenden (mittel)");
        sb.AppendLine("3. Schritt: Problemlosung (anspruchsvoller)");
        if (stepCount >= 4)
            sb.AppendLine("4-5. Schritt: Transfer/Vertiefung");

        return sb.ToString();
    }
}
