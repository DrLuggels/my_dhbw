using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("documents")]
public class Document
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(200)]
    public string? Subject { get; set; }

    [Column(TypeName = "TEXT")]
    public string? Summary { get; set; }

    [Column(TypeName = "TEXT")]
    public string? ExtractedText { get; set; }

    [Column(TypeName = "JSON")]
    public string? Tags { get; set; }

    [Column(TypeName = "JSON")]
    public string? Metadata { get; set; }

    public bool IsProcessed { get; set; } = false;

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(50)]
    public string Source { get; set; } = "manual_upload";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // NEW: Enhanced Fields for AI System
    public DocumentCategory DocumentCategory { get; set; } = DocumentCategory.Sonstiges;

    public bool IsTemporary { get; set; } = false; // Wird als Backup archiviert?
    public bool IsArchived { get; set; } = false; // In Backup-Bucket verschoben
    public DateTime? ArchivedAt { get; set; }

    // AI-Analysis Results
    [Column(TypeName = "JSON")]
    public string? DetectedErrors { get; set; } // JSON Array von Fehlern

    [Column(TypeName = "TEXT")]
    public string? CorrectedText { get; set; } // Korrigierte Version (automatisch erstellt)

    public int? ErrorCount { get; set; }

    // Relations
    public int? RelatedProjectId { get; set; }

    // === Embedding & Knowledge Network ===

    /// <summary>
    /// Whether this document has a vector embedding
    /// </summary>
    public bool HasEmbedding { get; set; } = false;

    /// <summary>
    /// Qdrant point ID for semantic search
    /// </summary>
    [MaxLength(100)]
    public string? QdrantPointId { get; set; }

    /// <summary>
    /// Number of images extracted from this document
    /// </summary>
    public int ImageCount { get; set; } = 0;

    /// <summary>
    /// Whether images have been extracted and processed
    /// </summary>
    public bool ImagesProcessed { get; set; } = false;

    // === Chunking ===

    /// <summary>
    /// Number of semantic chunks extracted from this document
    /// </summary>
    public int ChunkCount { get; set; } = 0;

    /// <summary>
    /// Whether this document has been chunked
    /// </summary>
    public bool IsChunked { get; set; } = false;

    /// <summary>
    /// When the document was chunked
    /// </summary>
    public DateTime? ChunkedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RelatedProjectId")]
    public virtual Project? RelatedProject { get; set; }

    public virtual ICollection<DocumentImage> Images { get; set; } = new List<DocumentImage>();

    public virtual ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
