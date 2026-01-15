using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.OmniLearning;

// Helper extension for converting entities to DTOs
public static class OmniLearningExtensions
{
    public static UnifiedEntityDto ToDto(this UnifiedKnowledgeEntity entity)
    {
        return new UnifiedEntityDto
        {
            Id = entity.Id,
            EntityType = entity.EntityType,
            Name = entity.Name,
            Description = entity.Description,
            Subject = entity.Subject,
            Topic = entity.Topic,
            Subtopic = entity.Subtopic,
            ConfidenceScore = entity.ConfidenceScore,
            ImportanceScore = entity.ImportanceScore,
            OccurrenceCount = entity.OccurrenceCount,
            IsVerified = entity.IsVerified,
            MasteryScore = entity.MasteryScore,
            EffectiveKnowledge = entity.EffectiveKnowledge,
            DecayFactor = entity.DecayFactor,
            TotalAttempts = entity.TotalAttempts,
            TotalCorrect = entity.TotalCorrect,
            SuccessRate = entity.SuccessRate,
            CurrentBloomLevel = entity.CurrentBloomLevel,
            BloomLevelName = OmniLearningEngineService.GetBloomLevelName(entity.CurrentBloomLevel),
            EasySuccessRate = entity.EasySuccessRate,
            MediumSuccessRate = entity.MediumSuccessRate,
            HardSuccessRate = entity.HardSuccessRate,
            NextReview = entity.NextReview,
            FsrsState = entity.FsrsState,
            FsrsStateName = OmniLearningEngineService.GetFsrsStateName(entity.FsrsState),
            SourceDocumentId = entity.SourceDocumentId,
            LastInteraction = entity.LastInteraction,
            CreatedAt = entity.CreatedAt
        };
    }

    public static UnifiedRelationshipDto ToDto(this UnifiedKnowledgeRelationship relationship)
    {
        return new UnifiedRelationshipDto
        {
            Id = relationship.Id,
            SourceEntityId = relationship.SourceEntityId,
            SourceEntityName = relationship.SourceEntity?.Name ?? "",
            TargetEntityId = relationship.TargetEntityId,
            TargetEntityName = relationship.TargetEntity?.Name ?? "",
            RelationshipType = relationship.RelationshipType,
            CurrentStrength = relationship.CurrentStrength,
            Evidence = relationship.Evidence,
            Description = relationship.Description,
            IsVerified = relationship.IsVerified,
            IsBidirectional = relationship.IsBidirectional,
            IsPrerequisite = relationship.IsPrerequisite,
            RequiredMasteryLevel = relationship.IsPrerequisite ? relationship.RequiredMasteryLevel : null,
            IsStrict = relationship.IsStrict
        };
    }
}

/// <summary>
/// Omnifunktionales Lernsystem - Zentrales Interface
/// Kombiniert alle Lernfunktionen in einer einheitlichen API:
/// - Dokumentenverarbeitung und Wissensextraktion
/// - Knowledge Graph mit FSRS + Decay
/// - Adaptive Übungsgenerierung (alle 6 Komponententypen)
/// - Spaced Repetition mit Bloom's Taxonomy
/// - Einheitliche Lern-Warteschlange
/// </summary>
public interface IOmniLearningEngineService
{
    #region Initialisierung

    /// <summary>
    /// Initialisiert den Service (Qdrant Collections, etc.)
    /// </summary>
    Task InitializeAsync();

    #endregion

    #region Dokumentenverarbeitung

    /// <summary>
    /// Verarbeitet ein Dokument: Chunking → Embedding → Entity-Extraktion → Knowledge Graph
    /// </summary>
    Task<DocumentProcessingResult> ProcessDocumentAsync(int documentId, int userId, ProcessingOptions? options = null);

    /// <summary>
    /// Verarbeitet mehrere Dokumente im Batch
    /// </summary>
    Task<BatchProcessingResult> ProcessDocumentsBatchAsync(int[] documentIds, int userId, ProcessingOptions? options = null);

    #endregion

    #region Entity-Management

    /// <summary>
    /// Erstellt eine neue Wissens-Entität
    /// </summary>
    Task<UnifiedKnowledgeEntity> CreateEntityAsync(int userId, CreateEntityDto dto);

