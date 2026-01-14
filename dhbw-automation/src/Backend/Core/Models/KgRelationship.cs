using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Knowledge Graph Relationship - represents a connection between two KgEntities.
/// Used for the Learning Engine to understand concept dependencies and relationships.
/// </summary>
[Table("kg_relationships")]
public class KgRelationship
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// User who owns this relationship (null = global/system relationship)
    /// </summary>
    public int? UserId { get; set; }

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

    /// <summary>
    /// Relationship type: is_a, part_of, relates_to, requires, contradicts, example_of, defines, uses, precedes, derives_from
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RelationshipType { get; set; } = "relates_to";

    /// <summary>
    /// Strength/confidence of the relationship (0.0 to 1.0)
    /// </summary>
    public double Strength { get; set; } = 1.0;

    /// <summary>
    /// Chunk ID where this relationship was extracted from
    /// </summary>
    public int? ExtractedFromChunkId { get; set; }

    /// <summary>
    /// Document ID where this relationship was extracted from
    /// </summary>
    public int? ExtractedFromDocumentId { get; set; }

    /// <summary>
    /// Evidence text that supports this relationship
    /// </summary>
    [MaxLength(1000)]
    public string? Evidence { get; set; }

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
    /// Description of the relationship
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this relationship is active (not deleted)
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("SourceEntityId")]
    public virtual KgEntity SourceEntity { get; set; } = null!;

    [ForeignKey("TargetEntityId")]
    public virtual KgEntity TargetEntity { get; set; } = null!;

    [ForeignKey("ExtractedFromChunkId")]
    public virtual DocumentChunk? ExtractedFromChunk { get; set; }

    [ForeignKey("ExtractedFromDocumentId")]
    public virtual Document? ExtractedFromDocument { get; set; }
}

/// <summary>
/// Constants for relationship types
/// </summary>
public static class KgRelationshipTypes
{
    /// <summary>A is a type of B (inheritance/classification)</summary>
    public const string IsA = "is_a";

    /// <summary>A is part of B (composition)</summary>
    public const string PartOf = "part_of";

    /// <summary>A is related to B (general association)</summary>
    public const string RelatesTo = "relates_to";

    /// <summary>A requires B (dependency/prerequisite)</summary>
    public const string Requires = "requires";

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
}
