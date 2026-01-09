using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Shared.Helpers;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace DHBWAutomation.Backend.Core.Services;

public class IntentAnalysisService : IIntentAnalysisService
{
    private readonly AnthropicClient _anthropicClient;
    private readonly AiMetrics _aiMetrics;
    private readonly ILogger<IntentAnalysisService> _logger;

    private const string AnthropicModel = "claude-sonnet-4.5";

    public IntentAnalysisService(
        AnthropicClient anthropicClient,
        AiMetrics aiMetrics,
        ILogger<IntentAnalysisService> logger)
    {
        _anthropicClient = anthropicClient;
        _aiMetrics = aiMetrics;
        _logger = logger;
    }

    public async Task<DocumentIntent> AnalyzeDocumentIntentAsync(string text, string documentType)
    {
        return await _aiMetrics.TrackAsync("AnalyzeIntent", "Anthropic", AnthropicModel, async () =>
        {
            try
            {
                _logger.LogInformation("Analyzing document intent with Claude Sonnet 4.5");

                var systemPrompt = @"Du bist ein Experte für Intent-Erkennung in studentischen Dokumenten.

Analysiere den gegebenen Text und extrahiere strukturierte Informationen:

1. **Primary Intent**: Was möchte der Student mit diesem Dokument? Optionen:
   - 'schedule_meeting': Meeting/Termin planen
   - 'learning_content': Lerninhalt/Mitschrift
   - 'project_idea': Projektidee
   - 'todo': Aufgabenliste
   - 'question': Frage/Unklarheit
   - 'note': Allgemeine Notiz

2. **Meetings**: Extrahiere ALLE Erwähnungen von Treffen mit Personen
   - PersonName, Purpose, SuggestedDate, SuggestedTime, EstimatedDurationMinutes, ConfidenceScore (0-100)

3. **TODOs**: Extrahiere ALLE Aufgaben
   - Title, Description, Priority (low/medium/high/urgent), SuggestedDeadline, Category, ConfidenceScore (0-100)

4. **Projekte**: Projektideen mit Name, Description, Requirements, Ideas, EstimatedPriority, ConfidenceScore (0-100)

5. **Fehler**: Erkenne fachliche Fehler (z.B. Mathe, Programmierung)
   - ErrorType (spelling/concept/calculation/logic)
   - Subject, Topic, Original, Corrected, Explanation, Severity

6. **Lerninhalt**: Subject, Topic, KeyConcepts, ComprehensionLevel (good/partial/poor), NeedsMoreStudy

=== NEU: CONFIDENCE SCORES & FRAGEN-SYSTEM ===

Für jede extrahierte Entität (Meeting, TODO, Projekt):
- Gib einen ConfidenceScore (0-100) an:
  * 90-100: Sehr sicher, alle Daten klar
  * 70-89: Unsicher, einige Daten fehlen
  * 0-69: Sehr unklar, kritische Daten fehlen

Wenn Daten unklar/fehlen (ConfidenceScore < 90):
- Erstelle Klärungsfragen im ""questions""-Array
- Pro fehlendes/unklares Feld eine Frage

Frage-Format:
{
  ""fieldName"": ""meeting.suggestedDate"",  // oder ""todo.0.dueDate"" bei Listen
  ""questionText"": ""Wann genau möchtest du dieses Meeting planen?"",
  ""suggestedAnswers"": [""Montag 14:00"", ""Mittwoch 16:00"", ""Freitag 10:00"", ""Nächste Woche""],
  ""priority"": ""high"",  // critical, high, medium, low
  ""answerType"": ""datetime"",  // text, date, time, datetime, choice, number
  ""entityIndex"": 0  // nur bei Listen (todos), sonst null
}

Priority-Regeln:
- ""critical"": Entität ist OHNE diese Info unbrauchbar (z.B. Person bei Meeting fehlt)
- ""high"": Stark empfohlen (z.B. Datum bei Meeting fehlt)
- ""medium"": Hilfreich (z.B. Dauer bei Meeting fehlt)
- ""low"": Optional (z.B. Beschreibung bei TODO fehlt)

Beispiele für Fragen:
- Meeting ohne Datum: ""Wann möchtest du [Person] treffen?"" (priority: high)
- TODO ohne Deadline: ""Bis wann möchtest du das erledigen?"" (priority: medium)
- Projekt ohne Priorität: ""Wie wichtig ist dir dieses Projekt?"" (priority: medium)
- Meeting ohne Person: ""Mit wem möchtest du dich treffen?"" (priority: critical)

JSON-Format:
{
  ""primaryIntent"": ""..."",
  ""secondaryIntents"": [...],
  ""confidenceScore"": 85,  // Overall Score
  ""meeting"": {
    ""personName"": ""Paulina"",
    ""purpose"": ""Matheprojekt besprechen"",
    ""suggestedDate"": null,
    ""suggestedTime"": null,
    ""estimatedDurationMinutes"": 60,
    ""confidenceScore"": 65  // Niedrig weil Datum/Zeit fehlt
  },
  ""todos"": [...],
  ""project"": {...} oder null,
  ""errors"": [...],
  ""learningInfo"": {...} oder null,
  ""questions"": [
    {
      ""fieldName"": ""meeting.suggestedDate"",
      ""questionText"": ""Wann möchtest du Paulina treffen?"",
      ""suggestedAnswers"": [""Montag Nachmittag"", ""Mittwoch Nachmittag"", ""Freitag Vormittag"", ""Nächste Woche""],
      ""priority"": ""high"",
      ""answerType"": ""datetime"",
      ""entityIndex"": null
    },
    {
      ""fieldName"": ""meeting.suggestedTime"",
      ""questionText"": ""Um welche Uhrzeit ungefähr?"",
      ""suggestedAnswers"": [""10:00"", ""14:00"", ""16:00"", ""18:00""],
      ""priority"": ""medium"",
      ""answerType"": ""time"",
      ""entityIndex"": null
    }
  ],
  ""actionRequired"": ""ask_user"",
  ""urgency"": ""medium""
}

Wichtig:
- Erkenne auch implizite Intents (z.B. ""demnächst mit paulina treffen"" = schedule_meeting)
- Bei Mathe/Programmierung: Prüfe auf Fehler in Berechnungen/Code
- Sei präzise bei Datums- und Zeitangaben
- IMMER ConfidenceScore berechnen
- Wenn ConfidenceScore < 90: Erstelle passende Fragen
- Wenn unklar: ActionRequired = ""ask_user""";

                var userMessage = $"Analysiere dieses {documentType}-Dokument:\n\n{text.Substring(0, Math.Min(text.Length, 8000))}";

                var responseDoc = await _anthropicClient.ChatJsonAsync(
                    systemPrompt,
                    userMessage,
                    AnthropicModel,
                    maxTokens: 4096
                );

                // Parse the JSON response with defensive parsing
                var intent = ParseIntentFromJsonDocument(responseDoc);

                _logger.LogInformation($"Intent analysis complete: PrimaryIntent={intent.PrimaryIntent}");
                return intent;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("unavailable"))
            {
                _logger.LogWarning(ex, "Anthropic service unavailable - returning fallback intent");
                return new DocumentIntent
                {
                    PrimaryIntent = "unknown",
                    ActionRequired = "none"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document intent");
                return new DocumentIntent
                {
                    PrimaryIntent = "unknown",
                    ActionRequired = "none"
                };
            }
        });
    }

    private DocumentIntent ParseIntentFromJsonDocument(JsonDocument doc)
    {
        try
        {
            var root = doc.RootElement;

            var intent = new DocumentIntent
            {
                PrimaryIntent = TryGetString(root, "primaryIntent") ?? "unknown",
                ActionRequired = TryGetString(root, "actionRequired") ?? "none",
                Urgency = TryGetString(root, "urgency") ?? "low",
                ConfidenceScore = TryGetInt32(root, "confidenceScore") ?? 100
            };

            // Parse secondary intents
            if (root.TryGetProperty("secondaryIntents", out var secondaryIntents) && 
                secondaryIntents.ValueKind == JsonValueKind.Array)
            {
                intent.SecondaryIntents = secondaryIntents.EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => s!)
                    .ToList();
            }

            // Parse meeting (defensive)
            if (root.TryGetProperty("meeting", out var meeting) && meeting.ValueKind == JsonValueKind.Object)
            {
                intent.Meeting = new ExtractedMeeting
                {
                    PersonName = TryGetString(meeting, "personName") ?? "",
                    Purpose = TryGetString(meeting, "purpose") ?? "",
                    EstimatedDurationMinutes = TryGetInt32(meeting, "estimatedDurationMinutes") ?? 60,
                    ConfidenceScore = TryGetInt32(meeting, "confidenceScore") ?? 100
                };

                var dateStr = TryGetString(meeting, "suggestedDate");
                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                {
                    intent.Meeting.SuggestedDate = date;
                }

                intent.Meeting.SuggestedTime = TryGetString(meeting, "suggestedTime");
            }

            // Parse todos (defensive)
            if (root.TryGetProperty("todos", out var todos) && todos.ValueKind == JsonValueKind.Array)
            {
                foreach (var todo in todos.EnumerateArray())
                {
                    if (todo.ValueKind != JsonValueKind.Object) continue;

                    var extractedTodo = new ExtractedTodo
                    {
                        Title = TryGetString(todo, "title") ?? "",
                        Description = TryGetString(todo, "description"),
                        Priority = TryGetString(todo, "priority") ?? "medium",
                        Category = TryGetString(todo, "category") ?? "general",
                        ConfidenceScore = TryGetInt32(todo, "confidenceScore") ?? 100
                    };

                    var deadlineStr = TryGetString(todo, "suggestedDeadline");
                    if (!string.IsNullOrEmpty(deadlineStr) && DateTime.TryParse(deadlineStr, out var deadline))
                    {
                        extractedTodo.SuggestedDeadline = deadline;
                    }

                    intent.Todos.Add(extractedTodo);
                }
            }

            // Parse project (defensive)
            if (root.TryGetProperty("project", out var project) && project.ValueKind == JsonValueKind.Object)
            {
                intent.Project = new ExtractedProject
                {
                    Name = TryGetString(project, "name") ?? "",
                    Description = TryGetString(project, "description") ?? "",
                    EstimatedPriority = TryGetString(project, "estimatedPriority") ?? "medium",
                    ConfidenceScore = TryGetInt32(project, "confidenceScore") ?? 100
                };

                if (project.TryGetProperty("requirements", out var reqs) && reqs.ValueKind == JsonValueKind.Array)
                {
                    intent.Project.Requirements = reqs.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s!)
                        .ToList();
                }

                if (project.TryGetProperty("ideas", out var ideas) && ideas.ValueKind == JsonValueKind.Array)
                {
                    intent.Project.Ideas = ideas.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s!)
                        .ToList();
                }
            }

            // Parse questions (NEW for AI Staging System)
            if (root.TryGetProperty("questions", out var questions) && questions.ValueKind == JsonValueKind.Array)
            {
                foreach (var question in questions.EnumerateArray())
                {
                    if (question.ValueKind != JsonValueKind.Object) continue;

                    var extractedQuestion = new ExtractedQuestion
                    {
                        FieldName = TryGetString(question, "fieldName") ?? "",
                        QuestionText = TryGetString(question, "questionText") ?? "",
                        Priority = TryGetString(question, "priority") ?? "medium",
                        AnswerType = TryGetString(question, "answerType") ?? "text",
                        EntityIndex = TryGetInt32(question, "entityIndex")
                    };

                    // Parse suggested answers
                    if (question.TryGetProperty("suggestedAnswers", out var answers) && answers.ValueKind == JsonValueKind.Array)
                    {
                        extractedQuestion.SuggestedAnswers = answers.EnumerateArray()
                            .Select(e => e.GetString())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => s!)
                            .ToList();
                    }

                    intent.Questions.Add(extractedQuestion);
                }
            }

            // Parse errors (defensive)
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                foreach (var error in errors.EnumerateArray())
                {
                    if (error.ValueKind != JsonValueKind.Object) continue;

                    var detectedError = new DetectedError
                    {
                        ErrorType = TryGetString(error, "errorType") ?? "concept",
                        Subject = TryGetString(error, "subject") ?? "",
                        Topic = TryGetString(error, "topic") ?? "",
                        Original = TryGetString(error, "original") ?? "",
                        Corrected = TryGetString(error, "corrected") ?? "",
                        Explanation = TryGetString(error, "explanation") ?? "",
                        Severity = TryGetString(error, "severity") ?? "low"
                    };

                    intent.Errors.Add(detectedError);
                }
            }

            // Parse learning info (defensive)
            if (root.TryGetProperty("learningInfo", out var learningInfo) && learningInfo.ValueKind == JsonValueKind.Object)
            {
                intent.LearningInfo = new LearningContent
                {
                    Subject = TryGetString(learningInfo, "subject") ?? "",
                    Topic = TryGetString(learningInfo, "topic") ?? "",
                    ComprehensionLevel = TryGetString(learningInfo, "comprehensionLevel") ?? "partial",
                    NeedsMoreStudy = TryGetBool(learningInfo, "needsMoreStudy") ?? false
                };

                if (learningInfo.TryGetProperty("keyConcepts", out var concepts) && concepts.ValueKind == JsonValueKind.Array)
                {
                    intent.LearningInfo.KeyConcepts = concepts.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s!)
                        .ToList();
                }
            }

            return intent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON from Claude response");
            return new DocumentIntent
            {
                PrimaryIntent = "unknown",
                ActionRequired = "none"
            };
        }
    }

    // Defensive JSON helper methods
    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static int? TryGetInt32(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : null;
    }

    private static bool? TryGetBool(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.True) return true;
            if (prop.ValueKind == JsonValueKind.False) return false;
        }
        return null;
    }

    public async Task<List<UserInteraction>> GenerateInteractionsAsync(
        DocumentIntent intent,
        int userId,
        int documentId)
    {
        var interactions = new List<UserInteraction>();

        try
        {
            // Generate interaction for meeting
            if (intent.Meeting != null)
            {
                var meetingInteraction = new UserInteraction
                {
                    UserId = userId,
                    InteractionType = "schedule_meeting",
                    Context = JsonSerializer.Serialize(intent.Meeting),
                    Question = $"Du möchtest dich mit {intent.Meeting.PersonName} treffen" +
                              (string.IsNullOrEmpty(intent.Meeting.Purpose) ? "" : $" ({intent.Meeting.Purpose})") +
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
                };

                interactions.Add(meetingInteraction);
            }

            // Generate interaction for project
            if (intent.Project != null)
            {
                var projectInteraction = new UserInteraction
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
                };

                interactions.Add(projectInteraction);
            }

            // Generate interaction for learning deficits
            if (intent.Errors?.Count > 2)
            {
                var deficitInteraction = new UserInteraction
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
                };

                interactions.Add(deficitInteraction);
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
