using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DHBWAutomation.Backend.Core.Services.Embedding;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for generating personalized exercises using RAG (Retrieval-Augmented Generation).
/// Combines user's document chunks with Claude for context-aware exercise generation.
/// </summary>
public class RagExerciseService : IRagExerciseService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly IAdaptiveDifficultyService _difficultyService;
    private readonly IDeadlinePriorityService _priorityService;
    private readonly AnthropicClient _anthropicClient;
    private readonly ILogger<RagExerciseService> _logger;

    private const int DefaultMaxContextChunks = 5;
    private const double DefaultSimilarityThreshold = 0.5;

    public RagExerciseService(
        AppDbContext dbContext,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        IAdaptiveDifficultyService difficultyService,
        IDeadlinePriorityService priorityService,
        AnthropicClient anthropicClient,
        ILogger<RagExerciseService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _difficultyService = difficultyService;
        _priorityService = priorityService;
        _anthropicClient = anthropicClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RagExerciseResult> GenerateExerciseAsync(
        int userId,
        int nodeId,
        string difficulty,
        ExerciseGenerationOptions? options = null)
    {
        options ??= new ExerciseGenerationOptions();

        var node = await _dbContext.UserKnowledgeNodes.FindAsync(nodeId);
        if (node == null)
        {
            throw new ArgumentException($"Node {nodeId} not found", nameof(nodeId));
        }

        return await GenerateExerciseForTopicAsync(
            userId,
            node.Subject,
            node.Topic,
            difficulty,
            options);
    }

    /// <inheritdoc />
    public async Task<RagExerciseResult> GenerateExerciseForTopicAsync(
        int userId,
        string subject,
        string topic,
        string difficulty,
        ExerciseGenerationOptions? options = null)
    {
        options ??= new ExerciseGenerationOptions();

        // Step 1: Retrieve relevant context chunks using RAG
        var chunks = new List<RetrievedChunk>();
        if (options.UseRag)
        {
            chunks = await RetrieveContextAsync(
                userId,
                $"{subject}: {topic}",
                options.MaxContextChunks,
                options.SimilarityThreshold);

            _logger.LogInformation("Retrieved {Count} context chunks for topic '{Topic}'", chunks.Count, topic);
        }

        // Step 2: Build context string from chunks
        var context = BuildContextFromChunks(chunks);

        // Step 3: Generate exercise using Claude with context
        var exercise = await GenerateWithClaudeAsync(
            subject,
            topic,
            difficulty,
            context,
            options);

        // Step 4: Populate metadata
        exercise.Subject = subject;
        exercise.Topic = topic;
        exercise.Difficulty = difficulty;
        exercise.WasRagUsed = chunks.Count > 0;
        exercise.UsedChunks = chunks;
        exercise.SourceDocuments = chunks.Select(c => c.DocumentName).Distinct().ToList();

        // Try to find associated node
        var node = await _dbContext.UserKnowledgeNodes
            .FirstOrDefaultAsync(n => n.UserId == userId && n.Subject == subject && n.Topic == topic);
        if (node != null)
        {
            exercise.NodeId = node.Id;
            exercise.EstimatedSuccessProbability =
                await _difficultyService.EstimateSuccessProbabilityAsync(node.Id, difficulty);
        }

        _logger.LogInformation(
            "Generated {Type} exercise for {Subject}/{Topic} ({Difficulty}) with {ChunkCount} context chunks",
            exercise.Type, subject, topic, difficulty, chunks.Count);

        return exercise;
    }

    /// <inheritdoc />
    public async Task<List<RetrievedChunk>> RetrieveContextAsync(
        int userId,
        string topic,
        int topK = 5,
        double threshold = 0.5)
    {
        var chunks = new List<RetrievedChunk>();

        try
        {
            // Generate embedding for the topic
            var embedding = await _embeddingService.GenerateEmbeddingAsync(topic, userId);
            if (embedding == null)
            {
                _logger.LogWarning("Could not generate embedding for topic '{Topic}'", topic);
                return chunks;
            }

            // Search in document chunks collection
            var results = await _qdrantService.SearchSimilarAsync(
                QdrantCollections.Chunks,
                embedding,
                topK,
                threshold,
                userId);

            _logger.LogDebug("Qdrant returned {Count} results for topic '{Topic}'", results.Count, topic);

            // Fetch full chunk details from database
            var chunkIds = results
                .Where(r => r.EntityType == KnowledgeEntityTypes.DocumentChunk)
                .Select(r => r.EntityId)
                .ToList();

            var dbChunks = await _dbContext.DocumentChunks
                .Include(c => c.Document)
                .Where(c => chunkIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            foreach (var result in results.Where(r => r.EntityType == KnowledgeEntityTypes.DocumentChunk))
            {
                if (dbChunks.TryGetValue(result.EntityId, out var chunk))
                {
                    chunks.Add(new RetrievedChunk
                    {
                        ChunkId = chunk.Id,
                        DocumentId = chunk.DocumentId,
                        DocumentName = chunk.Document.FileName,
                        Content = chunk.Content,
                        TopicLabel = chunk.TopicLabel,
                        Summary = chunk.Summary,
                        SimilarityScore = result.Score,
                        PageNumbers = chunk.PageNumbers
                    });
                }
            }

            // Sort by similarity score
            chunks = chunks.OrderByDescending(c => c.SimilarityScore).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving context chunks for topic '{Topic}'", topic);
        }

        return chunks;
    }

    /// <inheritdoc />
    public async Task<List<RagExerciseResult>> GenerateSessionExercisesAsync(
        int userId,
        int count = 5,
        ExerciseGenerationOptions? options = null)
    {
        options ??= new ExerciseGenerationOptions();
        var exercises = new List<RagExerciseResult>();

        // Get recommended topics based on priority
        var recommendations = await _priorityService.GetRecommendedTopicsAsync(userId, count);

        foreach (var rec in recommendations)
        {
            try
            {
                // Select difficulty adaptively
                var difficultySelection = await _difficultyService.SelectDifficultyAsync(userId, rec.NodeId);

                var exercise = await GenerateExerciseAsync(
                    userId,
                    rec.NodeId,
                    difficultySelection.Difficulty,
                    options);

                exercises.Add(exercise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating exercise for node {NodeId}", rec.NodeId);
            }
        }

        return exercises;
    }

    /// <inheritdoc />
    public async Task<List<RagExerciseResult>> GenerateExamExercisesAsync(
        int userId,
        string subject,
        int easyCount = 4,
        int mediumCount = 8,
        int hardCount = 8)
    {
        var exercises = new List<RagExerciseResult>();
        var options = new ExerciseGenerationOptions
        {
            IncludeHints = false, // No hints in exam mode
            UseRag = true
        };

        // Get all nodes for this subject
        var nodes = await _dbContext.UserKnowledgeNodes
            .Where(n => n.UserId == userId && n.Subject == subject)
            .ToListAsync();

        if (nodes.Count == 0)
        {
            _logger.LogWarning("No knowledge nodes found for subject '{Subject}'", subject);
            return exercises;
        }

        // Shuffle nodes for variety
        var shuffledNodes = nodes.OrderBy(_ => Guid.NewGuid()).ToList();

        // Generate easy questions
        for (int i = 0; i < easyCount && i < shuffledNodes.Count; i++)
        {
            try
            {
                var exercise = await GenerateExerciseAsync(userId, shuffledNodes[i].Id, "easy", options);
                exercises.Add(exercise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating easy exercise");
            }
        }

        // Generate medium questions
        for (int i = 0; i < mediumCount && i < shuffledNodes.Count; i++)
        {
            var nodeIndex = (i + easyCount) % shuffledNodes.Count;
            try
            {
                var exercise = await GenerateExerciseAsync(userId, shuffledNodes[nodeIndex].Id, "medium", options);
                exercises.Add(exercise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating medium exercise");
            }
        }

        // Generate hard questions
        for (int i = 0; i < hardCount && i < shuffledNodes.Count; i++)
        {
            var nodeIndex = (i + easyCount + mediumCount) % shuffledNodes.Count;
            try
            {
                var exercise = await GenerateExerciseAsync(userId, shuffledNodes[nodeIndex].Id, "hard", options);
                exercises.Add(exercise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hard exercise");
            }
        }

        _logger.LogInformation(
            "Generated exam exercises: {Easy} easy, {Medium} medium, {Hard} hard for subject '{Subject}'",
            exercises.Count(e => e.Difficulty == "easy"),
            exercises.Count(e => e.Difficulty == "medium"),
            exercises.Count(e => e.Difficulty == "hard"),
            subject);

        return exercises;
    }

    #region Private Helper Methods

    /// <summary>
    /// Builds a context string from retrieved chunks for the prompt.
    /// </summary>
    private string BuildContextFromChunks(List<RetrievedChunk> chunks)
    {
        if (chunks.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("=== KONTEXT AUS DEINEN DOKUMENTEN ===\n");

        foreach (var chunk in chunks)
        {
            sb.AppendLine($"--- Quelle: {chunk.DocumentName} (Seite {chunk.PageNumbers ?? "?"}) ---");
            if (!string.IsNullOrEmpty(chunk.TopicLabel))
            {
                sb.AppendLine($"Thema: {chunk.TopicLabel}");
            }
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates an exercise using Claude with optional RAG context.
    /// </summary>
    private async Task<RagExerciseResult> GenerateWithClaudeAsync(
        string subject,
        string topic,
        string difficulty,
        string context,
        ExerciseGenerationOptions options)
    {
        var systemPrompt = BuildSystemPrompt(options);
        var userPrompt = BuildUserPrompt(subject, topic, difficulty, context, options);

        var response = await _anthropicClient.ChatAsync(
            systemPrompt,
            userPrompt,
            model: "claude-sonnet-4-5",
            maxTokens: 2048,
            temperature: 0.7);

        return ParseExerciseResponse(response, options.ExerciseType);
    }

    /// <summary>
    /// Builds the system prompt for exercise generation.
    /// </summary>
    private string BuildSystemPrompt(ExerciseGenerationOptions options)
    {
        var language = options.Language == "de" ? "Deutsch" : "English";

        return $@"Du bist ein erfahrener Tutor, der personalisierte Übungsaufgaben erstellt.

WICHTIGE REGELN:
1. Erstelle Aufgaben auf {language}
2. Nutze den bereitgestellten Kontext aus den Dokumenten des Nutzers, falls vorhanden
3. Die Aufgabe muss klar und eindeutig formuliert sein
4. Gib die Antwort IMMER im JSON-Format zurück

AUFGABENTYP: {options.ExerciseType}
- multiple_choice: 4 Antwortmöglichkeiten (A, B, C, D), genau eine richtig
- fill_blank: Lückentext mit eindeutiger Antwort
- true_false: Wahr/Falsch-Aussage
- open_ended: Offene Frage mit Musterantwort

JSON-FORMAT:
{{
    ""question"": ""Die Frage"",
    ""options"": [""A) Option 1"", ""B) Option 2"", ""C) Option 3"", ""D) Option 4""],  // nur bei multiple_choice
    ""correct_answer"": ""Die korrekte Antwort"",
    ""explanation"": ""Ausführliche Erklärung der Lösung"",
    ""hints"": [""Hinweis 1"", ""Hinweis 2""]  // optional
}}";
    }

    /// <summary>
    /// Builds the user prompt for exercise generation.
    /// </summary>
    private string BuildUserPrompt(
        string subject,
        string topic,
        string difficulty,
        string context,
        ExerciseGenerationOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Erstelle eine {GetDifficultyDescription(difficulty)} Übungsaufgabe:");
        sb.AppendLine($"- Fach: {subject}");
        sb.AppendLine($"- Thema: {topic}");
        sb.AppendLine($"- Aufgabentyp: {options.ExerciseType}");

        if (options.FocusSubtopics?.Count > 0)
        {
            sb.AppendLine($"- Fokus auf: {string.Join(", ", options.FocusSubtopics)}");
        }

        if (!string.IsNullOrEmpty(context))
        {
            sb.AppendLine();
            sb.AppendLine(context);
            sb.AppendLine();
            sb.AppendLine("WICHTIG: Beziehe dich auf den obigen Kontext aus den Dokumenten des Nutzers!");
        }

        if (!options.IncludeHints)
        {
            sb.AppendLine();
            sb.AppendLine("HINWEIS: Keine Tipps/Hints in dieser Aufgabe (Prüfungsmodus).");
        }

        sb.AppendLine();
        sb.AppendLine("Antworte NUR mit dem JSON-Objekt, keine zusätzliche Erklärung.");

        return sb.ToString();
    }

    /// <summary>
    /// Gets a German description for the difficulty level.
    /// </summary>
    private string GetDifficultyDescription(string difficulty)
    {
        return difficulty.ToLower() switch
        {
            "easy" => "einfache (Grundlagen)",
            "medium" => "mittelschwere",
            "hard" => "schwierige (fortgeschritten)",
            _ => "mittelschwere"
        };
    }

    /// <summary>
    /// Parses Claude's response into a RagExerciseResult object.
    /// </summary>
    private RagExerciseResult ParseExerciseResponse(string response, string exerciseType)
    {
        var exercise = new RagExerciseResult
        {
            Type = exerciseType
        };

        try
        {
            // Try to extract JSON from response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                if (root.TryGetProperty("question", out var question))
                {
                    exercise.Question = question.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
                {
                    exercise.Options = options.EnumerateArray()
                        .Select(o => o.GetString() ?? string.Empty)
                        .ToList();
                }

                if (root.TryGetProperty("correct_answer", out var answer))
                {
                    exercise.CorrectAnswer = answer.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("explanation", out var explanation))
                {
                    exercise.Explanation = explanation.GetString();
                }

                if (root.TryGetProperty("hints", out var hints) && hints.ValueKind == JsonValueKind.Array)
                {
                    exercise.Hints = hints.EnumerateArray()
                        .Select(h => h.GetString() ?? string.Empty)
                        .ToList();
                }
            }
            else
            {
                // Fallback: use entire response as question
                exercise.Question = response;
                exercise.CorrectAnswer = "Unable to parse response";
                _logger.LogWarning("Could not parse JSON from Claude response, using raw text");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing exercise JSON response");
            exercise.Question = response;
            exercise.CorrectAnswer = "Parse error";
        }

        return exercise;
    }

    #endregion
}
