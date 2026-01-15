using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;
using InteractiveExerciseModel = DHBWAutomation.Backend.Core.Models.InteractiveExercise;

namespace DHBWAutomation.Backend.Core.Services.OmniLearning;

public partial class OmniLearningEngineService
{
    #region Exercise Generation

    /// <summary>
    /// Generiert eine einzelne Übung
    /// </summary>
    public async Task<OmniExerciseDto> GenerateExerciseAsync(int userId, GenerateExerciseRequest request)
    {
        // Bestimme Ziel-Entität
        UnifiedKnowledgeEntity? entity = null;

        if (request.EntityId.HasValue)
        {
            entity = await _context.UnifiedKnowledgeEntities
                .FirstOrDefaultAsync(e => e.Id == request.EntityId.Value && e.UserId == userId && e.IsActive);
        }
        else if (!string.IsNullOrEmpty(request.Subject) || !string.IsNullOrEmpty(request.Topic))
        {
            var query = _context.UnifiedKnowledgeEntities
                .Where(e => e.UserId == userId && e.IsActive);

            if (!string.IsNullOrEmpty(request.Subject))
                query = query.Where(e => e.Subject == request.Subject);
            if (!string.IsNullOrEmpty(request.Topic))
                query = query.Where(e => e.Topic == request.Topic);

            // Wähle Entität basierend auf Priorität (niedrige Mastery bevorzugt)
            entity = await query
                .OrderBy(e => e.MasteryScore)
                .FirstOrDefaultAsync();
        }

        // Bestimme Schwierigkeit
        string difficulty = request.Difficulty ?? "medium";
        if (request.UseAdaptive && entity != null)
        {
            difficulty = entity.GetRecommendedDifficulty();
        }

        // Bestimme Bloom-Level
        int bloomLevel = request.BloomLevel ?? 1;
        if (request.UseAdaptive && entity != null)
        {
            bloomLevel = DetermineRecommendedBloomLevel(entity.MasteryScore, entity.CurrentBloomLevel);
        }

        // Bestimme Übungstyp
        string exerciseType = request.ExerciseType ?? SelectExerciseType(bloomLevel);

        // Generiere Übung mit KI
        var exercise = await GenerateExerciseWithAIAsync(
            userId, entity, exerciseType, difficulty, bloomLevel, request.DocumentId);

        return exercise;
    }

    /// <summary>
    /// Generiert eine Lern-Session mit mehreren Übungen
    /// </summary>
    public async Task<List<OmniExerciseDto>> GenerateSessionAsync(int userId, GenerateSessionRequest request)
    {
        var exercises = new List<OmniExerciseDto>();

        // Hole überfällige Übungen zuerst
        if (request.IncludeOverdue)
        {
            var overdueExercises = await GetDueExercisesAsync(userId, request.Count / 2);
            exercises.AddRange(overdueExercises);
        }

        // Generiere neue Übungen für verbleibende Slots
        var remainingCount = request.Count - exercises.Count;
        if (remainingCount > 0)
        {
            // Hole Entitäten nach Priorität
            var query = _context.UnifiedKnowledgeEntities
                .Where(e => e.UserId == userId && e.IsActive);

            if (!string.IsNullOrEmpty(request.Subject))
                query = query.Where(e => e.Subject == request.Subject);
            if (!string.IsNullOrEmpty(request.Topic))
                query = query.Where(e => e.Topic == request.Topic);

            var entities = await query
                .OrderBy(e => e.MasteryScore)
                .Take(remainingCount)
                .ToListAsync();

            foreach (var entity in entities)
            {
                var exerciseType = request.ExerciseTypes?.Any() == true
                    ? request.ExerciseTypes[new Random().Next(request.ExerciseTypes.Count)]
                    : SelectExerciseType(entity.CurrentBloomLevel);

                var exercise = await GenerateExerciseWithAIAsync(
                    userId, entity, exerciseType,
                    entity.GetRecommendedDifficulty(),
                    entity.CurrentBloomLevel, null);

                exercises.Add(exercise);

                if (exercises.Count >= request.Count) break;
            }
        }

        return exercises.Take(request.Count).ToList();
    }

