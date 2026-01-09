using System.ComponentModel.DataAnnotations;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Represents a fundamental knowledge topic that should be periodically reviewed
/// Independent of learning deficits - tracks baseline knowledge retention
/// </summary>
public class KnowledgeBaseItem
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Subtopic { get; set; }

    /// <summary>
    /// Category: "grundlagen" (basics), "advanced", "important"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "grundlagen";

    /// <summary>
    /// Importance level: "low", "medium", "high", "critical"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Importance { get; set; } = "medium";

    /// <summary>
    /// When this topic was last tested
    /// </summary>
    public DateTime LastTestedDate { get; set; }

    /// <summary>
    /// How many times this topic has been tested
    /// </summary>
    public int TestCount { get; set; } = 0;

    /// <summary>
    /// Average score across all tests (0-100)
    /// </summary>
    public double AverageScore { get; set; } = 0.0;

    /// <summary>
    /// Last score achieved (0-100)
    /// </summary>
    public double LastScore { get; set; } = 0.0;

    /// <summary>
    /// When this item should be reviewed again
    /// Calculated based on performance and spaced repetition
    /// </summary>
    public DateTime NextReviewDate { get; set; }

    /// <summary>
    /// Optional: User notes about this topic
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Is this topic currently active for review?
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual User? User { get; set; }
}
