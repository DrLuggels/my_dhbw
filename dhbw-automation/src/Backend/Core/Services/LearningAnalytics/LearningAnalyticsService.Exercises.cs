using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.LearningAnalytics;

public partial class LearningAnalyticsService
{
    public async Task<GeneratedExercise> GenerateExerciseForDeficitAsync(int deficitId, string? difficultyOverride = null)
    {
        return await _aiMetrics.TrackAsync("GenerateExercise", "Anthropic", AnthropicModel, async () =>
        {
            try
            {
                var deficit = await _context.LearningDeficits.FindAsync(deficitId);
                if (deficit == null)
                    throw new ArgumentException($"Deficit {deficitId} not found");

                _logger.LogInformation($"Generating exercise for deficit: {deficit.Subject} - {deficit.Topic}");

                string difficulty = !string.IsNullOrEmpty(difficultyOverride)
                    ? difficultyOverride
                    : deficit.Severity switch
                    {
                        "high" => "easy",
                        "medium" => "medium",
                        _ => "medium"
                    };

                var difficultyGuidance = GetDifficultyGuidance(difficulty);

                var systemPrompt = $@"Du bist ein Experte für die Erstellung von abwechslungsreichen Übungsaufgaben.

Erstelle eine EINZIGARTIGE Übungsaufgabe mit Schwierigkeit '{difficulty.ToUpper()}' für:

Fach: {deficit.Subject}
Thema: {deficit.Topic}
Fehlertyp: {deficit.ErrorType}
Beschreibung: {deficit.ErrorDescription}

{difficultyGuidance}

WICHTIG:
- Erstelle eine NEUE, KREATIVE Aufgabe (keine Standardbeispiele wie '1+1')
- Verwende realistische Szenarien und Kontexte
- Bei Mathe: Verwende verschiedene Zahlen und Aufgabentypen
- Die Frage sollte klar formuliert und eindeutig lösbar sein

Gib deine Antwort als JSON zurück:
{{
    ""type"": ""multiple_choice"" | ""text_answer"" | ""calculation"" | ""code"",
    ""question"": ""<p>HTML-formatierte Frage mit konkretem Aufgabentext</p>"",
    ""options"": [""A) Option 1"", ""B) Option 2"", ""C) Option 3"", ""D) Option 4""],
    ""correct_answer"": ""Die exakte korrekte Antwort"",
    ""explanation"": ""<p>Detaillierte Erklärung mit Lösungsweg</p>"",
    ""help_text"": ""<p>Hilfreicher Tipp ohne die Lösung zu verraten</p>""
}}";

                var apiKey = await GetApiKeyAsync(deficit.UserId);
                var responseDoc = await _anthropicClient.ChatJsonAsync(
                    systemPrompt, "Generiere jetzt eine Übungsaufgabe.",
                    AnthropicModel, maxTokens: 2048, apiKey: apiKey);

                var exerciseData = ParseExerciseJson(responseDoc);

                var exercise = new GeneratedExercise
                {
                    UserId = deficit.UserId,
                    DeficitId = deficitId,
                    Subject = deficit.Subject,
                    Topic = deficit.Topic,
                    ExerciseType = exerciseData.Type,
                    Question = exerciseData.Question,
                    CorrectAnswer = JsonSerializer.Serialize(exerciseData.CorrectAnswer),
                    Explanation = exerciseData.Explanation,
                    HelpText = exerciseData.HelpText,
                    Difficulty = difficulty,
                    NextReviewDate = DateTime.UtcNow.AddDays(1),
                    ReviewCount = 0,
                    EaseFactor = 2.5,
                    CreatedAt = DateTime.UtcNow
                };

                _context.GeneratedExercises.Add(exercise);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generated exercise {exercise.Id} for deficit {deficitId}");
                return exercise;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("unavailable"))
            {
                _logger.LogError(ex, "Anthropic service unavailable");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating exercise for deficit {deficitId}");
                throw;
            }
        });
    }

    private string GetDifficultyGuidance(string difficulty) => difficulty switch
    {
        "easy" => @"EASY Schwierigkeit bedeutet:
- Grundlegende Konzepte und Definitionen
- Einfache, direkte Fragen ohne Tricks
- Keine komplexen Berechnungen
- Z.B.: 'Was ist 2+3?', 'Was bedeutet der Begriff X?'",
        "medium" => @"MEDIUM Schwierigkeit bedeutet:
- Anwendung von Konzepten auf konkrete Situationen
- Mehrere Rechenschritte oder Überlegungen nötig
- Transferwissen erforderlich
- Z.B.: 'Löse die Gleichung 3x + 5 = 20', 'Erkläre den Zusammenhang zwischen X und Y'",
        "hard" => @"HARD Schwierigkeit bedeutet:
- Komplexe, mehrstufige Probleme
- Kombination mehrerer Konzepte nötig
- Kritisches Denken und Analyse erforderlich
- Textaufgaben mit versteckten Informationen
- Z.B.: 'Analysiere diesen Code auf Fehler', 'Beweise warum...'",
        _ => "Mittlere Schwierigkeit"
    };

    private (string Type, string Question, string CorrectAnswer, string Explanation, string HelpText) ParseExerciseJson(JsonDocument doc)
    {
        try
        {
            var exerciseRoot = doc.RootElement;

            var question = SanitizeHtml(TryGetString(exerciseRoot, "question"));
            var explanation = SanitizeHtml(TryGetString(exerciseRoot, "explanation"));
            var helpText = SanitizeHtml(TryGetString(exerciseRoot, "help_text"));
            var correctAnswer = TryGetString(exerciseRoot, "correct_answer") ?? "";

            _logger.LogInformation("Parsed exercise JSON successfully");

            return (
                Type: TryGetString(exerciseRoot, "type") ?? "text_answer",
                Question: question,
                CorrectAnswer: correctAnswer,
                Explanation: explanation,
                HelpText: helpText
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing exercise JSON");
            return ("text_answer", "Fehler beim Generieren der Aufgabe", "", "", "");
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    public async Task UpdateExerciseProgressAsync(int exerciseId, string userAnswer, bool isCorrect)
    {
        try
        {
            var exercise = await _context.GeneratedExercises.FindAsync(exerciseId);
            if (exercise == null)
                throw new ArgumentException($"Exercise {exerciseId} not found");

            exercise.UserAnswer = userAnswer;
            exercise.AnsweredAt = DateTime.UtcNow;
            exercise.IsCorrect = isCorrect;

            if (isCorrect)
            {
                exercise.ReviewCount++;
                var interval = exercise.ReviewCount switch
                {
                    1 => 1,
                    2 => 6,
                    _ => (int)Math.Round(exercise.ReviewCount * exercise.EaseFactor)
                };
                exercise.NextReviewDate = DateTime.UtcNow.AddDays(interval);
                exercise.EaseFactor = Math.Max(1.3, exercise.EaseFactor + 0.1);
                _logger.LogInformation($"Exercise {exerciseId} correct - next review in {interval} days");
            }
            else
            {
                exercise.ReviewCount = 0;
                exercise.NextReviewDate = DateTime.UtcNow.AddDays(1);
                exercise.EaseFactor = Math.Max(1.3, exercise.EaseFactor - 0.2);
                _logger.LogInformation($"Exercise {exerciseId} incorrect - reset to review tomorrow");
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating exercise progress for exercise {exerciseId}");
            throw;
        }
    }
}
