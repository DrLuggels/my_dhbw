using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Unified Learning Service - combines functionality from:
/// - LearningEngineService (entity extraction, Bloom taxonomy)
/// - PersonalKnowledgeGraphService (knowledge graph, decay)
/// - RagExerciseService (RAG-based exercise generation)
/// - DeadlinePriorityService (priority calculation)
/// - PrerequisiteService (prerequisite checking)
/// - AdaptiveDifficultyService (20/40/40 rule)
///
/// Uses combined FSRS + Decay algorithm for spaced repetition.
/// </summary>
public interface IUnifiedLearningService
{
    #region Entity Management

    /// <summary>
    /// Gets or creates a unified knowledge entity for the given topic.
    /// </summary>
    Task<UnifiedKnowledgeEntity> GetOrCreateEntityAsync(
        int userId,
        string subject,
        string topic,
        string? entityType = null,
        string? name = null);

    /// <summary>
    /// Gets an entity by ID.
    /// </summary>
    Task<UnifiedKnowledgeEntity?> GetEntityAsync(int entityId);

    /// <summary>
    /// Gets all entities for a user with optional filtering.
    /// </summary>
    Task<List<UnifiedKnowledgeEntity>> GetUserEntitiesAsync(
        int userId,
        UnifiedEntityFilter? filter = null);

    /// <summary>
    /// Updates an entity after an interaction (exercise, review, etc.).
    /// </summary>
    Task<UnifiedKnowledgeEntity> UpdateEntityAfterInteractionAsync(
        int entityId,
        bool isCorrect,
        string difficulty,
        int bloomLevel,
        double responseTimeSeconds);

    /// <summary>
    /// Merges duplicate entities into one.
    /// </summary>
    Task<UnifiedKnowledgeEntity> MergeEntitiesAsync(
        int primaryEntityId,
        IEnumerable<int> duplicateEntityIds,
        int userId);

    #endregion

    #region Entity Extraction (from LearningEngineService)

    /// <summary>
    /// Extracts entities and relationships from a document using Claude.
    /// </summary>
    Task<UnifiedExtractionResult> ExtractEntitiesFromDocumentAsync(
        int documentId,
        int userId,
        UnifiedExtractionOptions? options = null);

    /// <summary>
    /// Extracts entities from a specific chunk.
    /// </summary>
    Task<UnifiedExtractionResult> ExtractEntitiesFromChunkAsync(
        int chunkId,
        int userId);

    /// <summary>
    /// Processes multiple documents in batch.
    /// </summary>
    Task<List<UnifiedExtractionResult>> ProcessDocumentsBatchAsync(
        IEnumerable<int> documentIds,
        int userId,
        UnifiedExtractionOptions? options = null);

    #endregion

    #region Knowledge Graph Operations

    /// <summary>
    /// Gets the unified knowledge graph for a user.
    /// </summary>
    Task<UnifiedKnowledgeGraphDto> GetKnowledgeGraphAsync(
        int userId,
        UnifiedGraphOptions? options = null);

    /// <summary>
    /// Gets entities related to a specific entity.
    /// </summary>
    Task<List<UnifiedEntityDto>> GetRelatedEntitiesAsync(
        int entityId,
        int userId,
        int depth = 1);

    /// <summary>
    /// Searches entities by name or description.
    /// </summary>
    Task<List<UnifiedEntityDto>> SearchEntitiesAsync(
        int userId,
        string query,
        string? entityType = null,
        int limit = 20);

    /// <summary>
    /// Creates or updates a relationship between entities.
    /// </summary>
    Task<UnifiedKnowledgeRelationship> CreateOrUpdateRelationshipAsync(
        int userId,
        int sourceEntityId,
        int targetEntityId,
        string relationshipType,
        double? strength = null);

    /// <summary>
    /// Gets statistics about the knowledge graph.
    /// </summary>
    Task<UnifiedGraphStatsDto> GetGraphStatsAsync(int userId);

    #endregion

