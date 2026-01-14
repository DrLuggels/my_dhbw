using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Unified Knowledge Relationship - combines KgRelationship, UserKnowledgeEdge, and PrerequisiteChain
/// into a single comprehensive relationship model with:
/// - 14 relationship types including prerequisite
/// - Time-decay for relationship strength
/// - Prerequisite enforcement attributes
/// - Evidence and extraction tracking
/// </summary>
[Table("unified_knowledge_relationships")]
public class UnifiedKnowledgeRelationship
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// User who owns this relationship
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Source entity ID
    /// </summary>
    [Required]
    public int SourceEntityId { get; set; }

    /// <summary>
    /// Target entity ID
    /// </summary>
    [Required]
    public int TargetEntityId { get; set; }

    #region Relationship Type & Metadata (from KgRelationship)

    /// <summary>
    /// Relationship type: is_a, part_of, relates_to, requires, prerequisite, contradicts,
    /// example_of, defines, uses, precedes, derives_from, extends, implements, similar_to
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RelationshipType { get; set; } = UnifiedRelationshipTypes.RelatesTo;

    /// <summary>
    /// Evidence text that supports this relationship
    /// </summary>
    [MaxLength(1000)]
    public string? Evidence { get; set; }

    /// <summary>
    /// Description of the relationship
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Source chunk ID where this relationship was extracted from
    /// </summary>
    public int? ExtractedFromChunkId { get; set; }

    /// <summary>
    /// Source document ID where this relationship was extracted from
    /// </summary>
    public int? ExtractedFromDocumentId { get; set; }

    /// <summary>
    /// Whether this relationship was auto-extracted (vs manually created)
    /// </summary>
    public bool IsAutoExtracted { get; set; } = true;

    /// <summary>
    /// Whether this relationship has been verified by a user
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Whether this is a bidirectional relationship
    /// </summary>
    public bool IsBidirectional { get; set; } = false;

    /// <summary>
    /// Whether this relationship is active (not deleted)
    /// </summary>
    public bool IsActive { get; set; } = true;

    #endregion

    #region Strength with Decay (from UserKnowledgeEdge)

    /// <summary>
    /// Initial/base strength of the relationship (0.0 to 1.0)
    /// </summary>
    public double InitialStrength { get; set; } = 1.0;

    /// <summary>
    /// Decay rate per day for relationship strength
    /// Default: 3% per day (slower than entities)
    /// </summary>
    public double DecayRate { get; set; } = 0.03;

    /// <summary>
    /// Last time this relationship was reinforced
    /// </summary>
    public DateTime LastReinforced { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Computed current strength with exponential decay applied.
    /// Formula: InitialStrength × e^(-DecayRate × daysSinceReinforcement)
    /// </summary>
    [NotMapped]
    public double CurrentStrength
    {
        get
        {
            var daysSinceReinforcement = (DateTime.UtcNow - LastReinforced).TotalDays;
            return InitialStrength * Math.Exp(-DecayRate * daysSinceReinforcement);
        }
    }

    /// <summary>
    /// Number of times this relationship has been reinforced
    /// </summary>
    public int ReinforcementCount { get; set; } = 0;

    /// <summary>
    /// Number of times this relationship has been weakened
    /// </summary>
    public int WeakeningCount { get; set; } = 0;

    #endregion

    #region Prerequisite Attributes (from PrerequisiteChain)

    /// <summary>
    /// For prerequisite relationships: minimum mastery level required (0.0 to 1.0)
    /// Default: 60% mastery required
    /// </summary>
    public double RequiredMasteryLevel { get; set; } = 0.6;

    /// <summary>
    /// For prerequisite relationships: if true, strictly enforced (blocks progress).
    /// If false, only a warning is shown.
    /// </summary>
    public bool IsStrict { get; set; } = true;

    /// <summary>
    /// Confidence score for auto-generated relationships (0.0 to 1.0)
    /// </summary>
    public double ConfidenceScore { get; set; } = 1.0;

    #endregion

    #region Timestamps

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    #endregion

    #region Navigation Properties

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("SourceEntityId")]
    public virtual UnifiedKnowledgeEntity SourceEntity { get; set; } = null!;

    [ForeignKey("TargetEntityId")]
    public virtual UnifiedKnowledgeEntity TargetEntity { get; set; } = null!;

    [ForeignKey("ExtractedFromChunkId")]
    public virtual DocumentChunk? ExtractedFromChunk { get; set; }

    [ForeignKey("ExtractedFromDocumentId")]
    public virtual Document? ExtractedFromDocument { get; set; }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Reinforce this relationship (increases strength)
    /// </summary>
    public void Reinforce(double strengthBoost = 0.1)
    {
        ReinforcementCount++;
        InitialStrength = Math.Min(1.0, InitialStrength + strengthBoost);
        LastReinforced = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Weaken this relationship (decreases strength)
    /// </summary>
    public void Weaken(double strengthPenalty = 0.1)
    {
        WeakeningCount++;
        InitialStrength = Math.Max(0.0, InitialStrength - strengthPenalty);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this is a prerequisite relationship
    /// </summary>
    [NotMapped]
    public bool IsPrerequisite => RelationshipType == UnifiedRelationshipTypes.Prerequisite ||
                                   RelationshipType == UnifiedRelationshipTypes.Requires;

    /// <summary>
    /// Check if prerequisite is met (source entity mastery >= required level)
    /// </summary>
    public bool CheckPrerequisiteMet(double sourceMasteryScore)
    {
        if (!IsPrerequisite) return true;
        return sourceMasteryScore >= RequiredMasteryLevel;
    }

    #endregion
}

/// <summary>
/// Unified relationship type constants (extended from KgRelationshipTypes + EdgeTypes)
/// </summary>
public static class UnifiedRelationshipTypes
{
    /// <summary>A is a type of B (inheritance/classification)</summary>
    public const string IsA = "is_a";

    /// <summary>A is part of B (composition)</summary>
    public const string PartOf = "part_of";

    /// <summary>A is related to B (general association)</summary>
    public const string RelatesTo = "relates_to";

    /// <summary>A requires B (soft dependency)</summary>
    public const string Requires = "requires";

    /// <summary>A is a prerequisite for B (hard dependency with mastery check)</summary>
    public const string Prerequisite = "prerequisite";

    /// <summary>A contradicts B (conflict/opposition)</summary>
    public const string Contradicts = "contradicts";

    /// <summary>A is an example of B</summary>
    public const string ExampleOf = "example_of";

    /// <summary>A defines B</summary>
    public const string Defines = "defines";

    /// <summary>A uses B</summary>
    public const string Uses = "uses";

    /// <summary>A precedes B (temporal/logical order)</summary>
    public const string Precedes = "precedes";

    /// <summary>A derives from B</summary>
    public const string DerivesFrom = "derives_from";

    /// <summary>A extends B (specialization)</summary>
    public const string Extends = "extends";

    /// <summary>A implements B</summary>
    public const string Implements = "implements";

    /// <summary>A is similar to B</summary>
    public const string SimilarTo = "similar_to";

    /// <summary>A applies B (application of concept)</summary>
    public const string Application = "application";

    /// <summary>
    /// Get all relationship types that indicate a learning dependency
    /// </summary>
    public static readonly string[] LearningDependencies = new[]
    {
        Prerequisite, Requires, DerivesFrom, Extends, Implements
    };

    /// <summary>
    /// Get all relationship types
    /// </summary>
    public static readonly string[] All = new[]
    {
        IsA, PartOf, RelatesTo, Requires, Prerequisite, Contradicts,
        ExampleOf, Defines, Uses, Precedes, DerivesFrom, Extends,
        Implements, SimilarTo, Application
    };
}
