using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Represents a calculated learning priority for a knowledge topic.
/// Combines deadline urgency, topic relevance, mastery gap, and decay amount
/// to determine what the user should learn next.
/// </summary>
[Table("learning_priorities")]
public class LearningPriority
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? UserKnowledgeNodeId { get; set; }
    public int? MoodleAssignmentId { get; set; }
    public int? CalendarEventId { get; set; }

    // Priority score components (0-100 each)

    /// <summary>
    /// Urgency based on deadline proximity.
    /// Formula: max(0, 100 * (1 - daysUntilDeadline / 30))
    /// </summary>
    public double DeadlineUrgency { get; set; } = 0.0;

    /// <summary>
    /// Relevance of topic to upcoming assignment/exam.
    /// Based on semantic similarity (0-100).
    /// </summary>
    public double TopicRelevance { get; set; } = 0.0;

    /// <summary>
    /// Gap in mastery level.
    /// Formula: (1 - MasteryLevel) * 100
    /// </summary>
    public double MasteryGap { get; set; } = 0.0;

    /// <summary>
    /// Amount of decay since last interaction.
    /// Formula: (1 - EffectiveStrength) * 100
    /// </summary>
    public double DecayAmount { get; set; } = 0.0;

    /// <summary>
    /// Weighted composite priority score.
    /// Formula: w1*DeadlineUrgency + w2*TopicRelevance + w3*MasteryGap + w4*DecayAmount
    /// Default weights: 0.35, 0.25, 0.25, 0.15
    /// </summary>
    public double CompositeScore { get; set; } = 0.0;

    // Target deadline (if applicable)
    public DateTime? Deadline { get; set; }

    // Calculation metadata
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("UserKnowledgeNodeId")]
    public virtual UserKnowledgeNode? KnowledgeNode { get; set; }

    [ForeignKey("MoodleAssignmentId")]
    public virtual MoodleAssignment? MoodleAssignment { get; set; }

    [ForeignKey("CalendarEventId")]
    public virtual CalendarEvent? CalendarEvent { get; set; }
}

/// <summary>
/// Priority weight configuration
/// </summary>
public static class PriorityWeights
{
    public const double DeadlineUrgency = 0.35;
    public const double TopicRelevance = 0.25;
    public const double MasteryGap = 0.25;
    public const double DecayAmount = 0.15;

    public static double Calculate(double urgency, double relevance, double gap, double decay)
    {
        return DeadlineUrgency * urgency +
               TopicRelevance * relevance +
               MasteryGap * gap +
               DecayAmount * decay;
    }
}