    /// <summary>
    /// Holt eine Entität mit allen Details
    /// </summary>
    Task<UnifiedKnowledgeEntity?> GetEntityAsync(int entityId, int userId);

    /// <summary>
    /// Sucht Entitäten (semantisch oder textbasiert)
    /// </summary>
    Task<List<UnifiedEntityDto>> SearchEntitiesAsync(int userId, string query, EntitySearchFilters? filters = null);

    /// <summary>
    /// Holt verwandte Entitäten (Graph-Traversierung)
    /// </summary>
    Task<List<UnifiedEntityDto>> GetRelatedEntitiesAsync(int entityId, int userId, int depth = 2);

    /// <summary>
    /// Merged doppelte Entitäten
    /// </summary>
    Task<UnifiedKnowledgeEntity> MergeEntitiesAsync(int[] entityIds, int userId);

    /// <summary>
    /// Holt alle Entitäten eines Users
    /// </summary>
    Task<List<UnifiedEntityDto>> GetUserEntitiesAsync(int userId, EntityListFilters? filters = null);

    #endregion

    #region Beziehungs-Management

    /// <summary>
    /// Erstellt eine neue Beziehung zwischen Entitäten
    /// </summary>
    Task<UnifiedKnowledgeRelationship> CreateRelationshipAsync(int userId, CreateRelationshipDto dto);

    /// <summary>
    /// Generiert automatisch Beziehungen für eine Entität
    /// </summary>
    Task<List<UnifiedKnowledgeRelationship>> GenerateRelationshipsAsync(int entityId, int userId);

    /// <summary>
    /// Prüft ob Prerequisites erfüllt sind
    /// </summary>
    Task<PrerequisiteCheckResult> CheckPrerequisitesAsync(int userId, int targetEntityId);

    /// <summary>
    /// Holt die Prerequisite-Kette für eine Entität
    /// </summary>
    Task<List<PrerequisiteChainDto>> GetPrerequisiteChainAsync(int entityId, int userId);

    #endregion

    #region Übungsgenerierung

    /// <summary>
    /// Generiert eine einzelne Übung
    /// </summary>
    Task<OmniExerciseDto> GenerateExerciseAsync(int userId, GenerateExerciseRequest request);

    /// <summary>
    /// Generiert eine Lern-Session mit mehreren Übungen
    /// </summary>
    Task<List<OmniExerciseDto>> GenerateSessionAsync(int userId, GenerateSessionRequest request);

    /// <summary>
    /// Reicht eine Antwort ein und erhält Feedback
    /// </summary>
    Task<ExerciseSubmissionResult> SubmitAnswerAsync(int exerciseId, int userId, AnswerSubmissionDto submission);

    /// <summary>
    /// Holt Übungen die zur Wiederholung fällig sind
    /// </summary>
    Task<List<OmniExerciseDto>> GetDueExercisesAsync(int userId, int limit = 10);

    #endregion

    #region Adaptive Scheduling

    /// <summary>
    /// Berechnet Lern-Prioritäten für einen User
    /// </summary>
    Task<List<UnifiedLearningPriority>> CalculatePrioritiesAsync(int userId, PriorityCalculationOptions? options = null);

    /// <summary>
    /// Holt die nächste empfohlene Übung
    /// </summary>
    Task<OmniExerciseDto?> GetNextExerciseAsync(int userId);

    /// <summary>
    /// Holt Schwachstellen des Users
    /// </summary>
    Task<List<WeakAreaDto>> GetWeakAreasAsync(int userId, int limit = 10);

    /// <summary>
    /// Holt überfällige Wiederholungen
    /// </summary>
    Task<List<OverdueItemDto>> GetOverdueItemsAsync(int userId);

    #endregion

    #region Visualisierung

    /// <summary>
    /// Holt den Knowledge Graph für Visualisierung
    /// </summary>
    Task<KnowledgeGraphDto> GetKnowledgeGraphAsync(int userId, GraphVisualizationFilters? filters = null);

    /// <summary>
    /// Holt Cluster-Visualisierung (2D-Projektion)
    /// </summary>
    Task<ClusterVisualizationDto> GetClusterVisualizationAsync(int userId, string? subject = null);

