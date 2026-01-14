using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.LearningAnalytics;

public partial class LearningAnalyticsService
{
    public async Task<List<KnowledgeBaseItem>> GetStaleKnowledgeItemsAsync(int userId, int daysSinceLastTest = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastTest);

        var staleItems = await _context.KnowledgeBaseItems
            .Where(k => k.UserId == userId && k.IsActive && k.LastTestedDate < cutoffDate)
            .OrderBy(k => k.LastTestedDate)
            .ThenByDescending(k => k.Importance)
            .ToListAsync();

        _logger.LogInformation($"Found {staleItems.Count} stale knowledge items for user {userId} (not tested in {daysSinceLastTest}+ days)");
        return staleItems;
    }

    public async Task<List<GeneratedExercise>> GeneratePeriodicReviewExercisesAsync(int userId, int count = 5)
    {
        _logger.LogInformation($"Generating {count} periodic review exercises for user {userId}");

        var exercises = new List<GeneratedExercise>();
        var staleItems = await GetStaleKnowledgeItemsAsync(userId, 30);

        if (staleItems.Count == 0)
        {
            _logger.LogWarning($"No stale knowledge items found for user {userId}");
            return exercises;
        }

        var itemsToTest = staleItems.Take(count).ToList();
        var apiKey = await GetApiKeyAsync(userId);

        foreach (var item in itemsToTest)
        {
            try
            {
                var prompt = BuildPeriodicReviewPrompt(item);
                var responseDoc = await _anthropicClient.ChatJsonAsync(
                    systemPrompt: "Du bist ein KI-Tutor für DHBW-Studenten.",
                    userMessage: prompt, model: AnthropicModel, maxTokens: 2048, apiKey: apiKey);

                var exerciseData = ParseExerciseJson(responseDoc);

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

    public async Task<KnowledgeBaseItem> UpsertKnowledgeBaseItemAsync(
        int userId, string subject, string topic,
        string category = "grundlagen", string importance = "medium")
    {
        var existingItem = await _context.KnowledgeBaseItems
            .FirstOrDefaultAsync(k => k.UserId == userId && k.Subject == subject && k.Topic == topic);

        if (existingItem != null)
        {
            existingItem.Category = category;
            existingItem.Importance = importance;
            _logger.LogInformation($"Updated knowledge base item: {subject}/{topic}");
            await _context.SaveChangesAsync();
            return existingItem;
        }

        var newItem = new KnowledgeBaseItem
        {
            UserId = userId,
            Subject = subject,
            Topic = topic,
            Category = category,
            Importance = importance,
            LastTestedDate = DateTime.UtcNow.AddDays(-60),
            TestCount = 0,
            AverageScore = 0.0,
            LastScore = 0.0,
            NextReviewDate = DateTime.UtcNow,
            IsActive = true
        };

        _context.KnowledgeBaseItems.Add(newItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created new knowledge base item: {subject}/{topic} ({category}, {importance})");
        return newItem;
    }

    public async Task UpdateKnowledgeBaseScoreAsync(int knowledgeBaseItemId, double score)
    {
        var item = await _context.KnowledgeBaseItems.FindAsync(knowledgeBaseItemId);
        if (item == null)
        {
            _logger.LogWarning($"Knowledge base item {knowledgeBaseItemId} not found");
            return;
        }

        item.LastScore = score;
        item.TestCount++;
        item.AverageScore = ((item.AverageScore * (item.TestCount - 1)) + score) / item.TestCount;
        item.LastTestedDate = DateTime.UtcNow;

        int intervalDays;
        if (score >= 0.9)
        {
            intervalDays = item.TestCount switch { 1 => 7, 2 => 30, 3 => 90, _ => 180 };
        }
        else if (score >= 0.7)
        {
            intervalDays = item.TestCount switch { 1 => 3, 2 => 14, 3 => 30, _ => 60 };
        }
        else if (score >= 0.5)
        {
            intervalDays = 1;
        }
        else
        {
            intervalDays = 0;
            item.Importance = "high";
        }

        item.NextReviewDate = DateTime.UtcNow.AddDays(intervalDays);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            $"Updated knowledge base item {knowledgeBaseItemId}: Score={score:F2}, " +
            $"AvgScore={item.AverageScore:F2}, NextReview in {intervalDays} days");
    }

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
        sb.AppendLine("  \"question\": \"Aufgabenstellung (HTML erlaubt)\",");
        sb.AppendLine("  \"type\": \"multiple_choice|true_false|fill_blank|code_completion\",");
        sb.AppendLine("  \"options\": [\"Option A\", \"Option B\", \"Option C\", \"Option D\"],");
        sb.AppendLine("  \"correct_answer\": \"Korrekte Antwort\",");
        sb.AppendLine("  \"difficulty\": \"easy|medium|hard\",");
        sb.AppendLine("  \"explanation\": \"Ausführliche Erklärung (HTML erlaubt)\",");
        sb.AppendLine("  \"help_text\": \"Optionaler Tipp (HTML erlaubt)\"");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
