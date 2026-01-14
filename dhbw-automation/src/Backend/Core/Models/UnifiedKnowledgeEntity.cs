using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Unified Knowledge Entity - combines KgEntity, UserKnowledgeNode, and UserEntityPerformance
/// into a single comprehensive learning entity with:
/// - Entity metadata and extraction info (from KgEntity)
/// - FSRS spaced repetition parameters (from UserEntityPerformance)
/// - Exponential decay model (from UserKnowledgeNode)
/// - 20/40/40 difficulty distribution tracking
/// - Bloom's Taxonomy performance per level
/// </summary>
[Table("unified_knowledge_entities")]
public class UnifiedKnowledgeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// User who owns this entity
    /// </summary>
    [Required]
    public int UserId { get; set; }

    #region Entity Metadata (from KgEntity)

    /// <summary>
    /// Entity type: concept, definition, formula, person, theorem, method, algorithm, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = "concept";

    /// <summary>
    /// Name/title of the entity
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Normalized name for matching (lowercase, no special chars)
    /// </summary>
    [MaxLength(255)]
    public string? NormalizedName { get; set; }

    /// <summary>
    /// Description or definition of the entity
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    /// <summary>
    /// Subject area (e.g., "Mathematik", "Programmierung")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Topic within the subject (e.g., "Lineare Algebra", "OOP")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Subtopic for finer granularity
    /// </summary>
    [MaxLength(200)]
    public string? Subtopic { get; set; }

    /// <summary>
    /// Confidence score of the extraction (0.0 to 1.0)
    /// </summary>
    public double ConfidenceScore { get; set; } = 1.0;

    /// <summary>
    /// Importance score based on frequency and relationships (0.0 to 1.0)
    /// </summary>
    public double ImportanceScore { get; set; } = 0.5;

    /// <summary>
    /// Number of times this entity appears across documents
    /// </summary>
    public int OccurrenceCount { get; set; } = 1;

    /// <summary>
    /// Source document ID (if extracted from a document)
    /// </summary>
    public int? SourceDocumentId { get; set; }

    /// <summary>
    /// Source chunk ID (where this entity was extracted from)
    /// </summary>
    public int? SourceChunkId { get; set; }

    /// <summary>
    /// Additional metadata as JSON (formulas, dates, etc.)
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this entity has been verified by a user
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Whether this entity is active (not deleted/merged)
    /// </summary>
    public bool IsActive { get; set; } = true;

    #endregion

    #region Embedding (from both KgEntity and UserKnowledgeNode)

    /// <summary>
    /// Whether this entity has a vector embedding in Qdrant
    /// </summary>
    public bool HasEmbedding { get; set; } = false;

    /// <summary>
    /// Qdrant point ID for this entity's embedding
    /// </summary>
    [MaxLength(100)]
    public string? QdrantPointId { get; set; }

    #endregion

    #region FSRS Parameters (from UserEntityPerformance)

    /// <summary>
    /// FSRS Stability - how long the memory will last (in days)
    /// </summary>
    public double Stability { get; set; } = 0.0;

    /// <summary>
    /// FSRS Difficulty - inherent difficulty of the item (0.0 to 1.0)
    /// </summary>
    public double Difficulty { get; set; } = 0.5;

    /// <summary>
    /// FSRS Elapsed days since last review
    /// </summary>
    public int ElapsedDays { get; set; } = 0;

    /// <summary>
    /// FSRS Scheduled days until next review
    /// </summary>
    public int ScheduledDays { get; set; } = 0;

    /// <summary>
    /// FSRS Number of reviews (reps)
    /// </summary>
    public int Reps { get; set; } = 0;

    /// <summary>
    /// FSRS Number of lapses (forgetting events)
    /// </summary>
    public int Lapses { get; set; } = 0;

    /// <summary>
    /// FSRS State: 0=New, 1=Learning, 2=Review, 3=Relearning
    /// </summary>
    public int FsrsState { get; set; } = 0;

    /// <summary>
    /// Next scheduled review date (spaced repetition)
    /// </summary>
    public DateTime? NextReview { get; set; }

    #endregion

    #region Decay Parameters (from UserKnowledgeNode)

    /// <summary>
    /// Base strength of the memory (refreshed on each interaction)
    /// </summary>
    public double BaseStrength { get; set; } = 1.0;

    /// <summary>
    /// Decay rate per day (Ebbinghaus forgetting curve)
    /// Default: 5% per day
    /// </summary>
    public double DecayRate { get; set; } = 0.05;

    /// <summary>
    /// Last time the user interacted with this entity
    /// </summary>
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Computed decay factor based on time since last interaction.
    /// Formula: e^(-DecayRate * daysSinceInteraction)
    /// </summary>
    [NotMapped]
    public double DecayFactor
    {
        get
        {
            var daysSinceInteraction = (DateTime.UtcNow - LastInteraction).TotalDays;
            return Math.Exp(-DecayRate * daysSinceInteraction);
        }
    }

    #endregion

    #region 20/40/40 Difficulty Distribution (from UserKnowledgeNode)

    /// <summary>Easy exercises correct count</summary>
    public int EasyCorrect { get; set; } = 0;
    /// <summary>Easy exercises total count</summary>
    public int EasyTotal { get; set; } = 0;
    /// <summary>Medium exercises correct count</summary>
    public int MediumCorrect { get; set; } = 0;
    /// <summary>Medium exercises total count</summary>
    public int MediumTotal { get; set; } = 0;
    /// <summary>Hard exercises correct count</summary>
    public int HardCorrect { get; set; } = 0;
    /// <summary>Hard exercises total count</summary>
    public int HardTotal { get; set; } = 0;

    /// <summary>Easy difficulty success rate</summary>
    [NotMapped]
    public double EasySuccessRate => EasyTotal > 0 ? (double)EasyCorrect / EasyTotal : 0.0;

    /// <summary>Medium difficulty success rate</summary>
    [NotMapped]
    public double MediumSuccessRate => MediumTotal > 0 ? (double)MediumCorrect / MediumTotal : 0.0;

    /// <summary>Hard difficulty success rate</summary>
    [NotMapped]
    public double HardSuccessRate => HardTotal > 0 ? (double)HardCorrect / HardTotal : 0.0;

    #endregion

    #region Bloom's Taxonomy Performance

    /// <summary>
    /// Current target Bloom level (1-6)
    /// 1=Remember, 2=Understand, 3=Apply, 4=Analyze, 5=Evaluate, 6=Create
    /// </summary>
    public int CurrentBloomLevel { get; set; } = 1;

    /// <summary>
    /// Performance per Bloom level as JSON: {"1": {"attempts": 5, "correct": 4}, "2": {...}}
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? BloomPerformanceJson { get; set; }

    /// <summary>
    /// Get Bloom performance as dictionary
    /// </summary>
    [NotMapped]
    public Dictionary<int, BloomLevelPerformance> BloomPerformance
    {
        get
        {
            if (string.IsNullOrEmpty(BloomPerformanceJson))
                return new Dictionary<int, BloomLevelPerformance>();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<int, BloomLevelPerformance>>(BloomPerformanceJson)
                       ?? new Dictionary<int, BloomLevelPerformance>();
            }
            catch
            {
                return new Dictionary<int, BloomLevelPerformance>();
            }
        }
        set
        {
            BloomPerformanceJson = JsonSerializer.Serialize(value);
        }
    }

    #endregion

    #region Overall Statistics

    /// <summary>
    /// Total number of exercise attempts
    /// </summary>
    public int TotalAttempts { get; set; } = 0;

    /// <summary>
    /// Total number of correct answers
    /// </summary>
    public int TotalCorrect { get; set; } = 0;

    /// <summary>
    /// Average response time in seconds
    /// </summary>
    public double AverageResponseTimeSeconds { get; set; } = 0.0;

    /// <summary>
    /// Current streak of consecutive correct answers
    /// </summary>
    public int CurrentStreak { get; set; } = 0;

    /// <summary>
    /// Best streak achieved
    /// </summary>
    public int BestStreak { get; set; } = 0;

    /// <summary>
    /// Overall success rate
    /// </summary>
    [NotMapped]
    public double SuccessRate => TotalAttempts > 0 ? (double)TotalCorrect / TotalAttempts : 0.0;

    #endregion

    #region Computed Mastery (FSRS + Decay fusion)

    /// <summary>
    /// FSRS-based mastery score (0.0 to 1.0)
    /// Calculated from success rate, difficulty, and stability
    /// </summary>
    [NotMapped]
    public double FsrsMastery
    {
        get
        {
            var successComponent = SuccessRate * 0.6;
            var difficultyComponent = (1.0 - Difficulty) * 0.2;
            var stabilityComponent = Math.Min(Stability / 30.0, 0.2);
            return successComponent + difficultyComponent + stabilityComponent;
        }
    }

    /// <summary>
    /// Effective knowledge score combining FSRS mastery with decay.
    /// Formula: FsrsMastery × (0.4 + 0.6 × DecayFactor) × BaseStrength
    /// This ensures even without recent interaction, knowledge doesn't drop to zero.
    /// </summary>
    [NotMapped]
    public double EffectiveKnowledge => FsrsMastery * (0.4 + 0.6 * DecayFactor) * BaseStrength;

    /// <summary>
    /// Stored mastery score (for database queries)
    /// Updated after each interaction
    /// </summary>
    public double MasteryScore { get; set; } = 0.0;

    #endregion

    #region Timestamps

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    #endregion

    #region Navigation Properties

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("SourceDocumentId")]
    public virtual Document? SourceDocument { get; set; }

    [ForeignKey("SourceChunkId")]
    public virtual DocumentChunk? SourceChunk { get; set; }

    /// <summary>
    /// Relationships where this entity is the source
    /// </summary>
    public virtual ICollection<UnifiedKnowledgeRelationship> OutgoingRelationships { get; set; } = new List<UnifiedKnowledgeRelationship>();

    /// <summary>
    /// Relationships where this entity is the target
    /// </summary>
    public virtual ICollection<UnifiedKnowledgeRelationship> IncomingRelationships { get; set; } = new List<UnifiedKnowledgeRelationship>();

    #endregion

    #region Helper Methods

    /// <summary>
    /// Update mastery score and last interaction time
    /// </summary>
    public void UpdateAfterInteraction()
    {
        LastInteraction = DateTime.UtcNow;
        MasteryScore = EffectiveKnowledge;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Record an exercise attempt
    /// </summary>
    public void RecordAttempt(bool isCorrect, string difficulty, double responseTimeSeconds, int bloomLevel)
    {
        TotalAttempts++;
        if (isCorrect)
        {
            TotalCorrect++;
            CurrentStreak++;
            if (CurrentStreak > BestStreak)
                BestStreak = CurrentStreak;
        }
        else
        {
            CurrentStreak = 0;
        }

        // Update difficulty-specific stats
        switch (difficulty.ToLower())
        {
            case "easy":
                EasyTotal++;
                if (isCorrect) EasyCorrect++;
                break;
            case "medium":
                MediumTotal++;
                if (isCorrect) MediumCorrect++;
                break;
            case "hard":
                HardTotal++;
                if (isCorrect) HardCorrect++;
                break;
        }

        // Update Bloom performance
        var bloom = BloomPerformance;
        if (!bloom.ContainsKey(bloomLevel))
            bloom[bloomLevel] = new BloomLevelPerformance();
        bloom[bloomLevel].Attempts++;
        if (isCorrect) bloom[bloomLevel].Correct++;
        BloomPerformance = bloom;

        // Update response time (exponential moving average)
        if (AverageResponseTimeSeconds == 0)
            AverageResponseTimeSeconds = responseTimeSeconds;
        else
            AverageResponseTimeSeconds = AverageResponseTimeSeconds * 0.8 + responseTimeSeconds * 0.2;

        UpdateAfterInteraction();
    }

    /// <summary>
    /// Get the next recommended difficulty based on 20/40/40 rule
    /// </summary>
    public string GetRecommendedDifficulty()
    {
        var total = EasyTotal + MediumTotal + HardTotal;
        if (total < 5) return "easy"; // Start with easy

        var easyRatio = (double)EasyTotal / total;
        var mediumRatio = (double)MediumTotal / total;

        // Target: 20% easy, 40% medium, 40% hard
        if (easyRatio < 0.20) return "easy";
        if (mediumRatio < 0.40) return "medium";
        return "hard";
    }

    /// <summary>
    /// Check if ready to advance to next Bloom level
    /// </summary>
    public bool CanAdvanceBloomLevel()
    {
        if (CurrentBloomLevel >= 6) return false;

        var performance = BloomPerformance;
        if (!performance.ContainsKey(CurrentBloomLevel)) return false;

        var current = performance[CurrentBloomLevel];
        return current.Attempts >= 3 && current.SuccessRate >= 0.7;
    }

    #endregion
}

/// <summary>
/// Performance tracking per Bloom's Taxonomy level
/// </summary>
public class BloomLevelPerformance
{
    public int Attempts { get; set; } = 0;
    public int Correct { get; set; } = 0;
    public double SuccessRate => Attempts > 0 ? (double)Correct / Attempts : 0.0;
}

/// <summary>
/// Constants for unified entity types (extended from KgEntityTypes)
/// </summary>
public static class UnifiedEntityTypes
{
    public const string Concept = "concept";
    public const string Definition = "definition";
    public const string Formula = "formula";
    public const string Person = "person";
    public const string Date = "date";
    public const string Example = "example";
    public const string Theorem = "theorem";
    public const string Method = "method";
    public const string Term = "term";
    public const string Algorithm = "algorithm";
    public const string DataStructure = "data_structure";
    public const string Principle = "principle";
    public const string Process = "process";
    public const string Pattern = "pattern";
}