    #endregion

    #region Analytics

    /// <summary>
    /// Holt Mastery-Statistiken
    /// </summary>
    Task<MasteryStatsDto> GetMasteryStatsAsync(int userId);

    /// <summary>
    /// Holt Lern-Streak-Informationen
    /// </summary>
    Task<LearningStreakDto> GetStreakAsync(int userId);

    /// <summary>
    /// Holt Schwierigkeitsverteilung (20/40/40)
    /// </summary>
    Task<DifficultyDistributionDto> GetDifficultyDistributionAsync(int userId, string? subject = null);

    /// <summary>
    /// Holt Bloom-Progression
    /// </summary>
    Task<BloomProgressionDto> GetBloomProgressionAsync(int userId, string? subject = null);

    #endregion
}

#region DTOs

/// <summary>
/// Optionen für Dokumentenverarbeitung
/// </summary>
public class ProcessingOptions
{
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 100;
    public bool ExtractEntities { get; set; } = true;
    public bool ExtractRelationships { get; set; } = true;
    public bool GenerateEmbeddings { get; set; } = true;
    public string? FocusSubject { get; set; }
    public string? FocusTopic { get; set; }
}

/// <summary>
/// Ergebnis der Dokumentenverarbeitung
/// </summary>
public class DocumentProcessingResult
{
    public int DocumentId { get; set; }
    public int ChunksCreated { get; set; }
    public int EntitiesExtracted { get; set; }
    public int RelationshipsCreated { get; set; }
    public int EmbeddingsGenerated { get; set; }
    public List<string> Warnings { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Batch-Verarbeitungsergebnis
/// </summary>
public class BatchProcessingResult
{
    public int TotalDocuments { get; set; }
    public int SuccessfulDocuments { get; set; }
    public int FailedDocuments { get; set; }
    public List<DocumentProcessingResult> Results { get; set; } = new();
    public TimeSpan TotalProcessingTime { get; set; }
}

/// <summary>
/// DTO für Entity-Erstellung
/// </summary>
public class CreateEntityDto
{
    public string EntityType { get; set; } = "concept";
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Subject { get; set; }
    public required string Topic { get; set; }
    public string? Subtopic { get; set; }
    public int? SourceDocumentId { get; set; }
    public int? SourceChunkId { get; set; }
}

/// <summary>
/// DTO für Beziehungs-Erstellung
/// </summary>
public class CreateRelationshipDto
{
    public int SourceEntityId { get; set; }
    public int TargetEntityId { get; set; }
    public string RelationshipType { get; set; } = "relates_to";
    public string? Evidence { get; set; }
    public string? Description { get; set; }
    public bool IsStrict { get; set; } = false;
    public double RequiredMasteryLevel { get; set; } = 0.6;
}

/// <summary>
/// Unified Entity DTO für API-Responses
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
    public double ConfidenceScore { get; set; }
    public double ImportanceScore { get; set; }
    public int OccurrenceCount { get; set; }
    public bool IsVerified { get; set; }
    public double MasteryScore { get; set; }
    public double EffectiveKnowledge { get; set; }
    public double DecayFactor { get; set; }
    public int TotalAttempts { get; set; }
    public int TotalCorrect { get; set; }
    public double SuccessRate { get; set; }
    public int CurrentBloomLevel { get; set; }
    public string BloomLevelName { get; set; } = string.Empty;
    public double EasySuccessRate { get; set; }
    public double MediumSuccessRate { get; set; }
    public double HardSuccessRate { get; set; }
    public DateTime? NextReview { get; set; }
    public int FsrsState { get; set; }
    public string FsrsStateName { get; set; } = string.Empty;
    public int? SourceDocumentId { get; set; }
    public DateTime LastInteraction { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Unified Relationship DTO
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
    public bool IsPrerequisite { get; set; }
    public double? RequiredMasteryLevel { get; set; }
    public bool IsStrict { get; set; }
}

/// <summary>
/// Filter für Entity-Suche
/// </summary>
public class EntitySearchFilters
{
    public string? EntityType { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public double? MinMastery { get; set; }
    public double? MaxMastery { get; set; }
    public bool? HasEmbedding { get; set; }
    public bool SemanticSearch { get; set; } = true;
    public int Limit { get; set; } = 20;
}

/// <summary>
/// Filter für Entity-Liste
/// </summary>
public class EntityListFilters
{
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? EntityType { get; set; }
    public bool? NeedsReview { get; set; }
    public string SortBy { get; set; } = "mastery_asc";
    public int Limit { get; set; } = 50;
    public int Offset { get; set; } = 0;
}

/// <summary>
/// Ergebnis der Prerequisite-Prüfung
/// </summary>
public class PrerequisiteCheckResult
{
    public bool AllMet { get; set; }
    public List<BlockingPrerequisiteInfo> BlockingPrerequisites { get; set; } = new();
    public List<int> MetPrerequisiteIds { get; set; } = new();
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Prerequisite-Kette DTO
/// </summary>
public class PrerequisiteChainDto
{
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int Depth { get; set; }
    public double CurrentMastery { get; set; }
    public double RequiredMastery { get; set; }
    public bool IsMet { get; set; }
    public List<PrerequisiteChainDto> Prerequisites { get; set; } = new();
}

/// <summary>
/// Request für Übungsgenerierung
/// </summary>
public class GenerateExerciseRequest
{
    public int? EntityId { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? ExerciseType { get; set; }  // mc, fill_blank, drag_drop, slider, code, text_input
    public string? Difficulty { get; set; }     // easy, medium, hard, adaptive
    public int? BloomLevel { get; set; }        // 1-6, null = adaptive
    public bool UseAdaptive { get; set; } = true;
    public int? DocumentId { get; set; }
}

/// <summary>
/// Request für Session-Generierung
/// </summary>
public class GenerateSessionRequest
{
    public int Count { get; set; } = 5;
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public List<string>? ExerciseTypes { get; set; }
    public bool UseAdaptive { get; set; } = true;
    public bool IncludeOverdue { get; set; } = true;
    public string Mode { get; set; } = "learning"; // learning, exam_prep, exam_simulation
}

/// <summary>
/// Omni Exercise DTO
/// </summary>
public class OmniExerciseDto
{
    public int Id { get; set; }
    public string ExerciseType { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public object? Content { get; set; }  // Type-specific content (steps, options, etc.)
    public int BloomLevel { get; set; }
    public string BloomLevelName { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public int? EntityId { get; set; }
    public string? EntityName { get; set; }
    public int? DocumentId { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public int AttemptCount { get; set; }
    public double? LastScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Antwort-Einreichung
/// </summary>
public class AnswerSubmissionDto
{
    public required string Answer { get; set; }
    public object? StepAnswers { get; set; }  // For multi-step exercises
    public double? ResponseTimeSeconds { get; set; }
}

/// <summary>
/// Ergebnis der Antwort-Einreichung
/// </summary>
public class ExerciseSubmissionResult
{
    public bool IsCorrect { get; set; }
    public double Score { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? CorrectAnswer { get; set; }
    public double NewMasteryScore { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public int? NewBloomLevel { get; set; }
    public string? Achievement { get; set; }
}

/// <summary>
/// Optionen für Prioritätsberechnung
/// </summary>
public class PriorityCalculationOptions
{
    public bool IncludeDeadlines { get; set; } = true;
    public bool IncludeDecay { get; set; } = true;
    public bool IncludeBloomGap { get; set; } = true;
    public string? FocusSubject { get; set; }
    public double UrgencyWeight { get; set; } = 0.30;
    public double RelevanceWeight { get; set; } = 0.20;
    public double MasteryWeight { get; set; } = 0.25;
    public double DecayWeight { get; set; } = 0.15;
    public double BloomWeight { get; set; } = 0.10;
}

/// <summary>
/// Schwachstellen-DTO
/// </summary>
public class WeakAreaDto
{
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public double MasteryScore { get; set; }
    public double SuccessRate { get; set; }
    public int TotalAttempts { get; set; }
    public string WeaknessType { get; set; } = string.Empty;  // low_mastery, high_decay, low_bloom, etc.
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// Überfällige Items DTO
/// </summary>
public class OverdueItemDto
{
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public double CurrentMastery { get; set; }
    public double EstimatedMasteryLoss { get; set; }
}

/// <summary>
/// Filter für Graph-Visualisierung
/// </summary>
public class GraphVisualizationFilters
{
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public int? CenterEntityId { get; set; }
    public int Depth { get; set; } = 2;
    public double MinStrength { get; set; } = 0.3;
    public bool IncludeWeakEntities { get; set; } = true;
    public int MaxNodes { get; set; } = 100;
}

/// <summary>
/// Knowledge Graph DTO für Visualisierung
/// </summary>
public class KnowledgeGraphDto
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
    public GraphMetadata Metadata { get; set; } = new();
}

public class GraphNode
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public double MasteryScore { get; set; }
    public double Size { get; set; }  // Based on importance
    public string Color { get; set; } = string.Empty;  // Based on mastery
    public double? X { get; set; }
    public double? Y { get; set; }
}

public class GraphEdge
{
    public int Id { get; set; }
    public int Source { get; set; }
    public int Target { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
    public double Strength { get; set; }
    public bool IsPrerequisite { get; set; }
}

public class GraphMetadata
{
    public int TotalNodes { get; set; }
    public int TotalEdges { get; set; }
    public int SubjectCount { get; set; }
    public double AverageMastery { get; set; }
    public List<string> Subjects { get; set; } = new();
}

/// <summary>
/// Cluster-Visualisierung DTO
/// </summary>
public class ClusterVisualizationDto
{
    public List<ClusterPoint> Points { get; set; } = new();
    public List<ClusterInfo> Clusters { get; set; } = new();
}

public class ClusterPoint
{
    public int EntityId { get; set; }
    public string Label { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string ClusterId { get; set; } = string.Empty;
    public double MasteryScore { get; set; }
}

public class ClusterInfo
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public int EntityCount { get; set; }
    public double AverageMastery { get; set; }
}

/// <summary>
/// Mastery-Statistiken DTO
/// </summary>
public class MasteryStatsDto
{
    public double OverallMastery { get; set; }
    public int TotalEntities { get; set; }
    public int MasteredEntities { get; set; }  // >= 0.8 mastery
    public int LearningEntities { get; set; }  // 0.3 - 0.8 mastery
    public int NewEntities { get; set; }       // < 0.3 mastery
    public int TotalExercises { get; set; }
    public int CorrectAnswers { get; set; }
    public double OverallSuccessRate { get; set; }
    public Dictionary<string, SubjectStats> BySubject { get; set; } = new();
    public Dictionary<int, double> ByBloomLevel { get; set; } = new();
}

public class SubjectStats
{
    public string Subject { get; set; } = string.Empty;
    public double AverageMastery { get; set; }
    public int EntityCount { get; set; }
    public int ExerciseCount { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Lern-Streak DTO
/// </summary>
public class LearningStreakDto
{
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int TotalActiveDays { get; set; }
    public List<DateTime> RecentActivityDates { get; set; } = new();
}

/// <summary>
/// Schwierigkeitsverteilung DTO
/// </summary>
public class DifficultyDistributionDto
{
    public int EasyTotal { get; set; }
    public int EasyCorrect { get; set; }
    public double EasySuccessRate { get; set; }
    public int MediumTotal { get; set; }
    public int MediumCorrect { get; set; }
    public double MediumSuccessRate { get; set; }
    public int HardTotal { get; set; }
    public int HardCorrect { get; set; }
    public double HardSuccessRate { get; set; }
    public string RecommendedDifficulty { get; set; } = string.Empty;
    public bool FollowsTwentyFortyForty { get; set; }
    public string DistributionAdvice { get; set; } = string.Empty;
}

/// <summary>
/// Bloom-Progression DTO
/// </summary>
public class BloomProgressionDto
{
    public int CurrentLevel { get; set; }
    public string CurrentLevelName { get; set; } = string.Empty;
    public int TargetLevel { get; set; }
    public string TargetLevelName { get; set; } = string.Empty;
    public Dictionary<int, BloomLevelStats> LevelStats { get; set; } = new();
    public bool CanAdvance { get; set; }
    public string ProgressAdvice { get; set; } = string.Empty;
}

public class BloomLevelStats
{
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public int Correct { get; set; }
    public double SuccessRate { get; set; }
    public bool IsMastered { get; set; }
}

#endregion
