using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for the DeepTutor-style Learning Engine.
/// Handles document processing pipeline, knowledge graph extraction, and adaptive question generation.
/// </summary>
public interface ILearningEngineService
{
    // === Document Processing Pipeline ===

    /// <summary>
    /// Processes a document through the full learning engine pipeline:
    /// 1. Parse → 2. Chunk → 3. Embed → 4. Extract Entities → 5. Extract Relationships
    /// </summary>
    Task<LearningDocumentResult> ProcessDocumentAsync(
        int documentId,
        int userId,
        LearningProcessingOptions? options = null);

    /// <summary>
    /// Processes multiple documents in batch.
    /// </summary>
    Task<List<LearningDocumentResult>> ProcessDocumentsBatchAsync(
        IEnumerable<int> documentIds,
        int userId,
        LearningProcessingOptions? options = null);

    /// <summary>
    /// Extracts entities and relationships from a specific chunk.
    /// </summary>
    Task<ChunkExtractionResult> ExtractFromChunkAsync(
        int chunkId,
        int userId);

    // === Knowledge Graph Operations ===

    /// <summary>
    /// Gets the knowledge graph for a document (entities + relationships).
    /// </summary>
    Task<KnowledgeGraphDto> GetDocumentKnowledgeGraphAsync(
        int documentId,
        int userId);

    /// <summary>
    /// Gets the full knowledge graph for a user (across all documents).
    /// </summary>
    Task<KnowledgeGraphDto> GetUserKnowledgeGraphAsync(
        int userId,
        KnowledgeGraphOptions? options = null);

    /// <summary>
    /// Gets entities related to a specific entity.
    /// </summary>
    Task<List<KgEntityDto>> GetRelatedEntitiesAsync(
        int entityId,
        int userId,
        int depth = 1);

    /// <summary>
    /// Searches entities by name or description.
    /// </summary>
    Task<List<KgEntityDto>> SearchEntitiesAsync(
        int userId,
        string query,
        string? entityType = null,
        int limit = 20);

    /// <summary>
    /// Merges duplicate entities.
    /// </summary>
    Task<bool> MergeEntitiesAsync(
        int primaryEntityId,
        IEnumerable<int> duplicateEntityIds,
        int userId);

    // === Question Generation ===

    /// <summary>
    /// Generates questions based on document content and knowledge graph.
    /// </summary>
    Task<List<LearningQuestionDto>> GenerateQuestionsAsync(
        int userId,
        QuestionGenerationRequest request);

    /// <summary>
    /// Generates questions for a specific entity.
    /// </summary>
    Task<List<LearningQuestionDto>> GenerateEntityQuestionsAsync(
        int entityId,
        int userId,
        int count = 5,
        string? questionType = null,
        int? bloomLevel = null);

    // === User Performance ===

    /// <summary>
    /// Records a user's answer to a question.
    /// </summary>
    Task<AnswerFeedbackDto> SubmitAnswerAsync(
        int userId,
        AnswerSubmission submission);

    /// <summary>
    /// Gets entities the user needs to practice (weak areas).
    /// </summary>
    Task<List<WeakAreaDto>> GetWeakAreasAsync(
        int userId,
        int limit = 10);

    /// <summary>
    /// Gets entities due for review (spaced repetition).
    /// </summary>
    Task<List<KgEntityDto>> GetDueForReviewAsync(
        int userId,
        int limit = 10);

    /// <summary>
    /// Gets user's mastery statistics.
    /// </summary>
    Task<MasteryStatsDto> GetMasteryStatsAsync(
        int userId,
        string? subject = null);
}

// === DTOs ===

/// <summary>
/// Options for learning engine document processing.
/// </summary>
public class LearningProcessingOptions
{
    public bool ExtractEntities { get; set; } = true;
    public bool ExtractRelationships { get; set; } = true;
    public bool GenerateEmbeddings { get; set; } = true;
    public bool UseSemanticChunking { get; set; } = true;
    public int TargetChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 100;
    public double EntityConfidenceThreshold { get; set; } = 0.7;
    public double RelationshipStrengthThreshold { get; set; } = 0.5;
}