    /// <summary>
    /// Reicht eine Antwort ein und erhält Feedback
    /// </summary>
    public async Task<ExerciseSubmissionResult> SubmitAnswerAsync(
        int exerciseId, int userId, AnswerSubmissionDto submission)
    {
        // Hole Übung (aus InteractiveExercise oder GeneratedExercise)
        var interactiveExercise = await _context.InteractiveExercises
            .FirstOrDefaultAsync(e => e.Id == exerciseId && e.UserId == userId);

        bool isCorrect;
        double score;
        string feedback;
        string? explanation = null;
        string? correctAnswer = null;

        if (interactiveExercise != null)
        {
            // Interaktive Übung validieren
            var content = JsonSerializer.Deserialize<InteractiveExerciseContent>(interactiveExercise.ExerciseContent);
            var validationResult = await ValidateInteractiveAnswerAsync(content, submission);

            isCorrect = validationResult.IsCorrect;
            score = validationResult.Score;
            feedback = validationResult.Feedback;
            explanation = validationResult.Explanation;
            correctAnswer = validationResult.CorrectAnswer;

            // Update Exercise
            interactiveExercise.Score = score;
            interactiveExercise.CompletedAt = DateTime.UtcNow;
            interactiveExercise.ReviewCount++;

            // Update Spaced Repetition
            var newInterval = CalculateNewInterval(interactiveExercise.EaseFactor, interactiveExercise.ReviewCount, isCorrect);
            interactiveExercise.NextReviewDate = DateTime.UtcNow.AddDays(newInterval);
            if (isCorrect)
                interactiveExercise.EaseFactor = Math.Min(2.5, interactiveExercise.EaseFactor + 0.1);
            else
                interactiveExercise.EaseFactor = Math.Max(1.3, interactiveExercise.EaseFactor - 0.2);
        }
        else
        {
            // Generierte Übung oder direkte Antwort
            var generatedExercise = await _context.GeneratedExercises
                .FirstOrDefaultAsync(e => e.Id == exerciseId && e.UserId == userId);

            if (generatedExercise != null)
            {
                isCorrect = CheckAnswer(generatedExercise.CorrectAnswer, submission.Answer);
                score = isCorrect ? 100 : 0;
                feedback = isCorrect ? "Richtig!" : "Leider nicht korrekt.";
                explanation = generatedExercise.Explanation;
                correctAnswer = generatedExercise.CorrectAnswer;

                generatedExercise.UserAnswer = submission.Answer;
                generatedExercise.IsCorrect = isCorrect;
                generatedExercise.AnsweredAt = DateTime.UtcNow;
                generatedExercise.ReviewCount++;

                var newInterval = CalculateNewInterval(generatedExercise.EaseFactor, generatedExercise.ReviewCount, isCorrect);
                generatedExercise.NextReviewDate = DateTime.UtcNow.AddDays(newInterval);
            }
            else
            {
                return new ExerciseSubmissionResult
                {
                    IsCorrect = false,
                    Score = 0,
                    Feedback = "Übung nicht gefunden"
                };
            }
        }

        // Update Entity Performance wenn vorhanden
        UnifiedKnowledgeEntity? entity = null;
        int? newBloomLevel = null;

        if (interactiveExercise?.KnowledgeBaseItemId != null)
        {
            // Finde zugehörige Entität
            entity = await _context.UnifiedKnowledgeEntities
                .FirstOrDefaultAsync(e => e.UserId == userId &&
                    e.Subject == interactiveExercise.Subject &&
                    e.Topic == interactiveExercise.Topic &&
                    e.IsActive);
        }

        if (entity != null)
        {
            var difficulty = interactiveExercise?.Difficulty ?? "medium";
            var bloomLevel = entity.CurrentBloomLevel;
            var responseTime = submission.ResponseTimeSeconds ?? 30;

            entity.RecordAttempt(isCorrect, difficulty, responseTime, bloomLevel);

            // FSRS Update
            var quality = isCorrect ? (score >= 80 ? 4 : 3) : (score >= 30 ? 2 : 1);
            var fsrsResult = UpdateFsrs(
                entity.FsrsState,
                entity.Stability,
                entity.Difficulty,
                entity.Reps,
                entity.Lapses,
                isCorrect,
                quality);

            entity.FsrsState = fsrsResult.NewState;
            entity.Stability = fsrsResult.NewStability;
            entity.Difficulty = fsrsResult.NewDifficulty;
            entity.Reps = fsrsResult.NewReps;
            entity.Lapses = fsrsResult.NewLapses;
            entity.NextReview = fsrsResult.NextReview;

            // Check Bloom Level Advancement
            if (entity.CanAdvanceBloomLevel())
            {
                entity.CurrentBloomLevel++;
                newBloomLevel = entity.CurrentBloomLevel;
            }
        }

        await _context.SaveChangesAsync();

        return new ExerciseSubmissionResult
        {
            IsCorrect = isCorrect,
            Score = score,
            Feedback = feedback,
            Explanation = explanation,
            CorrectAnswer = correctAnswer,
            NewMasteryScore = entity?.MasteryScore ?? 0,
            NextReviewDate = entity?.NextReview,
            NewBloomLevel = newBloomLevel,
            Achievement = newBloomLevel.HasValue ? $"Neues Bloom-Level: {GetBloomLevelName(newBloomLevel.Value)}" : null
        };
    }