    #region Exercise Generation (RAG + Bloom + 20/40/40)

    /// <summary>
    /// Generates an exercise using RAG context, adaptive Bloom level, and 20/40/40 difficulty.
    /// </summary>
    Task<UnifiedExerciseDto> GenerateExerciseAsync(
        int userId,
        UnifiedExerciseRequest request);

    /// <summary>
    /// Generates multiple exercises for a learning session.
    /// </summary>
    Task<List<UnifiedExerciseDto>> GenerateExercisesAsync(
        int userId,
        UnifiedExerciseRequest request,
        int count = 5);

    /// <summary>
    /// Generates questions specifically for an entity.
    /// </summary>
    Task<List<UnifiedExerciseDto>> GenerateEntityExercisesAsync(
        int entityId,
        int userId,
        int count = 5,
        string? questionType = null);

    #endregion

    #region Answer Submission (FSRS + Decay)

    /// <summary>
    /// Submits an answer and updates the entity using combined FSRS + Decay algorithm.
    /// </summary>
    Task<UnifiedAnswerFeedbackDto> SubmitAnswerAsync(
        int userId,
        UnifiedAnswerSubmission submission);

    /// <summary>
    /// Records an exercise result without a formal answer (e.g., self-assessment).
    /// </summary>
    Task<UnifiedEntityImpact> RecordExerciseResultAsync(
        int userId,
        int entityId,
        bool isCorrect,
        string difficulty,
        int bloomLevel,
        double? responseTimeSeconds = null);

    #endregion

    #region Spaced Repetition (Due for Review)

    /// <summary>
    /// Gets entities due for review based on FSRS + Decay scheduling.
    /// </summary>
    Task<List<UnifiedEntityDto>> GetDueForReviewAsync(
        int userId,
        int limit = 10);

    /// <summary>
    /// Gets entities that need urgent review (fading quickly).
    /// </summary>
    Task<List<UnifiedEntityDto>> GetFadingEntitiesAsync(
        int userId,
        double decayThreshold = 0.5,
        int limit = 10);

    /// <summary>
    /// Calculates the next review date for an entity.
    /// </summary>
    DateTime CalculateNextReview(UnifiedKnowledgeEntity entity);

    #endregion

    #region Priority Recommendations (Deadline-aware)

    /// <summary>
    /// Gets prioritized learning recommendations considering deadlines, decay, and Bloom gaps.
    /// </summary>
    Task<List<UnifiedLearningPriority>> GetPrioritizedRecommendationsAsync(
        int userId,
        int limit = 10);

    /// <summary>
    /// Recalculates all priorities for a user.
    /// </summary>
    Task RecalculatePrioritiesAsync(int userId);

    /// <summary>
    /// Gets the next best thing to learn based on all factors.
    /// </summary>
    Task<UnifiedLearningPriority?> GetNextRecommendationAsync(int userId);

    #endregion

    #region Prerequisite Checking

    /// <summary>
    /// Checks if prerequisites for an entity are met.
    /// </summary>
    Task<UnifiedPrerequisiteCheckResult> CheckPrerequisitesAsync(
        int entityId,
        int userId);

    /// <summary>
    /// Gets the learning path to unlock an entity (missing prerequisites).
    /// </summary>
    Task<List<UnifiedEntityDto>> GetLearningPathAsync(
        int entityId,
        int userId);

    /// <summary>
    /// Creates a prerequisite relationship between entities.
    /// </summary>
    Task<UnifiedKnowledgeRelationship> CreatePrerequisiteAsync(
        int userId,
        int prerequisiteEntityId,
        int dependentEntityId,
        double requiredMasteryLevel = 0.6,
        bool isStrict = true);

    #endregion

    #region Statistics & Analytics

    /// <summary>
    /// Gets comprehensive mastery statistics for a user.
    /// </summary>
    Task<UnifiedMasteryStatsDto> GetMasteryStatsAsync(
        int userId,
        string? subject = null);

