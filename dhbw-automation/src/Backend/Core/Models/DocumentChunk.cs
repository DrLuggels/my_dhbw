using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Semantic chunk extracted from a document.
/// Each chunk represents a coherent topic section for granular vector search.
/// </summary>
[Table("document_chunks")]
public class DocumentChunk
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // === Parent Reference (Hierarchical) ===

    /// <summary>
    /// Parent document ID
    /// </summary>
    [Required]
    public int DocumentId { get; set; }

    /// <summary>
    /// User who owns this chunk (denormalized for query efficiency)
    /// </summary>
    [Required]
    public int UserId { get; set; }

    // === Chunk Content ===

    /// <summary>
    /// The actual text content of this chunk
    /// </summary>
    [Required]
    [Column(TypeName = "TEXT")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Character count of the content
    /// </summary>
    public int ContentLength { get; set; }

    // === Position & Structure ===

    /// <summary>
    /// Sequential chunk number within the document (0-indexed)
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Total number of chunks in the parent document
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// Start character position in original document
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// End character position in original document
    /// </summary>
    public int EndPosition { get; set; }

    /// <summary>
    /// Page number(s) if from PDF (comma-separated for multi-page chunks)
    /// </summary>
    [MaxLength(50)]
    public string? PageNumbers { get; set; }

    // === Semantic Metadata ===

    /// <summary>
    /// AI-generated topic label for this chunk
    /// </summary>
    [MaxLength(200)]
    public string? TopicLabel { get; set; }

    /// <summary>
    /// Brief summary of chunk content (AI-generated)
    /// </summary>
    [MaxLength(500)]
    public string? Summary { get; set; }

    /// <summary>
    /// Detected section heading if any
    /// </summary>
    [MaxLength(300)]
    public string? SectionHeading { get; set; }

    /// <summary>
    /// Chunk type: "introduction", "definition", "example", "exercise", "conclusion", "mixed"
    /// </summary>
    [MaxLength(50)]
    public string ChunkType { get; set; } = "mixed";

    /// <summary>
    /// Semantic similarity to previous chunk (0-1, for continuity analysis)
    /// </summary>
    public double? PreviousChunkSimilarity { get; set; }

    // === Embedding ===

    /// <summary>
    /// Whether this chunk has a vector embedding
    /// </summary>
    public bool HasEmbedding { get; set; } = false;

    /// <summary>
    /// Qdrant point ID for this chunk's embedding
    /// </summary>
    [MaxLength(100)]
    public string? QdrantPointId { get; set; }

    // === Link Tracking (for nightly job) ===

    /// <summary>
    /// Whether chunk-to-chunk links have been generated
    /// </summary>
    public bool HasBeenLinked { get; set; } = false;

    /// <summary>
    /// Whether chunk-to-event links have been generated
    /// </summary>
    public bool HasEventLinks { get; set; } = false;

    /// <summary>
    /// Whether chunk-to-knowledge-item links have been generated
    /// </summary>
    public bool HasKnowledgeLinks { get; set; } = false;

    /// <summary>
    /// Whether chunk-to-exercise links have been generated
    /// </summary>
    public bool HasExerciseLinks { get; set; } = false;

    /// <summary>
    /// Last time link generation was run for this chunk
    /// </summary>
    public DateTime? LastLinkGenerationAt { get; set; }

    // === Status & Metadata ===

    /// <summary>
    /// Processing status: "pending", "chunked", "embedded", "failed"
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Error message if chunking/embedding failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // === Navigation Properties ===

    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
