using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace DHBWAutomation.Backend.Core.Services;

public class LearningAnalyticsService : ILearningAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LearningAnalyticsService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _anthropicApiKey;

    private const string AnthropicEndpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicModel = "claude-sonnet-4.5";
    private const string AnthropicVersion = "2023-06-01";

    public LearningAnalyticsService(
        AppDbContext context,
        ILogger<LearningAnalyticsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }

    public async Task AnalyzeDocumentErrorsAsync(int documentId)
    {
        try
        {
            _logger.LogInformation($"Analyzing errors for document {documentId}");

            var document = await _context.Documents.FindAsync(documentId);
            if (document == null || string.IsNullOrEmpty(document.DetectedErrors))
            {
                _logger.LogWarning($"Document {documentId} not found or has no errors");
                return;
            }

            var errors = JsonSerializer.Deserialize<List<DetectedError>>(document.DetectedErrors);
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            // Process each error
            foreach (var error in errors)
            {
                // Find or create learning deficit
                var deficit = await _context.LearningDeficits
                    .FirstOrDefaultAsync(d =>
                        d.UserId == document.UserId &&
                        d.Subject == error.Subject &&
                        d.Topic == error.Topic &&
                        d.ErrorType == error.ErrorType);

                if (deficit == null)
                {
                    // Create new deficit
                    deficit = new LearningDeficit
                    {
                        UserId = document.UserId,
                        Subject = error.Subject,
                        Topic = error.Topic,
                        ErrorType = error.ErrorType,
                        ErrorDescription = error.Explanation,
                        OccurrenceCount = 1,
                        FirstOccurrence = DateTime.UtcNow,
                        LastOccurrence = DateTime.UtcNow,
                        Severity = error.Severity,
                        RelatedDocumentIds = JsonSerializer.Serialize(new[] { documentId })
                    };

                    _context.LearningDeficits.Add(deficit);
                    _logger.LogInformation($"Created new learning deficit: {error.Subject} - {error.Topic}");
                }
                else
                {
                    // Update existing deficit
                    deficit.OccurrenceCount++;
                    deficit.LastOccurrence = DateTime.UtcNow;

                    // Escalate severity based on occurrence count
                    if (deficit.OccurrenceCount >= 3)
                    {
                        deficit.Severity = "high";
                        deficit.NeedsTutoring = true;
                        _logger.LogWarning($"Deficit escalated to HIGH: {error.Subject} - {error.Topic} (occurred {deficit.OccurrenceCount} times)");
                    }
                    else if (deficit.OccurrenceCount >= 2)
                    {
                        deficit.Severity = "medium";
                    }

                    // Add document to related documents
                    var docIds = JsonSerializer.Deserialize<List<int>>(deficit.RelatedDocumentIds) ?? new List<int>();
                    if (!docIds.Contains(documentId))
                    {
                        docIds.Add(documentId);
                        deficit.RelatedDocumentIds = JsonSerializer.Serialize(docIds);
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Processed {errors.Count} errors from document {documentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing document errors for document {documentId}");
            throw;
        }
    }

    public async Task<List<LearningDeficit>> GetActiveDeficitsAsync(int userId)
    {
        return await _context.LearningDeficits
            .Where(d => d.UserId == userId && d.ResolvedAt == null)
            .OrderByDescending(d => d.Severity)
            .ThenByDescending(d => d.OccurrenceCount)
            .ThenByDescending(d => d.LastOccurrence)
            .ToListAsync();
    }

    public async Task<bool> ShouldScheduleTutoringAsync(int userId, string subject)
    {
        var highPriorityDeficits = await _context.LearningDeficits
            .Where(d => d.UserId == userId &&
                       d.Subject == subject &&
                       d.Severity == "high" &&
                       d.ResolvedAt == null)
            .CountAsync();

        return highPriorityDeficits > 0;
    }

    public async Task<GeneratedExercise> GenerateExerciseForDeficitAsync(int deficitId)
    {
        try
        {
            var deficit = await _context.LearningDeficits.FindAsync(deficitId);
            if (deficit == null)
            {
                throw new ArgumentException($"Deficit {deficitId} not found");
            }

            _logger.LogInformation($"Generating exercise for deficit: {deficit.Subject} - {deficit.Topic}");

            if (string.IsNullOrEmpty(_anthropicApiKey))
            {
                throw new InvalidOperationException("Anthropic API Key not configured");
            }

            // Determine difficulty based on severity (inverse relationship - high severity = easier exercises)
            var difficulty = deficit.Severity switch
            {
                "high" => "easy",       // High severity = student struggling = easier exercises
                "medium" => "medium",
                _ => "medium"
            };

            // Use Claude Sonnet 4.5 to generate exercise
            var systemPrompt = $@"Du bist ein Experte für die Erstellung von Übungsaufgaben.

Erstelle eine {difficulty} Übungsaufgabe für einen Studenten mit folgendem Lerndefizit:

Fach: {deficit.Subject}
Thema: {deficit.Topic}
Fehlertyp: {deficit.ErrorType}
Beschreibung: {deficit.ErrorDescription}

Die Aufgabe sollte dem Studenten helfen, genau dieses Defizit zu überwinden.

Gib deine Antwort als JSON zurück:
{{
    ""type"": ""multiple_choice"" | ""text_answer"" | ""calculation"" | ""code"",
    ""question"": ""HTML-formatierte Frage"",
    ""options"": [""A) Option 1"", ""B) Option 2"", ...] // nur bei multiple_choice,
    ""correct_answer"": ""Die korrekte Antwort"",
    ""explanation"": ""Detaillierte Erklärung der Lösung mit Schritt-für-Schritt Anleitung"",
    ""help_text"": ""Hilfestellung/Tipps wenn der Student nicht weiterkommt""
}}";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", _anthropicApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

            var requestBody = new
            {
                model = AnthropicModel,
                max_tokens = 2048,
                temperature = 0.5,
                system = systemPrompt,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = "Generiere jetzt eine Übungsaufgabe."
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(AnthropicEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseJson);

            var exerciseText = result.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            // Parse exercise JSON
            var exerciseData = ParseExerciseJson(exerciseText);

            // Create exercise entity
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
                NextReviewDate = DateTime.UtcNow.AddDays(1), // First review tomorrow
                ReviewCount = 0,
                EaseFactor = 2.5, // SM-2 Algorithm starting value
                CreatedAt = DateTime.UtcNow
            };

            _context.GeneratedExercises.Add(exercise);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Generated exercise {exercise.Id} for deficit {deficitId}");
            return exercise;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating exercise for deficit {deficitId}");
            throw;
        }
    }

    private (string Type, string Question, string CorrectAnswer, string Explanation, string HelpText) ParseExerciseJson(string json)
    {
        try
        {
            // Clean up JSON
            json = json.Trim();
            if (json.StartsWith("```json"))
                json = json.Substring(7);
            if (json.StartsWith("```"))
                json = json.Substring(3);
            if (json.EndsWith("```"))
                json = json.Substring(0, json.Length - 3);
            json = json.Trim();

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return (
                Type: root.GetProperty("type").GetString() ?? "text_answer",
                Question: root.GetProperty("question").GetString() ?? "",
                CorrectAnswer: root.GetProperty("correct_answer").GetString() ?? "",
                Explanation: root.GetProperty("explanation").GetString() ?? "",
                HelpText: root.TryGetProperty("help_text", out var help) ? help.GetString() ?? "" : ""
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing exercise JSON");
            return ("text_answer", "Fehler beim Generieren der Aufgabe", "", "", "");
        }
    }

    public async Task<List<LearningSession>> PlanLearningScheduleAsync(int userId)
    {
        try
        {
            _logger.LogInformation($"Planning learning schedule for user {userId}");

            var sessions = new List<LearningSession>();

            // Get all active deficits
            var deficits = await GetActiveDeficitsAsync(userId);

            foreach (var deficit in deficits)
            {
                // Estimate learning time based on severity (heuristic-based, NOT AI)
                var estimatedMinutes = EstimateLearningDuration(deficit.Subject, deficit.Topic, deficit.Severity);

                var session = new LearningSession
                {
                    Subject = deficit.Subject,
                    Topic = deficit.Topic,
                    Start = DateTime.MinValue, // Will be filled by SchedulingService
                    End = DateTime.MinValue,
                    PriorityScore = deficit.Severity switch
                    {
                        "high" => 90,
                        "medium" => 70,
                        _ => 50
                    }
                };

                sessions.Add(session);
            }

            _logger.LogInformation($"Planned {sessions.Count} learning sessions");
            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error planning learning schedule");
            return new List<LearningSession>();
        }
    }

    private int EstimateLearningDuration(string subject, string topic, string severity)
    {
        // Heuristic-based time estimation (NOT AI-based, as Claude is bad at this)
        var baseTime = severity switch
        {
            "high" => 120,      // 2 hours for high severity
            "medium" => 90,     // 1.5 hours for medium
            "low" => 60,        // 1 hour for low
            _ => 60
        };

        // Additional time for complex subjects
        if (subject.Contains("Mathematik", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("Programmierung", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("Algorithmen", StringComparison.OrdinalIgnoreCase))
        {
            baseTime += 30; // Add 30 minutes for complex subjects
        }

        return baseTime;
    }

    public async Task UpdateExerciseProgressAsync(int exerciseId, string userAnswer, bool isCorrect)
    {
        try
        {
            var exercise = await _context.GeneratedExercises.FindAsync(exerciseId);
            if (exercise == null)
            {
                throw new ArgumentException($"Exercise {exerciseId} not found");
            }

            exercise.UserAnswer = userAnswer;
            exercise.AnsweredAt = DateTime.UtcNow;
            exercise.IsCorrect = isCorrect;

            // Apply SM-2 Spaced Repetition Algorithm
            if (isCorrect)
            {
                // Correct answer: increase interval
                exercise.ReviewCount++;

                var interval = exercise.ReviewCount switch
                {
                    1 => 1,  // 1 day
                    2 => 6,  // 6 days
                    _ => (int)Math.Round(exercise.ReviewCount * exercise.EaseFactor)
                };

                exercise.NextReviewDate = DateTime.UtcNow.AddDays(interval);
                exercise.EaseFactor = Math.Max(1.3, exercise.EaseFactor + 0.1);

                _logger.LogInformation($"Exercise {exerciseId} correct - next review in {interval} days");
            }
            else
            {
                // Incorrect answer: reset
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
