using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Shared.Helpers;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace DHBWAutomation.Backend.Core.Services;

public class IntentAnalysisService : IIntentAnalysisService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiMetrics _aiMetrics;
    private readonly ILogger<IntentAnalysisService> _logger;
    private readonly AppDbContext _context;
    private readonly EncryptionHelper _encryptionHelper;

    private const string AnthropicModel = "claude-sonnet-4-5";
    private const string AnthropicEndpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    // Fallback API key from environment variables
    private readonly string? _anthropicApiKey;

    public IntentAnalysisService(
        IHttpClientFactory httpClientFactory,
        AiMetrics aiMetrics,
        ILogger<IntentAnalysisService> logger,
        AppDbContext context,
        EncryptionHelper encryptionHelper)
    {
        _httpClientFactory = httpClientFactory;
        _aiMetrics = aiMetrics;
        _logger = logger;
        _context = context;
        _encryptionHelper = encryptionHelper;

        // Load fallback API key from environment
        _anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }

    /// <summary>
    /// Holt den Anthropic API-Key - priorisiert User-Key vor globalem Key
    /// </summary>
    private async Task<string?> GetApiKeyAsync(int? userId)
    {
        _logger.LogInformation("🔑 GetApiKeyAsync called for Anthropic - UserId: {UserId}", userId);

        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);

            if (user != null && !string.IsNullOrEmpty(user.AnthropicApiKey))
            {
                _logger.LogInformation("🔑 User-specific Anthropic key found, decrypting...");
                var decrypted = _encryptionHelper.Decrypt(user.AnthropicApiKey);

                _logger.LogInformation("🔑 Decrypted Anthropic key (first 20 chars): {KeyPrefix}, Length: {Length}",
                    decrypted?.Substring(0, Math.Min(20, decrypted?.Length ?? 0)) ?? "null",
                    decrypted?.Length ?? 0);

                return decrypted;
            }
            else
            {
                _logger.LogWarning("🔑 User Anthropic key not found or empty, falling back to system key");
            }
        }
        else
        {
            _logger.LogWarning("🔑 UserId is NULL, using system fallback key for Anthropic");
        }

        // Fallback to system environment variable
        _logger.LogInformation("🔑 Using system Anthropic key, exists? {Exists}", !string.IsNullOrEmpty(_anthropicApiKey));
        return _anthropicApiKey;
    }

    public async Task<DocumentIntent> AnalyzeDocumentIntentAsync(string text, string documentType, int? userId = null)
    {
        return await _aiMetrics.TrackAsync("AnalyzeIntent", "Anthropic", AnthropicModel, async () =>
        {
            try
            {
                _logger.LogInformation("Analyzing document intent with Claude Sonnet 4.5 for user {UserId}", userId);

                // Get user-specific Anthropic API key
                var anthropicKey = await GetApiKeyAsync(userId);
                if (string.IsNullOrEmpty(anthropicKey))
                {
                    _logger.LogError("Anthropic API Key not configured for user {UserId} - cannot analyze intent", userId);
                    return new DocumentIntent
                    {
                        PrimaryIntent = "unknown",
                        ActionRequired = "none",
                        ConfidenceScore = 0
                    };
                }

                var systemPrompt = @"Du bist ein Experte für Intent-Erkennung in studentischen Dokumenten.

Analysiere den gegebenen Text und extrahiere strukturierte Informationen:

1. **Primary Intent**: Was möchte der Student mit diesem Dokument? Optionen:
   - 'schedule_meeting': Meeting/Termin planen (auch ""Termin eintragen"", ""Treffen planen"")
   - 'learning_content': Lerninhalt/Mitschrift
   - 'project_idea': Projektidee
   - 'todo': Allgemeine Aufgabenliste (KEINE Termine!)
   - 'question': Frage/Unklarheit
   - 'note': Allgemeine Notiz

2. **Meetings**: Extrahiere NUR echte, konkrete Meetings/Termine
   - ⚠️ WICHTIG: Nur extrahieren wenn WER, WANN oder WARUM klar ist
   - KEINE Meetings für: ""Termin eintragen"", ""xy erledigen"", ""irgendwann treffen""
   - Beispiele für echte Meetings:
     * ""mit Paulina treffen"" → Meeting (PersonName: Paulina)
     * ""Java Nachhilfe"" → Meeting (Purpose: Java Nachhilfe)
     * ""heute abend Treffen"" → Meeting (SuggestedDate: heute abend)
   - PersonName, Purpose, SuggestedDate, SuggestedTime, EstimatedDurationMinutes, ConfidenceScore (0-100)

3. **TODOs**: Extrahiere allgemeine Aufgaben (NIEMALS Meetings/Termine!)
   - ⚠️ WICHTIG: IGNORIERE Meta-Aufgaben wie:
     * ""Termin eintragen"", ""Termin die Tage eintragen""
     * ""xy erledigen"", ""Aufgabe xy muss noch gemacht werden"" (zu unspezifisch!)
     * Nur erstellen wenn KONKRETE Aufgabe beschrieben ist
   - ⚠️ Title ist PFLICHT! Wenn kein konkreter Title -> KEIN TODO erstellen!
   - Beispiele für echte TODOs: ""Hausaufgaben machen"", ""Code refactoren"", ""Präsentation vorbereiten""
   - Title, Description, Priority (low/medium/high/urgent), SuggestedDeadline, Category, ConfidenceScore (0-100)

4. **Projekte**: Projektideen mit Name, Description, Requirements, Ideas, EstimatedPriority, ConfidenceScore (0-100)

5. **Fehler**: Erkenne fachliche Fehler (z.B. Mathe, Programmierung)
   - ErrorType (spelling/concept/calculation/logic)
   - Subject, Topic, Original, Corrected, Explanation, Severity

6. **Lerninhalt**: Subject, Topic, KeyConcepts, ComprehensionLevel (good/partial/poor), NeedsMoreStudy

=== CONFIDENCE SCORES & FRAGEN-SYSTEM ===

⚠️ KRITISCH: ERSTELLE IMMER FRAGEN WENN:
- Wichtige Daten fehlen (Person, Datum, Zeit bei Meetings)
- Unklare Formulierungen (z.B. ""demnächst"", ""bald"")
- Verwirrende Details (z.B. unerwartete Zahlen wie ""1800"")
- ConfidenceScore < 90

Für jede extrahierte Entität (Meeting, TODO, Projekt):
- Gib einen ConfidenceScore (0-100) an:
  * 90-100: Sehr sicher, alle Daten klar
  * 70-89: Unsicher, einige Daten fehlen
  * 0-69: Sehr unklar, kritische Daten fehlen

Frage-Format:
{
  ""fieldName"": ""meetings.0.suggestedDate"",  // Format: ""meetings.INDEX.field"" oder ""todos.INDEX.field""
  ""questionText"": ""Wann genau möchtest du dieses Meeting planen?"",
  ""suggestedAnswers"": [""Montag 14:00"", ""Mittwoch 16:00"", ""Freitag 10:00"", ""Nächste Woche""],
  ""priority"": ""high"",  // critical, high, medium, low
  ""answerType"": ""datetime"",  // text, date, time, datetime, choice, number
  ""entityIndex"": 0  // Index im meetings/todos Array
}

Priority-Regeln:
- ""critical"": Entität ist OHNE diese Info unbrauchbar (z.B. Person bei Meeting fehlt)
- ""high"": Stark empfohlen (z.B. Datum bei Meeting fehlt)
- ""medium"": Hilfreich (z.B. Dauer bei Meeting fehlt)
- ""low"": Optional (z.B. Beschreibung bei TODO fehlt)

Beispiele für Fragen:
- Meeting ohne Datum: ""Wann möchtest du [Person] treffen?"" (priority: high)
- Unklare Zahl: ""Was bedeutet '1800' in deinem Text?"" (priority: medium)
- TODO ohne Deadline: ""Bis wann möchtest du das erledigen?"" (priority: medium)
- Meeting ohne Person: ""Mit wem möchtest du dich treffen?"" (priority: critical)

JSON-Format (WICHTIG: meetings ist ARRAY!):
{
  ""primaryIntent"": ""..."",
  ""secondaryIntents"": [...],
  ""confidenceScore"": 85,  // Overall Score
  ""meetings"": [
    {
      ""personName"": ""Paulina"",
      ""purpose"": ""Treffen"",
      ""suggestedDate"": null,
      ""suggestedTime"": null,
      ""estimatedDurationMinutes"": 60,
      ""confidenceScore"": 40
    },
    {
      ""personName"": null,
      ""purpose"": ""Java Nachhilfe"",
      ""suggestedDate"": null,
      ""suggestedTime"": null,
      ""estimatedDurationMinutes"": 90,
      ""confidenceScore"": 30
    }
  ],
  ""todos"": [...],
  ""project"": {...} oder null,
  ""errors"": [...],
  ""learningInfo"": {...} oder null,
  ""questions"": [
    {
      ""fieldName"": ""meetings.0.suggestedDate"",
      ""questionText"": ""Wann möchtest du Paulina treffen?"",
      ""suggestedAnswers"": [""Heute Abend"", ""Morgen"", ""Diese Woche"", ""Nächste Woche""],
      ""priority"": ""high"",
      ""answerType"": ""datetime"",
      ""entityIndex"": 0
    },
    {
      ""fieldName"": ""meetings.1.personName"",
      ""questionText"": ""Von wem benötigst du Java Nachhilfe?"",
      ""suggestedAnswers"": [""Tutor suchen"", ""Kommilitone"", ""Professor"", ""Online""],
      ""priority"": ""critical"",
      ""answerType"": ""text"",
      ""entityIndex"": 1
    }
  ],
  ""actionRequired"": ""ask_user"",
  ""urgency"": ""medium""
}

Wichtig:
- ⚠️ TRENNE verschiedene Meetings/TODOs - vermische sie NICHT!
- ⚠️ ERSTELLE Fragen wenn wichtige Daten fehlen oder unklar sind
- ⚠️ KEINE DUPLIKAT-FRAGEN! Jedes Feld nur EINMAL fragen, nicht mehrfach mit verschiedener Formulierung
- ⚠️ ""Termin eintragen"" = TODO, NICHT Meeting! (außer es ist klar mit wem/wann)
- Erkenne implizite Intents (z.B. ""demnächst mit paulina treffen"" = schedule_meeting)
- Bei Mathe/Programmierung: Prüfe auf Fehler in Berechnungen/Code
- Sei präzise bei Datums- und Zeitangaben
- IMMER ConfidenceScore berechnen
- Wenn ConfidenceScore < 90: Erstelle passende Fragen (KEINE Duplikate!)
- Wenn unklar: ActionRequired = ""ask_user""";

                var userMessage = $"Analysiere dieses {documentType}-Dokument:\n\n{text.Substring(0, Math.Min(text.Length, 8000))}";

                // Manual HTTP request to Anthropic API with user-specific key
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", anthropicKey);
                client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

                var requestBody = new
                {
                    model = AnthropicModel,
                    max_tokens = 4096,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = userMessage
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(AnthropicEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Anthropic API error: Status={Status}, Content={Content}",
                        response.StatusCode, errorContent);
                    throw new InvalidOperationException($"Anthropic API error: {response.StatusCode}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Anthropic response received, parsing JSON...");

                // DEBUG: Log full Anthropic response
                _logger.LogWarning("🔍 DEBUG: Full Anthropic response (first 1000 chars): {Response}",
                    responseJson.Substring(0, Math.Min(1000, responseJson.Length)));

                // Parse response - Anthropic returns { "content": [{ "text": "..." }] }
                var responseDoc = JsonDocument.Parse(responseJson);
                var contentArray = responseDoc.RootElement.GetProperty("content");
                var textContent = contentArray[0].GetProperty("text").GetString() ?? "{}";

                // DEBUG: Log extracted text content
                _logger.LogWarning("🔍 DEBUG: Extracted text content (first 1000 chars): {TextContent}",
                    textContent?.Substring(0, Math.Min(1000, textContent?.Length ?? 0)) ?? "null");

                // Remove markdown code blocks if present (Claude often wraps JSON in ```json ... ```)
                textContent = RemoveMarkdownCodeBlocks(textContent);

                _logger.LogWarning("🔍 DEBUG: After removing markdown (first 500 chars): {CleanedContent}",
                    textContent?.Substring(0, Math.Min(500, textContent?.Length ?? 0)) ?? "null");

                // Parse the text content as JSON (it should be the DocumentIntent JSON)
                var intentDoc = JsonDocument.Parse(textContent);

                // Parse the JSON response with defensive parsing
                var intent = ParseIntentFromJsonDocument(intentDoc);

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

    /// <summary>
    /// Removes markdown code blocks from text (e.g., ```json ... ```)
    /// Claude often wraps JSON responses in markdown code blocks
    /// </summary>
    private string RemoveMarkdownCodeBlocks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Remove opening markdown code block (```json or ```)
        text = text.Trim();
        if (text.StartsWith("```json"))
        {
            text = text.Substring(7).TrimStart(); // Remove ```json and leading whitespace
        }
        else if (text.StartsWith("```"))
        {
            text = text.Substring(3).TrimStart(); // Remove ``` and leading whitespace
        }

        // Remove closing markdown code block (```)
        // Use regex to handle any trailing whitespace/newlines after the backticks
        var closingBacktickIndex = text.LastIndexOf("```");
        if (closingBacktickIndex >= 0)
        {
            text = text.Substring(0, closingBacktickIndex).TrimEnd();
        }

        return text.Trim();
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

            // Parse meetings (defensive) - handle both "meetings" array and "meeting" object
            if (root.TryGetProperty("meetings", out var meetings) && meetings.ValueKind == JsonValueKind.Array)
            {
                var meetingsArray = meetings.EnumerateArray().ToList();
                _logger.LogInformation($"📅 Found {meetingsArray.Count} meetings in array");

                foreach (var meetingElement in meetingsArray)
                {
                    if (meetingElement.ValueKind != JsonValueKind.Object) continue;

                    var extractedMeeting = new ExtractedMeeting
                    {
                        PersonName = TryGetString(meetingElement, "personName") ?? "",
                        Purpose = TryGetString(meetingElement, "purpose") ?? "",
                        EstimatedDurationMinutes = TryGetInt32(meetingElement, "estimatedDurationMinutes") ?? 60,
                        ConfidenceScore = TryGetInt32(meetingElement, "confidenceScore") ?? 100
                    };

                    var dateStr = TryGetString(meetingElement, "suggestedDate");
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                    {
                        extractedMeeting.SuggestedDate = date;
                    }

                    extractedMeeting.SuggestedTime = TryGetString(meetingElement, "suggestedTime");

                    intent.Meetings.Add(extractedMeeting);
                }
            }
            // Fallback to "meeting" object (for backward compatibility)
            else if (root.TryGetProperty("meeting", out var meeting) && meeting.ValueKind == JsonValueKind.Object)
            {
                _logger.LogInformation("📅 Found single meeting object (legacy format)");

                var extractedMeeting = new ExtractedMeeting
                {
                    PersonName = TryGetString(meeting, "personName") ?? "",
                    Purpose = TryGetString(meeting, "purpose") ?? "",
                    EstimatedDurationMinutes = TryGetInt32(meeting, "estimatedDurationMinutes") ?? 60,
                    ConfidenceScore = TryGetInt32(meeting, "confidenceScore") ?? 100
                };

                var dateStr = TryGetString(meeting, "suggestedDate");
                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                {
                    extractedMeeting.SuggestedDate = date;
                }

                extractedMeeting.SuggestedTime = TryGetString(meeting, "suggestedTime");

                intent.Meetings.Add(extractedMeeting);
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
            // Generate interaction for each meeting
            foreach (var meeting in intent.Meetings)
            {
                var meetingInteraction = new UserInteraction
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
