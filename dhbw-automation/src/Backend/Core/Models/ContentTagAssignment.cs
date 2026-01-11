using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Many-to-many relationship between tags and content entities
/// </summary>
[Table("content_tag_assignments")]
public class ContentTagAssignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Tag ID
    /// </summary>
    [Required]
    public int TagId { get; set; }

    /// <summary>
    /// Entity type: "document", "exercise", "knowledge_item", "image", etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// When the tag was assigned
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned the tag (null = system/AI)
    /// </summary>
    public int? AssignedByUserId { get; set; }

    /// <summary>
    /// Whether this was auto-assigned by AI
    /// </summary>
    public bool IsAutoAssigned { get; set; } = false;

    // Navigation
    [ForeignKey("TagId")]
    public virtual ContentTag Tag { get; set; } = null!;

    [ForeignKey("AssignedByUserId")]
    public virtual User? AssignedByUser { get; set; }
}
