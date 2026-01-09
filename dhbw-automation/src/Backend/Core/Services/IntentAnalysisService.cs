using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace DHBWAutomation.Backend.Core.Services;

public class IntentAnalysisService : IIntentAnalysisService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IntentAnalysisService> _logger;
    private readonly string? _anthropicApiKey;

    private const string AnthropicEndpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicModel = "claude-sonnet-4.5";
    private const string AnthropicVersion = "2023-06-01";

    public IntentAnalysisService(
        IHttpClientFactory httpClientFactory,
        ILogger<IntentAnalysisService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }

    public async Task<DocumentIntent> AnalyzeDocumentIntentAsync(string text, string documentType)
    {
        try
        {
            if (string.IsNullOrEmpty(_anthropicApiKey))
            {
                _logger.LogWarning("Anthropic API Key not configured");
                return new DocumentIntent
                {
                    PrimaryIntent = "unknown",
                    ActionRequired = "none"
                };
            }

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
   - PersonName, Purpose, SuggestedDate, SuggestedTime, EstimatedDurationMinutes

3. **TODOs**: Extrahiere ALLE Aufgaben
   - Title, Description, Priority (low/medium/high/urgent), SuggestedDeadline, Category

4. **Projekte**: Projektideen mit Name, Description, Requirements, Ideas, EstimatedPriority

5. **Fehler**: Erkenne fachliche Fehler (z.B. Mathe, Programmierung)
   - ErrorType (spelling/concept/calculation/logic)
   - Subject, Topic, Original, Corrected, Explanation, Severity

6. **Lerninhalt**: Subject, Topic, KeyConcepts, ComprehensionLevel (good/partial/poor), NeedsMoreStudy

Gib deine Antwort als JSON zurück im folgenden Format:
{
  ""primaryIntent"": ""..."",
  ""secondaryIntents"": [...],
  ""meeting"": {...} oder null,
  ""todos"": [...],
  ""project"": {...} oder null,
  ""errors"": [...],
  ""learningInfo"": {...} oder null,
  ""actionRequired"": ""ask_user"" | ""auto_create"" | ""none"",
  ""urgency"": ""low"" | ""medium"" | ""high"" | ""urgent""
}

Wichtig:
- Erkenne auch implizite Intents (z.B. ""demnächst mit paulina treffen"" = schedule_meeting)
- Bei Mathe/Programmierung: Prüfe auf Fehler in Berechnungen/Code
- Sei präzise bei Datums- und Zeitangaben
- Wenn unklar: ActionRequired = ""ask_user""";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", _anthropicApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

            var requestBody = new
            {
                model = AnthropicModel,
                max_tokens = 4096,
                temperature = 0.3,
                system = systemPrompt,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Analysiere dieses {documentType}-Dokument:\n\n{text.Substring(0, Math.Min(text.Length, 8000))}"
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(AnthropicEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseJson);

            var analysisText = result.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            // Parse the JSON response
            var intent = ParseIntentFromJson(analysisText);

            _logger.LogInformation($"Intent analysis complete: PrimaryIntent={intent.PrimaryIntent}");
            return intent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document intent");
            return new DocumentIntent
            {
                PrimaryIntent = "error",
                ActionRequired = "none"
            };
        }
    }

    private DocumentIntent ParseIntentFromJson(string json)
    {
        try
        {
            // Clean up JSON (Claude sometimes includes markdown code blocks)
            json = json.Trim();
            if (json.StartsWith("```json"))
            {
                json = json.Substring(7);
            }
            if (json.StartsWith("```"))
            {
                json = json.Substring(3);
            }
            if (json.EndsWith("```"))
            {
                json = json.Substring(0, json.Length - 3);
            }
            json = json.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var intent = new DocumentIntent
            {
                PrimaryIntent = root.GetProperty("primaryIntent").GetString() ?? "unknown",
                ActionRequired = root.GetProperty("actionRequired").GetString() ?? "none",
                Urgency = root.GetProperty("urgency").GetString() ?? "low"
            };

            // Parse secondary intents
            if (root.TryGetProperty("secondaryIntents", out var secondaryIntents))
            {
                intent.SecondaryIntents = secondaryIntents.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            // Parse meeting
            if (root.TryGetProperty("meeting", out var meeting) && meeting.ValueKind != JsonValueKind.Null)
            {
                intent.Meeting = new ExtractedMeeting
                {
                    PersonName = meeting.GetProperty("personName").GetString() ?? "",
                    Purpose = meeting.TryGetProperty("purpose", out var purpose) ? purpose.GetString() ?? "" : "",
                    EstimatedDurationMinutes = meeting.TryGetProperty("estimatedDurationMinutes", out var duration) ? duration.GetInt32() : 60
                };

                if (meeting.TryGetProperty("suggestedDate", out var suggestedDate) && suggestedDate.ValueKind == JsonValueKind.String)
                {
                    DateTime.TryParse(suggestedDate.GetString(), out var date);
                    intent.Meeting.SuggestedDate = date;
                }

                if (meeting.TryGetProperty("suggestedTime", out var suggestedTime))
                {
                    intent.Meeting.SuggestedTime = suggestedTime.GetString();
                }
            }

            // Parse todos
            if (root.TryGetProperty("todos", out var todos) && todos.ValueKind == JsonValueKind.Array)
            {
                foreach (var todo in todos.EnumerateArray())
                {
                    var extractedTodo = new ExtractedTodo
                    {
                        Title = todo.GetProperty("title").GetString() ?? "",
                        Description = todo.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        Priority = todo.TryGetProperty("priority", out var prio) ? prio.GetString() ?? "medium" : "medium",
                        Category = todo.TryGetProperty("category", out var cat) ? cat.GetString() ?? "general" : "general"
                    };

                    if (todo.TryGetProperty("suggestedDeadline", out var deadline) && deadline.ValueKind == JsonValueKind.String)
                    {
                        DateTime.TryParse(deadline.GetString(), out var deadlineDate);
                        extractedTodo.SuggestedDeadline = deadlineDate;
                    }

                    intent.Todos.Add(extractedTodo);
                }
            }

            // Parse project
            if (root.TryGetProperty("project", out var project) && project.ValueKind != JsonValueKind.Null)
            {
                intent.Project = new ExtractedProject
                {
                    Name = project.GetProperty("name").GetString() ?? "",
                    Description = project.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    EstimatedPriority = project.TryGetProperty("estimatedPriority", out var prio) ? prio.GetString() ?? "medium" : "medium"
                };

                if (project.TryGetProperty("requirements", out var reqs) && reqs.ValueKind == JsonValueKind.Array)
                {
                    intent.Project.Requirements = reqs.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }

                if (project.TryGetProperty("ideas", out var ideas) && ideas.ValueKind == JsonValueKind.Array)
                {
                    intent.Project.Ideas = ideas.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }

            // Parse errors
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                foreach (var error in errors.EnumerateArray())
                {
                    var detectedError = new DetectedError
                    {
                        ErrorType = error.GetProperty("errorType").GetString() ?? "concept",
                        Subject = error.TryGetProperty("subject", out var subj) ? subj.GetString() ?? "" : "",
                        Topic = error.TryGetProperty("topic", out var topic) ? topic.GetString() ?? "" : "",
                        Original = error.TryGetProperty("original", out var orig) ? orig.GetString() ?? "" : "",
                        Corrected = error.TryGetProperty("corrected", out var corr) ? corr.GetString() ?? "" : "",
                        Explanation = error.TryGetProperty("explanation", out var expl) ? expl.GetString() ?? "" : "",
                        Severity = error.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "low" : "low"
                    };

                    intent.Errors.Add(detectedError);
                }
            }

            // Parse learning info
            if (root.TryGetProperty("learningInfo", out var learningInfo) && learningInfo.ValueKind != JsonValueKind.Null)
            {
                intent.LearningInfo = new LearningContent
                {
                    Subject = learningInfo.GetProperty("subject").GetString() ?? "",
                    Topic = learningInfo.TryGetProperty("topic", out var topic) ? topic.GetString() ?? "" : "",
                    ComprehensionLevel = learningInfo.TryGetProperty("comprehensionLevel", out var comp) ? comp.GetString() ?? "good" : "good",
                    NeedsMoreStudy = learningInfo.TryGetProperty("needsMoreStudy", out var needs) && needs.GetBoolean()
                };

                if (learningInfo.TryGetProperty("keyConcepts", out var concepts) && concepts.ValueKind == JsonValueKind.Array)
                {
                    intent.LearningInfo.KeyConcepts = concepts.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }

            return intent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing intent JSON");
            return new DocumentIntent
            {
                PrimaryIntent = "parse_error",
                ActionRequired = "none"
            };
        }
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