    /// <summary>
    /// Gets weak areas the user should practice.
    /// </summary>
    Task<List<UnifiedWeakAreaDto>> GetWeakAreasAsync(
        int userId,
        int limit = 10);

    /// <summary>
    /// Gets a learning summary for dashboard display.
    /// </summary>
    Task<UnifiedLearningSummaryDto> GetLearningSummaryAsync(int userId);

    #endregion
}

#region DTOs

/// <summary>
/// Filter options for entity queries.
/// </summary>
public class UnifiedEntityFilter
{
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? EntityType { get; set; }
    public double? MinMastery { get; set; }
    public double? MaxMastery { get; set; }
    public bool? IsActive { get; set; } = true;
    public int? Limit { get; set; }
    public string? SortBy { get; set; } // "mastery", "priority", "lastInteraction", "name"
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Options for entity extraction.
/// </summary>
public class UnifiedExtractionOptions
{
    public bool ExtractEntities { get; set; } = true;
    public bool ExtractRelationships { get; set; } = true;
    public bool GenerateEmbeddings { get; set; } = true;
    public bool DetectPrerequisites { get; set; } = true;
    public double EntityConfidenceThreshold { get; set; } = 0.7;
    public double RelationshipStrengthThreshold { get; set; } = 0.5;
    public int MaxEntitiesPerChunk { get; set; } = 20;
}

/// <summary>
/// Result of entity extraction.
/// </summary>
public class UnifiedExtractionResult
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public int EntitiesCreated { get; set; }
    public int EntitiesUpdated { get; set; }
    public int RelationshipsCreated { get; set; }
    public int PrerequisitesDetected { get; set; }