    /// <summary>
    /// Holt Übungen die zur Wiederholung fällig sind
    /// </summary>
    public async Task<List<OmniExerciseDto>> GetDueExercisesAsync(int userId, int limit = 10)
    {
        var now = DateTime.UtcNow;
        var result = new List<OmniExerciseDto>();

        // Hole fällige Interactive Exercises
        var dueInteractive = await _context.InteractiveExercises
            .Where(e => e.UserId == userId && e.NextReviewDate <= now)
            .OrderBy(e => e.NextReviewDate)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        foreach (var ex in dueInteractive)
        {
            result.Add(MapInteractiveExerciseToDto(ex));
        }

        // Hole fällige Generated Exercises
        var dueGenerated = await _context.GeneratedExercises
            .Where(e => e.UserId == userId && e.NextReviewDate <= now && e.IsCorrect != true)
            .OrderBy(e => e.NextReviewDate)
            .Take(limit - result.Count)
            .AsNoTracking()
            .ToListAsync();

        foreach (var ex in dueGenerated)
        {
            result.Add(MapGeneratedExerciseToDto(ex));
        }

        return result.Take(limit).ToList();
    }

    #region Helper Methods

    private string SelectExerciseType(int bloomLevel)
    {
        return bloomLevel switch
        {
            1 => "multiple_choice",  // Erinnern
            2 => "fill_blank",       // Verstehen
            3 => "drag_drop",        // Anwenden
            4 => "text_input",       // Analysieren
            5 => "code_editor",      // Bewerten
            6 => "text_input",       // Erschaffen
            _ => "multiple_choice"
        };
    }

