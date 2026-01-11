using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks vector embeddings stored in Qdrant
/// </summary>
[Table("qdrant_embeddings")]
public class QdrantEmbedding
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// User ID (for filtering in searches)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Entity type: "document", "exercise", "knowledge_item", "image", etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID in the database
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// Qdrant point ID (UUID string)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string QdrantPointId { get; set; } = string.Empty;

    /// <summary>
    /// Qdrant collection name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Embedding model used (e.g., "text-embedding-3-small")
    /// </summary>
    [MaxLength(100)]
    public string? EmbeddingModel { get; set; }

    /// <summary>
    /// Vector dimension
    /// </summary>
    public int VectorDimension { get; set; } = 1536;

    /// <summary>
    /// Text that was embedded (truncated for reference)
    /// </summary>
    [MaxLength(1000)]
    public string? EmbeddedTextPreview { get; set; }

    /// <summary>
    /// Full text length that was embedded
    /// </summary>
    public int FullTextLength { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

/// <summary>
/// Qdrant collection names
/// </summary>
public static class QdrantCollections
{
    public const string Documents = "dhbw_documents";
    public const string Exercises = "dhbw_exercises";
    public const string KnowledgeItems = "dhbw_knowledge";
    public const string Images = "dhbw_images";
}