    public List<UnifiedEntityDto> Entities { get; set; } = new();
    public List<UnifiedRelationshipDto> Relationships { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Options for knowledge graph retrieval.
/// </summary>
public class UnifiedGraphOptions
{
    public List<int>? DocumentIds { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public List<string>? EntityTypes { get; set; }
    public int? MaxEntities { get; set; }
    public int? MaxDepth { get; set; }
    public double? MinMastery { get; set; }
    public double? MinImportance { get; set; }
    public bool IncludeRelationships { get; set; } = true;
    public bool IncludePrerequisites { get; set; } = true;
}

/// <summary>
/// Knowledge graph DTO.
/// </summary>
public class UnifiedKnowledgeGraphDto
{
    public List<UnifiedEntityDto> Entities { get; set; } = new();
    public List<UnifiedRelationshipDto> Relationships { get; set; } = new();
    public UnifiedGraphStatsDto Stats { get; set; } = new();
}

/// <summary>
/// Entity DTO for API responses.
/// </summary>
public class UnifiedEntityDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Subtopic { get; set; }

    // Extraction info
    public double ConfidenceScore { get; set; }
    public double ImportanceScore { get; set; }
    public int OccurrenceCount { get; set; }
    public bool IsVerified { get; set; }

    // Mastery & Performance
    public double MasteryScore { get; set; }
    public double EffectiveKnowledge { get; set; }
    public double DecayFactor { get; set; }
    public int TotalAttempts { get; set; }
    public int TotalCorrect { get; set; }
    public double SuccessRate { get; set; }

    // Bloom & Difficulty
    public int CurrentBloomLevel { get; set; }
    public double EasySuccessRate { get; set; }
    public double MediumSuccessRate { get; set; }
    public double HardSuccessRate { get; set; }

    // Spaced Repetition
    public DateTime? NextReview { get; set; }
    public int FsrsState { get; set; }
    public string FsrsStateName { get; set; } = string.Empty;

    // Source info
    public int? SourceDocumentId { get; set; }
    public string? SourceDocumentName { get; set; }

    public DateTime LastInteraction { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Relationship DTO for API responses.
/// </summary>
public class UnifiedRelationshipDto
{
    public int Id { get; set; }
    public int SourceEntityId { get; set; }
    public string SourceEntityName { get; set; } = string.Empty;
    public int TargetEntityId { get; set; }
    public string TargetEntityName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public double CurrentStrength { get; set; }
    public string? Evidence { get; set; }
    public string? Description { get; set; }
    public bool IsVerified { get; set; }
    public bool IsBidirectional { get; set; }

    // For prerequisites
    public bool IsPrerequisite { get; set; }
    public double? RequiredMasteryLevel { get; set; }
    public bool IsStrict { get; set; }
}

/// <summary>
/// Knowledge graph statistics.
/// </summary>
public class UnifiedGraphStatsDto
{
    public int TotalEntities { get; set; }
    public int TotalRelationships { get; set; }
    public int TotalPrerequisites { get; set; }
    public Dictionary<string, int> EntitiesByType { get; set; } = new();
    public Dictionary<string, int> RelationshipsByType { get; set; } = new();
    public int DocumentsCovered { get; set; }
    public int ChunksCovered { get; set; }
    public double AverageMastery { get; set; }
    public int MasteredEntities { get; set; }
    public int LearningEntities { get; set; }
    public int NewEntities { get; set; }
}

/// <summary>
/// Request for exercise generation.
/// </summary>
public class UnifiedExerciseRequest
{
    public int? EntityId { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public List<int>? DocumentIds { get; set; }

    /// <summary>
    /// Difficulty: "easy", "medium", "hard", or "adaptive" (uses 20/40/40 rule)
    /// </summary>
    public string Difficulty { get; set; } = "adaptive";

    /// <summary>
    /// Bloom level: 1-6 or null for adaptive selection
    /// </summary>
    public int? BloomLevel { get; set; }

    /// <summary>
    /// Question types: "mc", "fill_blank", "true_false", "short_answer", "calculation"
    /// </summary>
    public List<string>? QuestionTypes { get; set; }

    /// <summary>
    /// Whether to use RAG context from documents
    /// </summary>
    public bool UseRagContext { get; set; } = true;

    /// <summary>
    /// Number of RAG chunks to include for context
    /// </summary>
    public int RagChunkCount { get; set; } = 5;
}

/// <summary>
/// A generated exercise with RAG context.
/// </summary>
public class UnifiedExerciseDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string QuestionType { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? Hint { get; set; }

    // Difficulty & Bloom
    public string Difficulty { get; set; } = string.Empty;
    public int BloomLevel { get; set; }
    public string BloomLevelName { get; set; } = string.Empty;

    // Source information
    public int? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }

    // RAG context (for display/reference)
    public List<string>? SourceDocuments { get; set; }
    public List<string>? RelatedConcepts { get; set; }
}

/// <summary>
/// Answer submission.
/// </summary>
public class UnifiedAnswerSubmission
{
    public string ExerciseId { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string UserAnswer { get; set; } = string.Empty;
    public double ResponseTimeSeconds { get; set; }
    public string? Difficulty { get; set; }
    public int? BloomLevel { get; set; }
    public string? QuestionType { get; set; }
}

/// <summary>
/// Feedback for a submitted answer.
/// </summary>
public class UnifiedAnswerFeedbackDto
{
    public bool IsCorrect { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? Feedback { get; set; }

    // Mastery changes
    public double PreviousMastery { get; set; }
    public double NewMastery { get; set; }
    public double MasteryChange { get; set; }

    // FSRS info
    public int NewFsrsState { get; set; }
    public string NewFsrsStateName { get; set; } = string.Empty;
    public DateTime? NextReview { get; set; }

    // Streak
    public int CurrentStreak { get; set; }
    public bool IsNewBestStreak { get; set; }

    // Recommendations
    public string? RecommendedAction { get; set; }
    public List<string>? RelatedTopicsToStudy { get; set; }
}

/// <summary>
/// Impact of an exercise on entity state.
/// </summary>
public class UnifiedEntityImpact
{
    public int EntityId { get; set; }
    public double PreviousMastery { get; set; }
    public double NewMastery { get; set; }
    public double MasteryChange { get; set; }
    public double DecayFactorApplied { get; set; }
    public int FsrsStateChanged { get; set; }
    public int RelationshipsReinforced { get; set; }
    public int RelationshipsWeakened { get; set; }
    public DateTime NextReview { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result of prerequisite check.
/// </summary>
public class UnifiedPrerequisiteCheckResult
{
    public bool CanProceed { get; set; }
    public int TotalPrerequisites { get; set; }
    public int MetPrerequisites { get; set; }
    public List<BlockingPrerequisiteInfo> BlockingPrerequisites { get; set; } = new();
    public string? BlockReason { get; set; }
    public List<UnifiedEntityDto>? SuggestedLearningPath { get; set; }
}

/// <summary>
/// Comprehensive mastery statistics.
/// </summary>
public class UnifiedMasteryStatsDto
{
    public int TotalEntities { get; set; }
    public int MasteredEntities { get; set; }  // Mastery >= 0.8
    public int LearningEntities { get; set; }  // 0.3 <= Mastery < 0.8
    public int NewEntities { get; set; }       // Mastery < 0.3

    public double AverageMastery { get; set; }
    public double AverageEffectiveKnowledge { get; set; }

    public int TotalAttempts { get; set; }
    public int TotalCorrect { get; set; }
    public double OverallSuccessRate { get; set; }

    // By subject
    public Dictionary<string, SubjectMasteryDto> BySubject { get; set; } = new();

    // By Bloom level
    public Dictionary<int, BloomLevelStatsDto> ByBloomLevel { get; set; } = new();

    // By difficulty (20/40/40)
    public DifficultyStatsDto EasyStats { get; set; } = new();
    public DifficultyStatsDto MediumStats { get; set; } = new();
    public DifficultyStatsDto HardStats { get; set; } = new();

    // Streaks
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    // Spaced repetition
    public int DueForReviewCount { get; set; }
    public int OverdueCount { get; set; }
}

/// <summary>
/// Stats per Bloom level.
/// </summary>
public class BloomLevelStatsDto
{
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public int Correct { get; set; }
    public double SuccessRate => Attempts > 0 ? (double)Correct / Attempts : 0;
}

/// <summary>
/// Stats per difficulty level.
/// </summary>
public class DifficultyStatsDto
{
    public string Difficulty { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Correct { get; set; }
    public double SuccessRate => Total > 0 ? (double)Correct / Total : 0;
    public double TargetRatio { get; set; }
    public double ActualRatio { get; set; }
}

/// <summary>
/// A weak area the user should practice.
/// </summary>
public class UnifiedWeakAreaDto
{
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;

    public double MasteryScore { get; set; }
    public double EffectiveKnowledge { get; set; }
    public int Attempts { get; set; }
    public int Correct { get; set; }
    public double SuccessRate { get; set; }

    /// <summary>
    /// Reason: "low_mastery", "overdue", "high_error_rate", "decaying_fast"
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    public int Priority { get; set; }

    public bool IsBlocked { get; set; }
    public List<string>? BlockingPrerequisites { get; set; }
}

/// <summary>
/// Learning summary for dashboard.
/// </summary>
public class UnifiedLearningSummaryDto
{
    public int UserId { get; set; }

    // Overall progress
    public int TotalEntities { get; set; }
    public int MasteredCount { get; set; }
    public double OverallMastery { get; set; }
    public double OverallEffectiveKnowledge { get; set; }

    // Today's activity
    public int TodayAttempts { get; set; }
    public int TodayCorrect { get; set; }
    public int TodayNewEntities { get; set; }

    // Recommendations
    public int DueForReviewCount { get; set; }
    public int WeakAreasCount { get; set; }
    public int BlockedEntitiesCount { get; set; }

    // Top priority
    public UnifiedLearningPriority? TopPriority { get; set; }

    // Streaks
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public bool StreakAtRisk { get; set; }

    // Next deadline
    public DateTime? NextDeadline { get; set; }
    public string? NextDeadlineName { get; set; }
    public int? DaysUntilNextDeadline { get; set; }
}

#endregion