/// <summary>
/// Result of processing a document through the learning engine.
/// </summary>
public class LearningDocumentResult
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public int ChunksCreated { get; set; }
    public int EntitiesExtracted { get; set; }
    public int RelationshipsExtracted { get; set; }
    public int EmbeddingsGenerated { get; set; }

    public List<string> Warnings { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Result of extracting entities/relationships from a chunk.
/// </summary>
public class ChunkExtractionResult
{
    public int ChunkId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public List<KgEntityDto> Entities { get; set; } = new();
    public List<KgRelationshipDto> Relationships { get; set; } = new();
}

/// <summary>
/// Knowledge graph representation for API responses.
/// </summary>
public class KnowledgeGraphDto
{
    public List<KgEntityDto> Entities { get; set; } = new();
    public List<KgRelationshipDto> Relationships { get; set; } = new();
    public KnowledgeGraphStats Stats { get; set; } = new();
}

/// <summary>
/// Statistics about a knowledge graph.
/// </summary>
public class KnowledgeGraphStats
{
    public int TotalEntities { get; set; }
    public int TotalRelationships { get; set; }
    public Dictionary<string, int> EntitiesByType { get; set; } = new();
    public Dictionary<string, int> RelationshipsByType { get; set; } = new();
    public int DocumentsCovered { get; set; }
    public int ChunksCovered { get; set; }
}

/// <summary>
/// Options for knowledge graph retrieval.
/// </summary>
public class KnowledgeGraphOptions
{
    public List<int>? DocumentIds { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public List<string>? EntityTypes { get; set; }
    public int? MaxEntities { get; set; }
    public int? MaxDepth { get; set; }
    public double? MinImportance { get; set; }
}

/// <summary>
/// Entity DTO for API responses.
/// </summary>
public class KgEntityDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public double ConfidenceScore { get; set; }
    public double ImportanceScore { get; set; }
    public int OccurrenceCount { get; set; }
    public bool IsVerified { get; set; }

    // Source info
    public int? DocumentId { get; set; }
    public string? DocumentName { get; set; }
    public int? ChunkId { get; set; }

    // User performance (if available)
    public double? MasteryScore { get; set; }
    public DateTime? NextReview { get; set; }
}

/// <summary>
/// Relationship DTO for API responses.
/// </summary>
public class KgRelationshipDto
{
    public int Id { get; set; }
    public int SourceEntityId { get; set; }
    public string SourceEntityName { get; set; } = string.Empty;
    public int TargetEntityId { get; set; }
    public string TargetEntityName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public double Strength { get; set; }
    public string? Evidence { get; set; }
    public string? Description { get; set; }
    public bool IsVerified { get; set; }
}

/// <summary>
/// Request for question generation.
/// </summary>
public class QuestionGenerationRequest
{
    public List<int>? DocumentIds { get; set; }
    public List<int>? EntityIds { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }

    public int Count { get; set; } = 10;
    public string Difficulty { get; set; } = "adaptive";
    public List<string>? QuestionTypes { get; set; }
    public int? MinBloomLevel { get; set; }
    public int? MaxBloomLevel { get; set; }
}

/// <summary>
/// A generated learning question.
/// </summary>
public class LearningQuestionDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string QuestionType { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public int BloomLevel { get; set; }
    public double Difficulty { get; set; }

    // Source information
    public int? EntityId { get; set; }
    public string? EntityName { get; set; }
    public int? SourceChunkId { get; set; }
    public int? SourceDocumentId { get; set; }
    public string? SourceDocumentName { get; set; }

    // For display
    public string? Hint { get; set; }
    public List<string>? RelatedConcepts { get; set; }
}

/// <summary>
/// Answer submission from user.
/// </summary>
public class AnswerSubmission
{
    public string QuestionId { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string UserAnswer { get; set; } = string.Empty;
    public double? ResponseTimeSeconds { get; set; }
    public string? QuestionType { get; set; }
    public int? BloomLevel { get; set; }
}

/// <summary>
/// Feedback for a submitted answer.
/// </summary>
public class AnswerFeedbackDto
{
    public bool IsCorrect { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? Feedback { get; set; }

    // Updated mastery info
    public double NewMasteryScore { get; set; }
    public double MasteryChange { get; set; }
    public DateTime? NextReview { get; set; }

    // Suggestions
    public List<string>? RelatedTopicsToStudy { get; set; }
}

/// <summary>
/// A weak area the user should practice.
/// </summary>
public class WeakAreaDto
{
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Topic { get; set; }

    public double MasteryScore { get; set; }
    public int Attempts { get; set; }
    public int Correct { get; set; }
    public double SuccessRate => Attempts > 0 ? (double)Correct / Attempts : 0;

    public string Reason { get; set; } = string.Empty; // "low_mastery", "overdue", "high_error_rate"
    public int Priority { get; set; }
}

/// <summary>
/// Mastery statistics for a user.
/// </summary>
public class MasteryStatsDto
{
    public int TotalEntities { get; set; }
    public int MasteredEntities { get; set; }
    public int LearningEntities { get; set; }
    public int NewEntities { get; set; }

    public double AverageMastery { get; set; }
    public int TotalAttempts { get; set; }
    public int TotalCorrect { get; set; }
    public double OverallSuccessRate { get; set; }

    public Dictionary<string, SubjectMasteryDto> BySubject { get; set; } = new();
    public Dictionary<int, int> ByBloomLevel { get; set; } = new();

    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
}

/// <summary>
/// Mastery stats for a specific subject.
/// </summary>
public class SubjectMasteryDto
{
    public string Subject { get; set; } = string.Empty;
    public int TotalEntities { get; set; }
    public int MasteredEntities { get; set; }
    public double AverageMastery { get; set; }
    public int Attempts { get; set; }
    public int Correct { get; set; }
}
