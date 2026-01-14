using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Knowledge Graph Entity - represents a concept, definition, formula, person, date, or example
/// extracted from document chunks for the Learning Engine.
/// </summary>
[Table("kg_entities")]
public class KgEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// User who owns this entity (null = global/system entity)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Source document ID
    /// </summary>
    public int? DocumentId { get; set; }

    /// <summary>
    /// Source chunk ID (where this entity was extracted from)
    /// </summary>
    public int? ChunkId { get; set; }

    /// <summary>
    /// Entity type: concept, definition, formula, person, date, example, theorem, method, term
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
    [MaxLength(100)]
    public string? Subject { get; set; }

    /// <summary>
    /// Topic within the subject (e.g., "Lineare Algebra", "OOP")
    /// </summary>
    [MaxLength(200)]
    public string? Topic { get; set; }

    /// <summary>
    /// Confidence score of the extraction (0.0 to 1.0)
    /// </summary>
    public double ConfidenceScore { get; set; } = 1.0;

    /// <summary>
    /// Number of times this entity appears across documents
    /// </summary>
    public int OccurrenceCount { get; set; } = 1;

    /// <summary>
    /// Importance score based on frequency and relationships (0.0 to 1.0)
    /// </summary>
    public double ImportanceScore { get; set; } = 0.5;

    /// <summary>
    /// Whether this entity has a vector embedding in Qdrant
    /// </summary>
    public bool HasEmbedding { get; set; } = false;

    /// <summary>
    /// Qdrant point ID for this entity's embedding
    /// </summary>
    [MaxLength(100)]
    public string? QdrantPointId { get; set; }

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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("DocumentId")]
    public virtual Document? Document { get; set; }

    [ForeignKey("ChunkId")]
    public virtual DocumentChunk? Chunk { get; set; }

    /// <summary>
    /// Relationships where this entity is the source
    /// </summary>
    public virtual ICollection<KgRelationship> OutgoingRelationships { get; set; } = new List<KgRelationship>();

    /// <summary>
    /// Relationships where this entity is the target
    /// </summary>
    public virtual ICollection<KgRelationship> IncomingRelationships { get; set; } = new List<KgRelationship>();
}

/// <summary>
/// Constants for entity types
/// </summary>
public static class KgEntityTypes
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
}
