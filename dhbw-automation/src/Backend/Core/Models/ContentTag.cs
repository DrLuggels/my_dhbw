using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// User-defined tags for content categorization
/// </summary>
[Table("content_tags")]
public class ContentTag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// User who created this tag
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Tag name (e.g., "Important", "Exam-Relevant", "Review-Needed")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tag color in hex format (e.g., "#FF5733")
    /// </summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// Icon name (for UI display)
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Optional description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this is a system/default tag
    /// </summary>
    public bool IsSystem { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<ContentTagAssignment> Assignments { get; set; } = new List<ContentTagAssignment>();
}
