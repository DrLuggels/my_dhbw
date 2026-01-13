using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Represents a connection between two knowledge nodes in the user's personal knowledge graph.
/// Implements time-decay for connection strength - connections fade without reinforcement.
/// </summary>
[Table("user_knowledge_edges")]
public class UserKnowledgeEdge
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int SourceNodeId { get; set; }

    [Required]
    public int TargetNodeId { get; set; }

    // Time-decay connection strength
    public double InitialStrength { get; set; } = 1.0;
    public double DecayRate { get; set; } = 0.03; // Default: 3% per day (slower than nodes)
    public DateTime LastReinforced { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Computed current strength with exponential decay applied.
    /// Formula: strength(t) = InitialStrength * e^(-DecayRate * t)
    /// </summary>
    [NotMapped]
    public double CurrentStrength
    {
        get
        {
            var daysSinceReinforcement = (DateTime.UtcNow - LastReinforced).TotalDays;
            return InitialStrength * Math.Exp(-DecayRate * daysSinceReinforcement);
        }
    }

    /// <summary>
    /// Edge type defining the relationship between nodes.
    /// </summary>
    [MaxLength(50)]
    public string EdgeType { get; set; } = EdgeTypes.Related;

    // Reinforcement tracking
    public int ReinforcementCount { get; set; } = 0;
    public int WeakeningCount { get; set; } = 0;

    // Bidirectional flag
    public bool IsBidirectional { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("SourceNodeId")]
    public virtual UserKnowledgeNode SourceNode { get; set; } = null!;

    [ForeignKey("TargetNodeId")]
    public virtual UserKnowledgeNode TargetNode { get; set; } = null!;
}

/// <summary>
/// Edge type constants for knowledge connections
/// </summary>
public static class EdgeTypes
{
    public const string Related = "related";
    public const string Prerequisite = "prerequisite";
    public const string Extension = "extension";
    public const string Application = "application";
    public const string Example = "example";
    public const string PartOf = "part_of";
}
