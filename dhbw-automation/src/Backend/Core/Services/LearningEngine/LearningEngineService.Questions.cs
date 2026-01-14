using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.LearningEngine;

/// <summary>
/// Question generation and user performance tracking for the Learning Engine.
/// </summary>
public partial class LearningEngineService
{
    /// <summary>
    /// Generates questions based on document content and knowledge graph.
    /// </summary>
    public async Task<List<LearningQuestionDto>> GenerateQuestionsAsync(
        int userId,
        QuestionGenerationRequest request)
    {
        var questions = new List<LearningQuestionDto>();

        // Get relevant entities
        var entityQuery = _context.KgEntities
            .Include(e => e.Chunk)
            .Include(e => e.Document)
            .Where(e => e.UserId == userId && e.IsActive);

        if (request.DocumentIds?.Any() == true)
        {
            entityQuery = entityQuery.Where(e => e.DocumentId.HasValue &&
                request.DocumentIds.Contains(e.DocumentId.Value));
        }

        if (request.EntityIds?.Any() == true)
        {
            entityQuery = entityQuery.Where(e => request.EntityIds.Contains(e.Id));
        }

        if (!string.IsNullOrEmpty(request.Subject))
        {
            entityQuery = entityQuery.Where(e => e.Subject == request.Subject);
        }

        if (!string.IsNullOrEmpty(request.Topic))
        {
            entityQuery = entityQuery.Where(e => e.Topic != null && e.Topic.Contains(request.Topic));
        }

        // Get entities sorted by importance
        var entities = await entityQuery
            .OrderByDescending(e => e.ImportanceScore)
            .Take(request.Count * 2) // Get more than needed for variety
            .ToListAsync();

        if (!entities.Any())
        {
            _logger.LogWarning("No entities found for question generation");
            return questions;
        }

        // Determine question types to use
        var questionTypes = request.QuestionTypes?.Any() == true
            ? request.QuestionTypes
            : new List<string> { QuestionTypes.MultipleChoice, QuestionTypes.FillInBlank };

        // Determine Bloom levels to use
        var minBloom = request.MinBloomLevel ?? 1;
        var maxBloom = request.MaxBloomLevel ?? 4;

        // If adaptive, get user performance to determine appropriate levels
        if (request.Difficulty == "adaptive")
        {
            var performances = await _context.UserEntityPerformances
                .Where(p => p.UserId == userId)
                .ToListAsync();

            if (performances.Any())
            {
                var avgMastery = performances.Average(p => p.MasteryScore);
                // Adjust Bloom level based on average mastery
                minBloom = avgMastery switch
                {
                    < 0.3 => 1,
                    < 0.5 => 2,
                    < 0.7 => 3,
                    < 0.9 => 4,
                    _ => 5
                };
                maxBloom = Math.Min(minBloom + 2, 6);
            }
        }

        // Generate questions for selected entities
        var questionCount = 0;
        var random = new Random();

        foreach (var entity in entities.OrderBy(_ => random.Next()))
        {
            if (questionCount >= request.Count) break;

            var questionType = questionTypes[random.Next(questionTypes.Count)];
            var bloomLevel = random.Next(minBloom, maxBloom + 1);

            try
            {
                var question = await GenerateQuestionForEntityAsync(
                    entity,
                    questionType,
                    bloomLevel,
                    userId);

                if (question != null)
                {
                    questions.Add(question);
                    questionCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate question for entity {EntityId}", entity.Id);
            }

            // Rate limiting
            await Task.Delay(300);
        }

        return questions;
    }

    /// <summary>
    /// Generates questions for a specific entity.
    /// </summary>
    public async Task<List<LearningQuestionDto>> GenerateEntityQuestionsAsync(
        int entityId,
        int userId,
        int count = 5,
        string? questionType = null,
        int? bloomLevel = null)
    {
        var entity = await _context.KgEntities
            .Include(e => e.Chunk)
            .Include(e => e.Document)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.UserId == userId && e.IsActive);

        if (entity == null)
        {
            _logger.LogWarning("Entity {EntityId} not found for user {UserId}", entityId, userId);
            return new List<LearningQuestionDto>();
        }

        var questions = new List<LearningQuestionDto>();
        var questionTypes = !string.IsNullOrEmpty(questionType)
            ? new List<string> { questionType }
            : new List<string>
            {
                QuestionTypes.MultipleChoice,
                QuestionTypes.FillInBlank,
                QuestionTypes.TrueFalse,
                QuestionTypes.ShortAnswer
            };

        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            var type = questionTypes[random.Next(questionTypes.Count)];
            var level = bloomLevel ?? random.Next(1, 5);

            try
            {
                var question = await GenerateQuestionForEntityAsync(entity, type, level, userId);
                if (question != null)
                {
                    questions.Add(question);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate question {Index} for entity {EntityId}", i, entityId);
            }

            await Task.Delay(300);
        }

        return questions;
    }

