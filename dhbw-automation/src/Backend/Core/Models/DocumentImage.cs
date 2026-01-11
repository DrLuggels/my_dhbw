using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Images extracted from PDF documents, analyzed by Gemini
/// </summary>
[Table("document_images")]
public class DocumentImage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Source document ID
    /// </summary>
    [Required]
    public int DocumentId { get; set; }

    /// <summary>
    /// Page number in PDF (1-indexed)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Image index on the page (0-indexed)
    /// </summary>
    public int ImageIndex { get; set; }

    /// <summary>
    /// Storage path in MinIO (images bucket)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Original filename or generated name
    /// </summary>
    [MaxLength(255)]
    public string? FileName { get; set; }

    /// <summary>
    /// Image format: "png", "jpg", "gif", etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ImageFormat { get; set; } = "png";

    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// AI-generated description of the image (from Gemini)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? GeminiDescription { get; set; }

    /// <summary>
    /// Text extracted from the image (OCR by Gemini)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Detected objects/elements as JSON
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? DetectedObjects { get; set; }

    /// <summary>
    /// Image type classification: "diagram", "chart", "photo", "screenshot", "table", "formula", "other"
    /// </summary>
    [MaxLength(50)]
    public string? ImageType { get; set; }

    /// <summary>
    /// Relevance score (0-1) for the document content
    /// </summary>
    public double RelevanceScore { get; set; } = 0.5;

    /// <summary>
    /// Whether the image has been processed by AI
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// When the image was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Whether this image has a vector embedding
    /// </summary>
    public bool HasEmbedding { get; set; } = false;

    /// <summary>
    /// Qdrant point ID for the description embedding
    /// </summary>
    [MaxLength(100)]
    public string? QdrantPointId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;
}