    private async Task<OmniExerciseDto> GenerateExerciseWithAIAsync(
        int userId,
        UnifiedKnowledgeEntity? entity,
        string exerciseType,
        string difficulty,
        int bloomLevel,
        int? documentId)
    {
        var apiKey = await GetAnthropicApiKeyAsync(userId);

        // Hole Kontext
        string context = "";
        if (entity != null)
        {
            context = $"Thema: {entity.Subject} - {entity.Topic}\n";
            context += $"Konzept: {entity.Name}\n";
            if (!string.IsNullOrEmpty(entity.Description))
                context += $"Beschreibung: {entity.Description}\n";

            // Hole verwandte Konzepte für Distraktoren
            var related = await GetRelatedEntitiesAsync(entity.Id, userId, 1);
            if (related.Any())
            {
                context += $"Verwandte Konzepte: {string.Join(", ", related.Take(3).Select(r => r.Name))}\n";
            }
        }

        if (documentId.HasValue)
        {
            var chunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId.Value)
                .OrderBy(c => c.ChunkIndex)
                .Take(2)
                .Select(c => c.Content)
                .ToListAsync();

            if (chunks.Any())
            {
                context += $"\nQuelltext:\n{string.Join("\n", chunks)}";
            }
        }

        var bloomLevelName = GetBloomLevelName(bloomLevel);
        var systemPrompt = GetExerciseGenerationPrompt(exerciseType, difficulty, bloomLevelName);
        var userPrompt = $"Erstelle eine Übung basierend auf:\n{context}";

        try
        {
            var response = await _anthropicClient.ChatAsync(
                systemPrompt, userPrompt, maxTokens: 2000, apiKey: apiKey);

            if (!string.IsNullOrEmpty(response))
            {
                return ParseExerciseResponse(response, exerciseType, difficulty, bloomLevel, entity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler bei Übungsgenerierung");
        }

        // Fallback: Einfache MC-Übung
        return new OmniExerciseDto
        {
            ExerciseType = exerciseType,
            Difficulty = difficulty,
            Subject = entity?.Subject ?? "Allgemein",
            Topic = entity?.Topic ?? "Allgemein",
            Question = entity != null ? $"Was ist {entity.Name}?" : "Allgemeine Frage",
            BloomLevel = bloomLevel,
            BloomLevelName = bloomLevelName,
            EntityId = entity?.Id,
            EntityName = entity?.Name,
            CreatedAt = DateTime.UtcNow
        };
    }

    private string GetExerciseGenerationPrompt(string exerciseType, string difficulty, string bloomLevel)
    {
        var basePrompt = $@"Du bist ein Experte für die Erstellung von Lernübungen auf Deutsch.
Erstelle eine {difficulty} Übung für Bloom-Level '{bloomLevel}'.

Übungstyp: {exerciseType}
";

        var typeSpecificPrompt = exerciseType switch
        {
            "multiple_choice" => @"
Erstelle eine Multiple-Choice-Frage mit 4 Optionen (A-D), wobei genau eine korrekt ist.
Antworte als JSON:
{
  ""question"": ""Die Frage"",
  ""options"": [""A) Option 1"", ""B) Option 2"", ""C) Option 3"", ""D) Option 4""],
  ""correct"": ""A"",
  ""explanation"": ""Erklärung warum A korrekt ist"",
  ""hint"": ""Ein hilfreicher Hinweis""
}",
            "fill_blank" => @"
Erstelle einen Lückentext mit 1-3 Lücken. Markiere Lücken mit {{blank}}.
Antworte als JSON:
{
  ""question"": ""Der Text mit {{blank}} Lücken"",
  ""blanks"": [{""id"": ""1"", ""answer"": ""richtige Antwort"", ""alternatives"": [""alternative1""]}],
  ""explanation"": ""Erklärung"",
  ""hint"": ""Hinweis""
}",
            "drag_drop" => @"
Erstelle eine Zuordnungsaufgabe mit 3-5 Elementen.
Antworte als JSON:
{
  ""question"": ""Ordne die Elemente den richtigen Kategorien zu"",
  ""items"": [{""id"": ""1"", ""content"": ""Element 1""}],
  ""zones"": [{""id"": ""zone1"", ""label"": ""Kategorie 1"", ""acceptedItems"": [""1""]}],
  ""explanation"": ""Erklärung""
}",
            _ => @"
