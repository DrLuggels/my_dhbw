using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.IntentAnalysis;

public partial class IntentAnalysisService
{
    public async Task<List<UserInteraction>> GenerateInteractionsAsync(
        DocumentIntent intent,
        int userId,
        int documentId)
    {
        var interactions = new List<UserInteraction>();

        try
        {
            foreach (var meeting in intent.Meetings)
            {
                interactions.Add(new UserInteraction
                {
                    UserId = userId,
                    InteractionType = "schedule_meeting",
                    Context = JsonSerializer.Serialize(meeting),
                    Question = $"Du möchtest dich mit {meeting.PersonName} treffen" +
                              (string.IsNullOrEmpty(meeting.Purpose) ? "" : $" ({meeting.Purpose})") +
                              ". Wann passt es dir?",
                    SuggestedOptions = JsonSerializer.Serialize(new[]
                    {
                        "Nächste Woche Nachmittag (14-17 Uhr)",
                        "Diese Woche noch",
                        "In 2 Wochen",
                        "Zeig mir verfügbare Termine"
                    }),
                    Status = "pending",
                    RelatedDocumentId = documentId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (intent.Project != null)
            {
                interactions.Add(new UserInteraction
                {
                    UserId = userId,
                    InteractionType = "new_project",
                    Context = JsonSerializer.Serialize(intent.Project),
                    Question = $"Ich habe eine Projektidee erkannt: '{intent.Project.Name}'. Wie wichtig/interessant ist dieses Projekt für dich?",
                    SuggestedOptions = JsonSerializer.Serialize(new[]
                    {
                        "Hohe Priorität, viel Zeit einplanen (6-8h/Woche)",
                        "Mittlere Priorität (4-6h/Woche)",
                        "Niedrige Priorität, nur gelegentlich (1-2h/Woche)",
                        "Macht viel Spaß, will viel Zeit investieren",
                        "Ist mir nicht so wichtig, vergiss es"
                    }),
                    Status = "pending",
                    RelatedDocumentId = documentId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (intent.Errors?.Count > 2)
            {
                interactions.Add(new UserInteraction
                {
                    UserId = userId,
                    InteractionType = "acknowledge_deficit",
                    Context = JsonSerializer.Serialize(new { ErrorCount = intent.Errors.Count, Subject = intent.LearningInfo?.Subject ?? "unbekannt" }),
                    Question = $"Ich habe {intent.Errors.Count} Fehler in deinem {intent.LearningInfo?.Subject ?? "Dokument"} erkannt. Möchtest du Übungsaufgaben dazu?",
                    SuggestedOptions = JsonSerializer.Serialize(new[]
                    {
                        "Ja, Übungen generieren und Lernzeit einplanen",
                        "Ja, nur Übungen generieren",
                        "Nein, später",
                        "Nein, kein Interesse"
                    }),
                    Status = "pending",
                    RelatedDocumentId = documentId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation($"Generated {interactions.Count} user interactions");
            return interactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating interactions");
            return interactions;
        }
    }
}
