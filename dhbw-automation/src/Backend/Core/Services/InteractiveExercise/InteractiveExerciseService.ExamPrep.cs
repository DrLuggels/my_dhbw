using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.InteractiveExercise;

public partial class InteractiveExerciseService
{
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

            var responseDoc = await _aiService.GenerateJsonWithGeminiAsync(systemPrompt, userPrompt, userId);

            if (responseDoc == null)
                throw new InvalidOperationException("Failed to generate exam prep exercise with Gemini");

            var (question, correctAnswer, explanation, helpText, subQuestions) = ParseExamPrepResponse(responseDoc);

            int? maxAttempts = exerciseMode switch
            {
                "exam_simulation" => 1,
                "exam_prep" => 3,
                _ => null
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
                HelpText = exerciseMode == "exam_simulation" ? null : helpText,
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
            sb.AppendLine("- Erstelle eine komplexe Aufgabe mit 2-3 Teilfragen");

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
            subQuestions = subQEl.GetRawText();

        return (question, correctAnswer, explanation, helpText, subQuestions);
    }
}
