using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Ganss.Xss;

namespace DHBWAutomation.Backend.Core.Services;

public class LearningAnalyticsService : ILearningAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LearningAnalyticsService> _logger;
    private readonly AnthropicClient _anthropicClient;
    private readonly AiMetrics _aiMetrics;
    private readonly HtmlSanitizer _htmlSanitizer;

    private const string AnthropicModel = "claude-sonnet-4.5";

    public LearningAnalyticsService(
        AppDbContext context,
        ILogger<LearningAnalyticsService> logger,
        AnthropicClient anthropicClient,
        AiMetrics aiMetrics)
    {
        _context = context;
        _logger = logger;
        _anthropicClient = anthropicClient;
        _aiMetrics = aiMetrics;

        // Initialize HTML Sanitizer with safe tags whitelist
        _htmlSanitizer = new HtmlSanitizer();
        ConfigureHtmlSanitizer();
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
        return await _aiMetrics.TrackAsync("GenerateExercise", "Anthropic", AnthropicModel, async () =>
        {
            try
            {
                var deficit = await _context.LearningDeficits.FindAsync(deficitId);
                if (deficit == null)
                {
                    throw new ArgumentException($"Deficit {deficitId} not found");
                }

                _logger.LogInformation($"Generating exercise for deficit: {deficit.Subject} - {deficit.Topic}");

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

                var responseDoc = await _anthropicClient.ChatJsonAsync(
                    systemPrompt,
                    "Generiere jetzt eine Übungsaufgabe.",
                    AnthropicModel,
                    maxTokens: 2048
                );

                // Parse exercise JSON with defensive parsing
                var exerciseData = ParseExerciseJson(responseDoc);

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

    private (string Type, string Question, string CorrectAnswer, string Explanation, string HelpText) ParseExerciseJson(JsonDocument doc)
    {
        try
        {
            var root = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text");

            var textContent = root.GetString() ?? "{}";
            
            // Remove markdown formatting if present
            textContent = textContent.Trim();
            if (textContent.StartsWith("```json"))
                textContent = textContent.Substring(7);
            if (textContent.StartsWith("```"))
                textContent = textContent.Substring(3);
            if (textContent.EndsWith("```"))
                textContent = textContent.Substring(0, textContent.Length - 3);
            textContent = textContent.Trim();

            var exerciseDoc = JsonDocument.Parse(textContent);
            var exerciseRoot = exerciseDoc.RootElement;

            // SECURITY: Sanitize all HTML content from Claude
            var question = SanitizeHtml(TryGetString(exerciseRoot, "question"));
            var explanation = SanitizeHtml(TryGetString(exerciseRoot, "explanation"));
            var helpText = SanitizeHtml(TryGetString(exerciseRoot, "help_text"));
            var correctAnswer = TryGetString(exerciseRoot, "correct_answer") ?? "";  // Don't sanitize answer (plain text)

            _logger.LogInformation("Sanitized exercise HTML content");

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

    // Helper methods for defensive JSON parsing
    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
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

    /// <summary>
    /// Configures HTML Sanitizer with whitelist of safe tags
    /// SECURITY: Prevents XSS attacks from AI-generated HTML
    /// </summary>
    private void ConfigureHtmlSanitizer()
    {
        // Clear all default tags
        _htmlSanitizer.AllowedTags.Clear();

        // Whitelist safe formatting tags
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
        _htmlSanitizer.AllowedTags.Add("div");
        _htmlSanitizer.AllowedTags.Add("h1");
        _htmlSanitizer.AllowedTags.Add("h2");
        _htmlSanitizer.AllowedTags.Add("h3");
        _htmlSanitizer.AllowedTags.Add("h4");
        _htmlSanitizer.AllowedTags.Add("h5");
        _htmlSanitizer.AllowedTags.Add("h6");
        _htmlSanitizer.AllowedTags.Add("blockquote");
        _htmlSanitizer.AllowedTags.Add("sub");
        _htmlSanitizer.AllowedTags.Add("sup");

        // Whitelist safe attributes
        _htmlSanitizer.AllowedAttributes.Clear();
        _htmlSanitizer.AllowedAttributes.Add("class");
        _htmlSanitizer.AllowedAttributes.Add("style");

        // Whitelist safe CSS properties (for math/code formatting)
        _htmlSanitizer.AllowedCssProperties.Clear();
        _htmlSanitizer.AllowedCssProperties.Add("color");
        _htmlSanitizer.AllowedCssProperties.Add("background-color");
        _htmlSanitizer.AllowedCssProperties.Add("font-weight");
        _htmlSanitizer.AllowedCssProperties.Add("font-style");
        _htmlSanitizer.AllowedCssProperties.Add("text-decoration");
        _htmlSanitizer.AllowedCssProperties.Add("margin");
        _htmlSanitizer.AllowedCssProperties.Add("padding");

        _logger.LogInformation("HTML Sanitizer configured with safe tags whitelist");
    }

    /// <summary>
    /// Sanitizes HTML content to prevent XSS attacks
    /// </summary>
    private string SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _htmlSanitizer.Sanitize(html);
    }

    #region Knowledge Base Methods - Periodic Review of Fundamentals

    /// <summary>
    /// Retrieves knowledge base items that haven't been tested in a while
    /// </summary>
    public async Task<List<KnowledgeBaseItem>> GetStaleKnowledgeItemsAsync(int userId, int daysSinceLastTest = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastTest);

        var staleItems = await _context.KnowledgeBaseItems
            .Where(k =>
                k.UserId == userId &&
                k.IsActive &&
                k.LastTestedDate < cutoffDate)
            .OrderBy(k => k.LastTestedDate) // Oldest first
            .ThenByDescending(k => k.Importance) // Important topics prioritized
            .ToListAsync();

        _logger.LogInformation($"Found {staleItems.Count} stale knowledge items for user {userId} (not tested in {daysSinceLastTest}+ days)");

        return staleItems;
    }

    /// <summary>
    /// Generates periodic review exercises for fundamental knowledge
    /// Combines stale fundamentals with random important topics
    /// </summary>
    public async Task<List<GeneratedExercise>> GeneratePeriodicReviewExercisesAsync(int userId, int count = 5)
    {
        _logger.LogInformation($"Generating {count} periodic review exercises for user {userId}");

        var exercises = new List<GeneratedExercise>();

        // Get stale knowledge items (not tested in 30+ days)
        var staleItems = await GetStaleKnowledgeItemsAsync(userId, 30);

        if (staleItems.Count == 0)
        {
            _logger.LogWarning($"No stale knowledge items found for user {userId}");
            return exercises;
        }

        // Select items to test (prioritize oldest and most important)
        var itemsToTest = staleItems.Take(count).ToList();

        foreach (var item in itemsToTest)
        {
            try
            {
                // Generate exercise using Claude
                var prompt = BuildPeriodicReviewPrompt(item);
                var responseDoc = await _anthropicClient.ChatJsonAsync(
                    systemPrompt: "Du bist ein KI-Tutor für DHBW-Studenten.",
                    userMessage: prompt,
                    model: AnthropicModel,
                    maxTokens: 2048);

                // Parse exercise from JSON response
                var exerciseData = ParseExerciseJson(responseDoc);

                // Create exercise entity
                var exercise = new GeneratedExercise
                {
                    UserId = item.UserId,
                    Subject = item.Subject,
                    Topic = item.Topic,
                    ExerciseType = exerciseData.Type,
                    Question = exerciseData.Question,
                    CorrectAnswer = JsonSerializer.Serialize(exerciseData.CorrectAnswer),
                    Explanation = exerciseData.Explanation,
                    HelpText = exerciseData.HelpText,
                    Difficulty = "medium",
                    KnowledgeBaseItemId = item.Id,
                    IsPeriodicReview = true,
                    NextReviewDate = DateTime.UtcNow.AddDays(1),
                    ReviewCount = 0,
                    EaseFactor = 2.5,
                    CreatedAt = DateTime.UtcNow
                };

                exercises.Add(exercise);
                _logger.LogInformation($"Generated periodic review exercise for {item.Subject}/{item.Topic}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating periodic review exercise for knowledge item {item.Id}");
            }
        }

        return exercises;
    }

    /// <summary>
    /// Creates or updates a knowledge base item for a specific topic
    /// </summary>
    public async Task<KnowledgeBaseItem> UpsertKnowledgeBaseItemAsync(
        int userId, string subject, string topic,
        string category = "grundlagen", string importance = "medium")
    {
        // Check if item already exists
        var existingItem = await _context.KnowledgeBaseItems
            .FirstOrDefaultAsync(k =>
                k.UserId == userId &&
                k.Subject == subject &&
                k.Topic == topic);

        if (existingItem != null)
        {
            // Update category/importance if changed
            existingItem.Category = category;
            existingItem.Importance = importance;

            _logger.LogInformation($"Updated knowledge base item: {subject}/{topic}");
            await _context.SaveChangesAsync();
            return existingItem;
        }

        // Create new item
        var newItem = new KnowledgeBaseItem
        {
            UserId = userId,
            Subject = subject,
            Topic = topic,
            Category = category,
            Importance = importance,
            LastTestedDate = DateTime.UtcNow.AddDays(-60), // Set as stale to trigger initial test
            TestCount = 0,
            AverageScore = 0.0,
            LastScore = 0.0,
            NextReviewDate = DateTime.UtcNow, // Test soon
            IsActive = true
        };

        _context.KnowledgeBaseItems.Add(newItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created new knowledge base item: {subject}/{topic} ({category}, {importance})");

        return newItem;
    }

    /// <summary>
    /// Updates knowledge base item scores after exercise completion
    /// Applies spaced repetition algorithm for NextReviewDate
    /// </summary>
    public async Task UpdateKnowledgeBaseScoreAsync(int knowledgeBaseItemId, double score)
    {
        var item = await _context.KnowledgeBaseItems.FindAsync(knowledgeBaseItemId);

        if (item == null)
        {
            _logger.LogWarning($"Knowledge base item {knowledgeBaseItemId} not found");
            return;
        }

        // Update scores
        item.LastScore = score;
        item.TestCount++;

        // Calculate new average score
        item.AverageScore = ((item.AverageScore * (item.TestCount - 1)) + score) / item.TestCount;

        // Update last tested date
        item.LastTestedDate = DateTime.UtcNow;

        // Apply spaced repetition for next review date (SM-2 algorithm simplified)
        int intervalDays;

        if (score >= 0.9) // Mastery level
        {
            intervalDays = item.TestCount switch
            {
                1 => 7,    // 1 week
                2 => 30,   // 1 month
                3 => 90,   // 3 months
                _ => 180   // 6 months
            };
        }
        else if (score >= 0.7) // Good level
        {
            intervalDays = item.TestCount switch
            {
                1 => 3,    // 3 days
                2 => 14,   // 2 weeks
                3 => 30,   // 1 month
                _ => 60    // 2 months
            };
        }
        else if (score >= 0.5) // Needs practice
        {
            intervalDays = 1; // Test again tomorrow
        }
        else // Weak - needs immediate review
        {
            intervalDays = 0; // Test again today
            item.Importance = "high"; // Escalate importance
        }

        item.NextReviewDate = DateTime.UtcNow.AddDays(intervalDays);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            $"Updated knowledge base item {knowledgeBaseItemId}: Score={score:F2}, " +
            $"AvgScore={item.AverageScore:F2}, NextReview in {intervalDays} days");
    }

    /// <summary>
    /// Builds Claude prompt for periodic review exercise generation
    /// </summary>
    private string BuildPeriodicReviewPrompt(KnowledgeBaseItem item)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Du bist ein KI-Tutor für DHBW-Studenten.");
        sb.AppendLine();
        sb.AppendLine("**AUFGABE**: Generiere eine Übungsaufgabe zur Auffrischung von Grundkenntnissen.");
        sb.AppendLine();
        sb.AppendLine($"**Fach**: {item.Subject}");
        sb.AppendLine($"**Thema**: {item.Topic}");
        sb.AppendLine($"**Kategorie**: {item.Category}");
        sb.AppendLine($"**Wichtigkeit**: {item.Importance}");
        sb.AppendLine($"**Letzte Übung vor**: {(DateTime.UtcNow - item.LastTestedDate).TotalDays:F0} Tagen");
        sb.AppendLine($"**Bisherige Durchschnittsleistung**: {item.AverageScore:P0}");
        sb.AppendLine();

        sb.AppendLine("**ANFORDERUNGEN**:");
        sb.AppendLine("1. Teste Grundkenntnisse und fundamentales Verständnis");
        sb.AppendLine("2. Die Aufgabe sollte praxisnah und relevant sein");
        sb.AppendLine("3. Schwierigkeit anpassen basierend auf bisheriger Durchschnittsleistung");
        sb.AppendLine("4. Vermeide exakt dieselben Aufgaben wie früher - variiere Beispiele");
        sb.AppendLine();

        sb.AppendLine("**OUTPUT FORMAT** (JSON):");
        sb.AppendLine("{");
        sb.AppendLine("  \"question\": \"Aufgabenstellung (HTML erlaubt: <code>, <strong>, <em>, <p>, <ul>, <li>, <pre>)\",");
        sb.AppendLine("  \"type\": \"multiple_choice|true_false|fill_blank|code_completion\",");
        sb.AppendLine("  \"options\": [\"Option A\", \"Option B\", \"Option C\", \"Option D\"],");
        sb.AppendLine("  \"correct_answer\": \"Korrekte Antwort\",");
        sb.AppendLine("  \"difficulty\": \"easy|medium|hard\",");
        sb.AppendLine("  \"explanation\": \"Ausführliche Erklärung (HTML erlaubt)\",");
        sb.AppendLine("  \"help_text\": \"Optionaler Tipp (HTML erlaubt)\"");
        sb.AppendLine("}");

        return sb.ToString();
    }

    #endregion
}