Erstelle eine Freitext-Frage.
Antworte als JSON:
{
  ""question"": ""Die Frage"",
  ""answer"": ""Die erwartete Antwort"",
  ""keywords"": [""wichtig1"", ""wichtig2""],
  ""explanation"": ""Erklärung"",
  ""hint"": ""Hinweis""
}"
        };

        return basePrompt + typeSpecificPrompt;
    }

    private OmniExerciseDto ParseExerciseResponse(
        string response, string exerciseType, string difficulty, int bloomLevel, UnifiedKnowledgeEntity? entity)
    {
        var dto = new OmniExerciseDto
        {
            ExerciseType = exerciseType,
            Difficulty = difficulty,
            Subject = entity?.Subject ?? "Allgemein",
            Topic = entity?.Topic ?? "Allgemein",
            BloomLevel = bloomLevel,
            BloomLevelName = GetBloomLevelName(bloomLevel),
            EntityId = entity?.Id,
            EntityName = entity?.Name,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<JsonElement>(jsonStr);

                if (parsed.TryGetProperty("question", out var questionProp))
                    dto.Question = questionProp.GetString() ?? "";

                if (parsed.TryGetProperty("hint", out var hintProp))
                    dto.Hint = hintProp.GetString();

                // Speichere den gesamten Inhalt für typ-spezifische Verarbeitung
                dto.Content = JsonSerializer.Deserialize<object>(jsonStr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler beim Parsen der Übungsantwort");
            dto.Question = "Fehler beim Generieren der Übung";
        }

        return dto;
    }

    private async Task<ValidationResult> ValidateInteractiveAnswerAsync(
        InteractiveExerciseContent? content, AnswerSubmissionDto submission)
    {
        // Vereinfachte Validierung
        return new ValidationResult
        {
            IsCorrect = true,
            Score = 100,
            Feedback = "Antwort erhalten",
            Explanation = null,
            CorrectAnswer = null
        };
    }

    private bool CheckAnswer(string? correctAnswer, string userAnswer)
    {
        if (string.IsNullOrEmpty(correctAnswer)) return false;
        return correctAnswer.Trim().Equals(userAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private int CalculateNewInterval(double easeFactor, int reviewCount, bool isCorrect)
    {
        if (!isCorrect) return 1;
        if (reviewCount == 1) return 1;
        if (reviewCount == 2) return 6;
        return (int)Math.Round(6 * Math.Pow(easeFactor, reviewCount - 2));
    }

    private OmniExerciseDto MapInteractiveExerciseToDto(InteractiveExerciseModel ex)
    {
        return new OmniExerciseDto
        {
            Id = ex.Id,
            ExerciseType = "interactive",
            Difficulty = ex.Difficulty,
            Subject = ex.Subject,
            Topic = ex.Topic,
            Question = $"Interaktive Übung: {ex.Topic}",
            BloomLevel = 3,
            BloomLevelName = GetBloomLevelName(3),
            NextReviewDate = ex.NextReviewDate,
            AttemptCount = ex.ReviewCount,
            LastScore = ex.Score,
            CreatedAt = ex.CreatedAt
        };
    }

    private OmniExerciseDto MapGeneratedExerciseToDto(GeneratedExercise ex)
    {
        return new OmniExerciseDto
        {
            Id = ex.Id,
            ExerciseType = ex.ExerciseType,
            Difficulty = ex.Difficulty,
            Subject = ex.Subject,
            Topic = ex.Topic,
            Question = ex.Question,
            BloomLevel = 2,
            BloomLevelName = GetBloomLevelName(2),
            NextReviewDate = ex.NextReviewDate,
            AttemptCount = ex.ReviewCount,
            CreatedAt = ex.CreatedAt
        };
    }

    private class ValidationResult
    {
        public bool IsCorrect { get; set; }
        public double Score { get; set; }
        public string Feedback { get; set; } = "";
        public string? Explanation { get; set; }
        public string? CorrectAnswer { get; set; }
    }

    #endregion

    #endregion
}
