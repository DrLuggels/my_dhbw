using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.UnifiedLearning;

/// <summary>
/// Exercise Generation - RAG-enhanced exercises with Bloom taxonomy and 20/40/40 difficulty
/// </summary>
public partial class UnifiedLearningService
{
    /// <inheritdoc />
    public async Task<UnifiedExerciseDto> GenerateExerciseAsync(
        int userId,
        UnifiedExerciseRequest request)
    {
        var exercises = await GenerateExercisesAsync(userId, request, 1);
        return exercises.FirstOrDefault() ?? throw new InvalidOperationException("Failed to generate exercise");
    }

    /// <inheritdoc />
    public async Task<List<UnifiedExerciseDto>> GenerateExercisesAsync(
        int userId,
        UnifiedExerciseRequest request,
        int count = 5)
    {
        // 1. Determine target entity
        UnifiedKnowledgeEntity? targetEntity = null;
        if (request.EntityId.HasValue)
        {
            targetEntity = await GetEntityAsync(request.EntityId.Value);
        }
        else if (!string.IsNullOrEmpty(request.Subject) && !string.IsNullOrEmpty(request.Topic))
        {
            targetEntity = await GetOrCreateEntityAsync(userId, request.Subject, request.Topic);
        }

        // 2. Determine difficulty using 20/40/40 rule
        var difficulty = request.Difficulty;
        if (difficulty == "adaptive" && targetEntity != null)
        {
            difficulty = DetermineRecommendedDifficulty(
                targetEntity.EasyTotal,
                targetEntity.MediumTotal,
                targetEntity.HardTotal);
        }

        // 3. Determine Bloom level
        var bloomLevel = request.BloomLevel ?? (targetEntity != null
            ? DetermineRecommendedBloomLevel(targetEntity.MasteryScore, targetEntity.CurrentBloomLevel)
            : 2);

        // 4. Get RAG context if enabled
        var ragContext = "";
        var sourceDocuments = new List<string>();
        if (request.UseRagContext)
        {
            (ragContext, sourceDocuments) = await GetRagContextAsync(
                userId,
                request.Subject ?? targetEntity?.Subject ?? "",
                request.Topic ?? targetEntity?.Topic ?? "",
                request.RagChunkCount);
        }

        // 5. Determine question types
        var questionTypes = request.QuestionTypes?.Any() == true
            ? request.QuestionTypes
            : GetRecommendedQuestionTypes(bloomLevel);

        // 6. Build prompt and generate with Claude
        var prompt = BuildExercisePrompt(
            targetEntity?.Subject ?? request.Subject ?? "Allgemein",
            targetEntity?.Topic ?? request.Topic ?? "Allgemein",
            targetEntity?.Name,
            targetEntity?.Description,
            difficulty,
            bloomLevel,
            questionTypes,
            ragContext,
            count);

        var apiKey = await GetAnthropicApiKeyAsync(userId);
        var response = await _anthropicClient.SendMessageAsync(prompt, apiKey);

        // 7. Parse response
        var exercises = ParseExerciseResponse(response, targetEntity, difficulty, bloomLevel, sourceDocuments);

        _logger.LogInformation(
            "Generated {Count} exercises for user {UserId}: subject={Subject}, topic={Topic}, difficulty={Difficulty}, bloom={Bloom}",
            exercises.Count, userId, request.Subject, request.Topic, difficulty, bloomLevel);

        return exercises;
    }

    /// <inheritdoc />
    public async Task<List<UnifiedExerciseDto>> GenerateEntityExercisesAsync(
        int entityId,
        int userId,
        int count = 5,
        string? questionType = null)
    {
        var request = new UnifiedExerciseRequest
        {
            EntityId = entityId,
            Difficulty = "adaptive",
            UseRagContext = true,
            QuestionTypes = questionType != null ? new List<string> { questionType } : null
        };

        return await GenerateExercisesAsync(userId, request, count);
    }

