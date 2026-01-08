using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Core.Models;

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

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