    /// <summary>
    /// Generate a single question for an entity using Claude.
    /// </summary>
    private async Task<LearningQuestionDto?> GenerateQuestionForEntityAsync(
        KgEntity entity,
        string questionType,
        int bloomLevel,
        int userId)
    {
        var apiKey = await GetAnthropicApiKeyAsync(userId);

        // Get context from chunk
        var context = entity.Chunk?.Content ?? entity.Description ?? "";

        // Get related entities for connection questions
        var relatedEntities = await _context.KgRelationships
            .Where(r => r.IsActive && (r.SourceEntityId == entity.Id || r.TargetEntityId == entity.Id))
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .Take(5)
            .Select(r => r.SourceEntityId == entity.Id ? r.TargetEntity.Name : r.SourceEntity.Name)
            .ToListAsync();

        var bloomDescription = BloomLevels.GetName(bloomLevel);
        var questionTypeDescription = questionType switch
        {
            QuestionTypes.MultipleChoice => "Multiple Choice mit 4 Optionen (A, B, C, D), eine richtig",
            QuestionTypes.FillInBlank => "Lückentext (Fill-in-the-Blank)",
            QuestionTypes.TrueFalse => "Wahr/Falsch mit Begründung",
            QuestionTypes.ShortAnswer => "Kurzantwort (1-2 Sätze)",
            QuestionTypes.Calculation => "Rechenaufgabe/Anwendung",
            QuestionTypes.Connection => "Verbindungsfrage (Wie hängt X mit Y zusammen?)",
            _ => "Multiple Choice"
        };

        var systemPrompt = $@"Du bist ein erfahrener Hochschuldozent, der Übungsfragen erstellt.

Erstelle eine {questionTypeDescription} Frage auf Bloom-Level {bloomLevel} ({bloomDescription}).

BLOOM-LEVEL ERKLÄRUNG:
1. Remember (Erinnern): Fakten abrufen, definieren, benennen
2. Understand (Verstehen): Erklären, interpretieren, zusammenfassen
3. Apply (Anwenden): Anwenden in neuen Situationen
4. Analyze (Analysieren): Unterscheiden, vergleichen, organisieren
5. Evaluate (Bewerten): Begründen, kritisieren, bewerten
6. Create (Erschaffen): Entwickeln, planen, produzieren

REGELN:
1. Die Frage muss auf dem gegebenen Konzept basieren
2. Der Schwierigkeitsgrad muss dem Bloom-Level entsprechen
3. Die Antwort muss eindeutig und korrekt sein
4. Die Erklärung muss lehrreich sein
5. Bei Multiple Choice: Eine eindeutig richtige Antwort, drei plausible Distraktoren

Antworte NUR mit validem JSON:
{{
  ""question"": ""Die Frage"",
  ""options"": [""A) ..."", ""B) ..."", ""C) ..."", ""D) ...""],  // nur bei MC
  ""correct_answer"": ""A"",  // bei MC nur Buchstabe, sonst volle Antwort
  ""explanation"": ""Erklärung warum die Antwort richtig ist"",
  ""hint"": ""Ein hilfreicher Hinweis"",
  ""difficulty"": 0.7  // 0.0-1.0
}}";

        var userMessage = $@"Konzept: {entity.Name}
