using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Represents a knowledge node in the user's personal knowledge graph.
/// Tracks mastery level and implements time-decay for spaced repetition.
/// </summary>
[Table("user_knowledge_nodes")]
public class UserKnowledgeNode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    // Topic identification
    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Subtopic { get; set; }

    // Mastery tracking (0.0 to 1.0)
    public double MasteryLevel { get; set; } = 0.0;

    // Time-decay parameters
    public double DecayRate { get; set; } = 0.05; // Default: 5% per day
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
    public double BaseStrength { get; set; } = 1.0;

    /// <summary>
    /// Computed effective strength with exponential decay applied.
    /// Formula: strength(t) = BaseStrength * e^(-DecayRate * t)
    /// </summary>
    [NotMapped]
    public double EffectiveStrength
    {
        get
        {
            var daysSinceInteraction = (DateTime.UtcNow - LastInteraction).TotalDays;
            return BaseStrength * Math.Exp(-DecayRate * daysSinceInteraction);
        }
    }

    // Exercise statistics - overall
    public int TotalExercises { get; set; } = 0;
    public int CorrectExercises { get; set; } = 0;
    public double AverageResponseTimeSeconds { get; set; } = 0.0;

    // Exercise statistics - per difficulty (for 20/40/40 distribution tracking)
    public int EasyCorrect { get; set; } = 0;
    public int EasyTotal { get; set; } = 0;
    public int MediumCorrect { get; set; } = 0;
    public int MediumTotal { get; set; } = 0;
    public int HardCorrect { get; set; } = 0;
    public int HardTotal { get; set; } = 0;

    /// <summary>
    /// Success rate per difficulty level
    /// </summary>
    [NotMapped]
    public double EasySuccessRate => EasyTotal > 0 ? (double)EasyCorrect / EasyTotal : 0.0;
    [NotMapped]
    public double MediumSuccessRate => MediumTotal > 0 ? (double)MediumCorrect / MediumTotal : 0.0;
    [NotMapped]
    public double HardSuccessRate => HardTotal > 0 ? (double)HardCorrect / HardTotal : 0.0;

    // Embedding reference for semantic search
    public bool HasEmbedding { get; set; } = false;
    [MaxLength(100)]
    public string? QdrantPointId { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    // Related edges
    public virtual ICollection<UserKnowledgeEdge> OutgoingEdges { get; set; } = new List<UserKnowledgeEdge>();
    public virtual ICollection<UserKnowledgeEdge> IncomingEdges { get; set; } = new List<UserKnowledgeEdge>();
}