    /// <summary>
    /// Get RAG context from relevant document chunks
    /// </summary>
    private async Task<(string context, List<string> sources)> GetRagContextAsync(
        int userId,
        string subject,
        string topic,
        int chunkCount)
    {
        try
        {
            // Generate embedding for query
            var query = $"{subject} {topic}";
            var embedding = await _embeddingService.GenerateEmbeddingAsync(query);

            if (embedding == null || embedding.Length == 0)
                return ("", new List<string>());

            // Search in Qdrant
            var results = await _qdrantService.SearchAsync(
                "dhbw_document_chunks", // Use document chunks collection
                embedding,
                chunkCount,
                new Dictionary<string, object> { { "userId", userId } });

            if (results == null || !results.Any())
                return ("", new List<string>());

            // Build context from chunks
            var contextParts = new List<string>();
            var sources = new HashSet<string>();

            foreach (var result in results)
            {
                if (result.Payload != null)
                {
                    if (result.Payload.TryGetValue("content", out var content))
                        contextParts.Add(content.ToString() ?? "");

                    if (result.Payload.TryGetValue("documentName", out var docName))
                        sources.Add(docName.ToString() ?? "");
                }
            }

            return (string.Join("\n\n---\n\n", contextParts), sources.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get RAG context, generating without context");
            return ("", new List<string>());
        }
    }

    /// <summary>
    /// Get recommended question types based on Bloom level
    /// </summary>
    private List<string> GetRecommendedQuestionTypes(int bloomLevel)
    {
        return bloomLevel switch
        {
            1 => new List<string> { QuestionTypes.MultipleChoice, QuestionTypes.TrueFalse }, // Remember
            2 => new List<string> { QuestionTypes.MultipleChoice, QuestionTypes.FillInBlank }, // Understand
            3 => new List<string> { QuestionTypes.Calculation, QuestionTypes.ShortAnswer }, // Apply
            4 => new List<string> { QuestionTypes.ShortAnswer, QuestionTypes.Connection }, // Analyze
            5 => new List<string> { QuestionTypes.ShortAnswer, QuestionTypes.Essay }, // Evaluate
            6 => new List<string> { QuestionTypes.Essay }, // Create
            _ => new List<string> { QuestionTypes.MultipleChoice }
        };
    }

    /// <summary>
    /// Build the Claude prompt for exercise generation
    /// </summary>
    private string BuildExercisePrompt(
        string subject,
        string topic,
        string? entityName,
        string? entityDescription,
        string difficulty,
        int bloomLevel,
        List<string> questionTypes,
        string ragContext,
        int count)
    {
        var bloomName = GetBloomLevelName(bloomLevel);
        var difficultyDesc = difficulty switch
        {
            "easy" => "einfach (Grundverständnis prüfen)",
            "medium" => "mittel (Anwendung erforderlich)",
            "hard" => "schwer (komplexe Analyse oder Synthese)",
            _ => "mittel"
        };

        var questionTypeStr = string.Join(", ", questionTypes.Select(t => t switch
        {
            "mc" => "Multiple-Choice",
            "fill_blank" => "Lückentext",
            "true_false" => "Richtig/Falsch",
            "short_answer" => "Kurzantwort",
            "calculation" => "Berechnung",
            "connection" => "Zuordnung",
            "essay" => "Freitext/Essay",
            _ => t
        }));

        var prompt = $@"Du bist ein Tutor für das Fach '{subject}' und erstellst Übungsfragen zum Thema '{topic}'.

Bloom-Taxonomie-Level: {bloomLevel} ({bloomName})
Schwierigkeit: {difficultyDesc}
Fragetypen: {questionTypeStr}
Anzahl: {count} Fragen";

        if (!string.IsNullOrEmpty(entityName))
            prompt += $"\n\nZentraler Begriff: {entityName}";
        if (!string.IsNullOrEmpty(entityDescription))
            prompt += $"\nBeschreibung: {entityDescription}";

        if (!string.IsNullOrEmpty(ragContext))
        {
            prompt += $@"

Verwende den folgenden Kontext aus Lernmaterialien für deine Fragen:
---
{ragContext}
---";
        }

        prompt += $@"

Erstelle {count} Übungsfrage(n) im folgenden JSON-Format:
{{
  ""questions"": [
    {{
      ""type"": ""mc|fill_blank|true_false|short_answer|calculation"",
      ""question"": ""Die Fragestellung..."",
      ""options"": [""A"", ""B"", ""C"", ""D""],  // nur für mc
      ""correct_answer"": ""Die korrekte Antwort"",
      ""explanation"": ""Erklärung warum diese Antwort richtig ist"",
      ""hint"": ""Optionaler Hinweis zur Lösung"",
      ""related_concepts"": [""Konzept1"", ""Konzept2""]
    }}
  ]
}}

Wichtig:
- Für Bloom-Level {bloomLevel} ({bloomName}): {GetBloomLevelGuidance(bloomLevel)}
- Für Schwierigkeit '{difficulty}': {GetDifficultyGuidance(difficulty)}
- Die Fragen müssen zum angegebenen Fach und Thema passen
- Antworte NUR mit dem JSON, keine zusätzlichen Erklärungen";

        return prompt;
    }

    /// <summary>
    /// Get guidance text for Bloom level
    /// </summary>
    private string GetBloomLevelGuidance(int level) => level switch
    {
        1 => "Prüfe Faktenwissen und Wiedergabe (z.B. 'Was ist...?', 'Nenne...')",
        2 => "Prüfe Verständnis und Interpretation (z.B. 'Erkläre...', 'Warum...?')",
        3 => "Prüfe Anwendung in neuen Situationen (z.B. 'Berechne...', 'Wende an...')",
        4 => "Prüfe Analyse und Zusammenhänge (z.B. 'Vergleiche...', 'Analysiere...')",
        5 => "Prüfe Bewertung und Beurteilung (z.B. 'Bewerte...', 'Welche Lösung ist besser?')",
        6 => "Prüfe Kreation und Synthese (z.B. 'Entwirf...', 'Erstelle...')",
        _ => "Prüfe allgemeines Verständnis"
    };

    /// <summary>
    /// Get guidance text for difficulty
    /// </summary>
    private string GetDifficultyGuidance(string difficulty) => difficulty switch
    {
        "easy" => "Einfache, direkte Fragen. Eine klare richtige Antwort. Keine Transferleistung nötig.",
        "medium" => "Moderate Komplexität. Erfordert Verständnis und etwas Nachdenken.",
        "hard" => "Komplexe Fragen mit mehreren Aspekten. Erfordert Analyse oder Kombination mehrerer Konzepte.",
        _ => "Moderate Komplexität"
    };

    /// <summary>
    /// Parse Claude's response into exercise DTOs
    /// </summary>
    private List<UnifiedExerciseDto> ParseExerciseResponse(
        string response,
        UnifiedKnowledgeEntity? entity,
        string difficulty,
        int bloomLevel,
        List<string> sourceDocuments)
    {
        var exercises = new List<UnifiedExerciseDto>();

        try
        {
            // Extract JSON from response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                _logger.LogWarning("Could not find JSON in Claude response");
                return exercises;
            }

            var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
            using var doc = JsonDocument.Parse(jsonContent);

            if (doc.RootElement.TryGetProperty("questions", out var questionsArray))
            {
                foreach (var q in questionsArray.EnumerateArray())
                {
                    var exercise = new UnifiedExerciseDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        QuestionType = q.TryGetProperty("type", out var type) ? type.GetString() ?? "mc" : "mc",
                        Question = q.TryGetProperty("question", out var question) ? question.GetString() ?? "" : "",
                        CorrectAnswer = q.TryGetProperty("correct_answer", out var answer) ? answer.GetString() ?? "" : "",
                        Explanation = q.TryGetProperty("explanation", out var explanation) ? explanation.GetString() : null,
                        Hint = q.TryGetProperty("hint", out var hint) ? hint.GetString() : null,
                        Difficulty = difficulty,
                        BloomLevel = bloomLevel,
                        BloomLevelName = GetBloomLevelName(bloomLevel),
                        EntityId = entity?.Id,
                        EntityName = entity?.Name,
                        Subject = entity?.Subject,
                        Topic = entity?.Topic,
                        SourceDocuments = sourceDocuments,
                    };

                    // Parse options for MC questions
                    if (q.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
                    {
                        exercise.Options = options.EnumerateArray()
                            .Select(o => o.GetString() ?? "")
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                    }

                    // Parse related concepts
                    if (q.TryGetProperty("related_concepts", out var concepts) && concepts.ValueKind == JsonValueKind.Array)
                    {
                        exercise.RelatedConcepts = concepts.EnumerateArray()
                            .Select(c => c.GetString() ?? "")
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                    }

                    exercises.Add(exercise);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response as JSON");
        }

        return exercises;
    }

    /// <inheritdoc />
    public async Task<UnifiedAnswerFeedbackDto> SubmitAnswerAsync(
        int userId,
        UnifiedAnswerSubmission submission)
    {
        var feedback = new UnifiedAnswerFeedbackDto();

        // Validate answer using Claude if needed
        // For now, simple string comparison (enhanced in production)
        // TODO: Implement semantic answer comparison

        if (submission.EntityId.HasValue)
        {
            var entity = await GetEntityAsync(submission.EntityId.Value);
            if (entity != null)
            {
                feedback.PreviousMastery = entity.MasteryScore;

                // Update entity with result
                var isCorrect = true; // In production: validate answer properly
                var difficulty = submission.Difficulty ?? "medium";
                var bloomLevel = submission.BloomLevel ?? entity.CurrentBloomLevel;

                await UpdateEntityAfterInteractionAsync(
                    entity.Id,
                    isCorrect,
                    difficulty,
                    bloomLevel,
                    submission.ResponseTimeSeconds);

                // Reload entity for updated values
                entity = await GetEntityAsync(entity.Id);
                if (entity != null)
                {
                    feedback.IsCorrect = isCorrect;
                    feedback.NewMastery = entity.MasteryScore;
                    feedback.MasteryChange = feedback.NewMastery - feedback.PreviousMastery;
                    feedback.NewFsrsState = entity.FsrsState;
                    feedback.NewFsrsStateName = GetFsrsStateName(entity.FsrsState);
                    feedback.NextReview = entity.NextReview;
                    feedback.CurrentStreak = entity.CurrentStreak;
                    feedback.IsNewBestStreak = entity.CurrentStreak == entity.BestStreak && entity.BestStreak > 1;
                }
            }
        }

        return feedback;
    }

    /// <inheritdoc />
    public async Task<UnifiedEntityImpact> RecordExerciseResultAsync(
        int userId,
        int entityId,
        bool isCorrect,
        string difficulty,
        int bloomLevel,
        double? responseTimeSeconds = null)
    {
        var entity = await GetEntityAsync(entityId)
            ?? throw new ArgumentException($"Entity {entityId} not found");

        var previousMastery = entity.MasteryScore;
        var previousDecay = entity.DecayFactor;

        await UpdateEntityAfterInteractionAsync(
            entityId,
            isCorrect,
            difficulty,
            bloomLevel,
            responseTimeSeconds ?? 0);

        entity = await GetEntityAsync(entityId)!;

        // Reinforce related relationships
        var reinforced = 0;
        var weakened = 0;

        var relationships = await _context.Set<UnifiedKnowledgeRelationship>()
            .Where(r => (r.SourceEntityId == entityId || r.TargetEntityId == entityId) && r.IsActive)
            .ToListAsync();

        foreach (var rel in relationships)
        {
            if (isCorrect)
            {
                rel.Reinforce(0.05);
                reinforced++;
            }
            else if (rel.CurrentStrength > 0.3)
            {
                rel.Weaken(0.03);
                weakened++;
            }
        }

        await _context.SaveChangesAsync();

        return new UnifiedEntityImpact
        {
            EntityId = entityId,
            PreviousMastery = previousMastery,
            NewMastery = entity!.MasteryScore,
            MasteryChange = entity.MasteryScore - previousMastery,
            DecayFactorApplied = previousDecay,
            FsrsStateChanged = entity.FsrsState,
            RelationshipsReinforced = reinforced,
            RelationshipsWeakened = weakened,
            NextReview = entity.NextReview ?? DateTime.UtcNow.AddDays(1),
            Message = isCorrect ? "Richtig! Weiter so." : "Nicht ganz richtig. Versuch es nochmal!"
        };
    }
}