Typ: {entity.EntityType}
{(entity.Description != null ? $"Beschreibung: {entity.Description}" : "")}
{(context.Length > 0 ? $"Kontext: {context.Substring(0, Math.Min(context.Length, 1000))}" : "")}
{(relatedEntities.Any() ? $"Verwandte Konzepte: {string.Join(", ", relatedEntities)}" : "")}
{(entity.Subject != null ? $"Fach: {entity.Subject}" : "")}

Erstelle eine {questionTypeDescription} Frage auf Bloom-Level {bloomLevel}.";

        try
        {
            var responseJson = await _anthropicClient.ChatJsonAsync(
                systemPrompt,
                userMessage,
                model: "claude-sonnet-4-5",
                maxTokens: 2048,
                apiKey: apiKey
            );

            return ParseQuestionResponse(responseJson, entity, questionType, bloomLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating question for entity {EntityId}", entity.Id);
            return null;
        }
    }

    /// <summary>
    /// Parse Claude's question response.
    /// </summary>
    private LearningQuestionDto? ParseQuestionResponse(
        JsonDocument json,
        KgEntity entity,
        string questionType,
        int bloomLevel)
    {
        try
        {
            var root = json.RootElement;

            var question = new LearningQuestionDto
            {
                Id = Guid.NewGuid().ToString(),
                QuestionType = questionType,
                Question = root.GetProperty("question").GetString() ?? "",
                BloomLevel = bloomLevel,
                EntityId = entity.Id,
                EntityName = entity.Name,
                SourceChunkId = entity.ChunkId,
                SourceDocumentId = entity.DocumentId,
                SourceDocumentName = entity.Document?.FileName
            };

            // Parse options (for MC)
            if (root.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
            {
                question.Options = options.EnumerateArray()
                    .Select(o => o.GetString() ?? "")
                    .ToList();
            }

            // Parse correct answer
            if (root.TryGetProperty("correct_answer", out var answer))
            {
                question.CorrectAnswer = answer.GetString() ?? "";
            }

            // Parse explanation
            if (root.TryGetProperty("explanation", out var explanation))
            {
                question.Explanation = explanation.GetString();
            }

            // Parse hint
            if (root.TryGetProperty("hint", out var hint))
            {
                question.Hint = hint.GetString();
            }

            // Parse difficulty
            if (root.TryGetProperty("difficulty", out var difficulty))
            {
                question.Difficulty = difficulty.GetDouble();
            }
            else
            {
                question.Difficulty = bloomLevel / 6.0;
            }

            return question;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing question response");
            return null;
        }
    }

    /// <summary>
    /// Records a user's answer to a question.
    /// </summary>
    public async Task<AnswerFeedbackDto> SubmitAnswerAsync(int userId, AnswerSubmission submission)
    {
        var feedback = new AnswerFeedbackDto();

        // Basic validation
        if (string.IsNullOrWhiteSpace(submission.UserAnswer))
        {
            feedback.IsCorrect = false;
            feedback.Feedback = "Bitte gib eine Antwort ein.";
            return feedback;
        }

        // If entity ID is provided, update performance
        if (submission.EntityId.HasValue)
        {
            var entity = await _context.KgEntities.FindAsync(submission.EntityId.Value);
            if (entity == null)
            {
                feedback.Feedback = "Entität nicht gefunden.";
                return feedback;
            }

            // Determine if answer is correct (simplified - in real app, use AI to evaluate)
            // For now, assume caller has already determined correctness
            var questionType = submission.QuestionType ?? QuestionTypes.MultipleChoice;
            var bloomLevel = submission.BloomLevel ?? 2;

            // Get or create performance record
            var performance = await _context.UserEntityPerformances
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId &&
                    p.EntityId == submission.EntityId.Value &&
                    p.QuestionType == questionType &&
                    p.BloomLevel == bloomLevel);

            if (performance == null)
            {
                performance = new UserEntityPerformance
                {
                    UserId = userId,
                    EntityId = submission.EntityId.Value,
                    QuestionType = questionType,
                    BloomLevel = bloomLevel,
                    State = FsrsStates.New
                };
                _context.UserEntityPerformances.Add(performance);
            }

            // Update performance using FSRS algorithm
            var oldMastery = performance.MasteryScore;
            UpdatePerformanceWithFsrs(performance, feedback.IsCorrect, submission.ResponseTimeSeconds);

            feedback.NewMasteryScore = performance.MasteryScore;
            feedback.MasteryChange = performance.MasteryScore - oldMastery;
            feedback.NextReview = performance.NextReview;

            await _context.SaveChangesAsync();
        }

        return feedback;
    }

    /// <summary>
    /// Update performance using simplified FSRS algorithm.
    /// </summary>
    private void UpdatePerformanceWithFsrs(UserEntityPerformance perf, bool isCorrect, double? responseTime)
    {
        perf.Attempts++;
        perf.LastAttempt = DateTime.UtcNow;

        if (responseTime.HasValue)
        {
            perf.AverageResponseTime = perf.AverageResponseTime.HasValue
                ? (perf.AverageResponseTime.Value * (perf.Attempts - 1) + responseTime.Value) / perf.Attempts
                : responseTime.Value;
        }

        if (isCorrect)
        {
            perf.Correct++;
            perf.CurrentStreak++;
            perf.BestStreak = Math.Max(perf.BestStreak, perf.CurrentStreak);
            perf.Reps++;

            // FSRS: Increase stability
            perf.Stability = perf.State == FsrsStates.New
                ? 1.0
                : perf.Stability * (1.0 + 0.5 * (1.0 - perf.Difficulty));

            // Decrease difficulty slightly
            perf.Difficulty = Math.Max(0.1, perf.Difficulty - 0.05);

            // Update state
            perf.State = perf.State switch
            {
                FsrsStates.New => FsrsStates.Learning,
                FsrsStates.Learning => perf.Reps >= 3 ? FsrsStates.Review : FsrsStates.Learning,
                FsrsStates.Relearning => FsrsStates.Review,
                _ => FsrsStates.Review
            };
        }
        else
        {
            perf.CurrentStreak = 0;
            perf.Lapses++;

            // FSRS: Reset stability on lapse
            perf.Stability = Math.Max(0.5, perf.Stability * 0.5);

            // Increase difficulty
            perf.Difficulty = Math.Min(1.0, perf.Difficulty + 0.1);

            // Move to relearning state
            perf.State = FsrsStates.Relearning;
        }

        // Calculate mastery score
        var successRate = perf.Attempts > 0 ? (double)perf.Correct / perf.Attempts : 0;
        perf.MasteryScore = successRate * 0.6 + (1.0 - perf.Difficulty) * 0.2 + Math.Min(perf.Stability / 30.0, 0.2);

        // Calculate next review date
        var intervalDays = perf.State switch
        {
            FsrsStates.New => 0,
            FsrsStates.Learning => 1,
            FsrsStates.Relearning => 1,
            _ => (int)Math.Ceiling(perf.Stability)
        };

        perf.ScheduledDays = intervalDays;
        perf.NextReview = DateTime.UtcNow.AddDays(intervalDays);
        perf.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets entities the user needs to practice (weak areas).
    /// </summary>
    public async Task<List<WeakAreaDto>> GetWeakAreasAsync(int userId, int limit = 10)
    {
        var weakAreas = new List<WeakAreaDto>();

        // Get entities with low mastery
        var lowMastery = await _context.UserEntityPerformances
            .Include(p => p.Entity)
            .Where(p => p.UserId == userId && p.Entity.IsActive && p.MasteryScore < 0.5)
            .OrderBy(p => p.MasteryScore)
            .Take(limit)
            .ToListAsync();

        foreach (var perf in lowMastery)
        {
            weakAreas.Add(new WeakAreaDto
            {
                EntityId = perf.EntityId,
                EntityName = perf.Entity.Name,
                EntityType = perf.Entity.EntityType,
                Subject = perf.Entity.Subject,
                Topic = perf.Entity.Topic,
                MasteryScore = perf.MasteryScore,
                Attempts = perf.Attempts,
                Correct = perf.Correct,
                Reason = perf.MasteryScore < 0.3 ? "low_mastery" : "high_error_rate",
                Priority = (int)((1.0 - perf.MasteryScore) * 100)
            });
        }

        // Add overdue items
        var overdue = await _context.UserEntityPerformances
            .Include(p => p.Entity)
            .Where(p => p.UserId == userId &&
                        p.Entity.IsActive &&
                        p.NextReview.HasValue &&
                        p.NextReview < DateTime.UtcNow)
            .OrderBy(p => p.NextReview)
            .Take(limit - weakAreas.Count)
            .ToListAsync();

        foreach (var perf in overdue)
        {
            if (weakAreas.Any(w => w.EntityId == perf.EntityId)) continue;

            weakAreas.Add(new WeakAreaDto
            {
                EntityId = perf.EntityId,
                EntityName = perf.Entity.Name,
                EntityType = perf.Entity.EntityType,
                Subject = perf.Entity.Subject,
                Topic = perf.Entity.Topic,
                MasteryScore = perf.MasteryScore,
                Attempts = perf.Attempts,
                Correct = perf.Correct,
                Reason = "overdue",
                Priority = (int)((DateTime.UtcNow - perf.NextReview!.Value).TotalDays * 10)
            });
        }

        return weakAreas.OrderByDescending(w => w.Priority).Take(limit).ToList();
    }

    /// <summary>
    /// Gets entities due for review (spaced repetition).
    /// </summary>
    public async Task<List<KgEntityDto>> GetDueForReviewAsync(int userId, int limit = 10)
    {
        var duePerformances = await _context.UserEntityPerformances
            .Include(p => p.Entity)
            .Where(p => p.UserId == userId &&
                        p.Entity.IsActive &&
                        p.NextReview.HasValue &&
                        p.NextReview <= DateTime.UtcNow)
            .OrderBy(p => p.NextReview)
            .Take(limit)
            .ToListAsync();

        return duePerformances.Select(p => MapToDto(p.Entity, p)).ToList();
    }

    /// <summary>
    /// Gets user's mastery statistics.
    /// </summary>
    public async Task<MasteryStatsDto> GetMasteryStatsAsync(int userId, string? subject = null)
    {
        var stats = new MasteryStatsDto();

        var performanceQuery = _context.UserEntityPerformances
            .Include(p => p.Entity)
            .Where(p => p.UserId == userId && p.Entity.IsActive);

        if (!string.IsNullOrEmpty(subject))
        {
            performanceQuery = performanceQuery.Where(p => p.Entity.Subject == subject);
        }

        var performances = await performanceQuery.ToListAsync();

        if (!performances.Any())
        {
            // Count total entities without performance
            stats.TotalEntities = await _context.KgEntities
                .CountAsync(e => e.UserId == userId && e.IsActive);
            stats.NewEntities = stats.TotalEntities;
            return stats;
        }

        stats.TotalEntities = performances.Count;
        stats.MasteredEntities = performances.Count(p => p.MasteryScore >= 0.8);
        stats.LearningEntities = performances.Count(p => p.MasteryScore >= 0.3 && p.MasteryScore < 0.8);
        stats.NewEntities = performances.Count(p => p.State == FsrsStates.New);

        stats.AverageMastery = performances.Average(p => p.MasteryScore);
        stats.TotalAttempts = performances.Sum(p => p.Attempts);
        stats.TotalCorrect = performances.Sum(p => p.Correct);
        stats.OverallSuccessRate = stats.TotalAttempts > 0
            ? (double)stats.TotalCorrect / stats.TotalAttempts
            : 0;

        // By subject
        stats.BySubject = performances
            .Where(p => !string.IsNullOrEmpty(p.Entity.Subject))
            .GroupBy(p => p.Entity.Subject!)
            .ToDictionary(
                g => g.Key,
                g => new SubjectMasteryDto
                {
                    Subject = g.Key,
                    TotalEntities = g.Count(),
                    MasteredEntities = g.Count(p => p.MasteryScore >= 0.8),
                    AverageMastery = g.Average(p => p.MasteryScore),
                    Attempts = g.Sum(p => p.Attempts),
                    Correct = g.Sum(p => p.Correct)
                });

        // By Bloom level
        stats.ByBloomLevel = performances
            .GroupBy(p => p.BloomLevel)
            .ToDictionary(g => g.Key, g => g.Count());

        // Streak info
        stats.CurrentStreak = performances.Max(p => p.CurrentStreak);
        stats.BestStreak = performances.Max(p => p.BestStreak);

        return stats;
    }
}
