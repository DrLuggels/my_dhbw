using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Unified Learning Priority - extends LearningPriority with:
/// - Reference to UnifiedKnowledgeEntity
/// - Target Bloom level and Bloom gap tracking
/// - Prerequisite blocking awareness
/// - Enhanced priority calculation
/// </summary>
[Table("unified_learning_priorities")]
public class UnifiedLearningPriority
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Reference to the unified knowledge entity
    /// </summary>
    public int? UnifiedEntityId { get; set; }

    /// <summary>
    /// Reference to related Moodle assignment (if deadline-driven)
    /// </summary>
    public int? MoodleAssignmentId { get; set; }

    /// <summary>
    /// Reference to related calendar event (if exam-driven)
    /// </summary>
    public int? CalendarEventId { get; set; }

    #region Priority Score Components (0-100 each)

    /// <summary>
    /// Urgency based on deadline proximity.
    /// Formula: max(0, 100 × (1 - daysUntilDeadline / 30))
    /// </summary>
    public double DeadlineUrgency { get; set; } = 0.0;

    /// <summary>
    /// Relevance of topic to upcoming assignment/exam.
    /// Based on semantic similarity (0-100).
    /// </summary>
    public double TopicRelevance { get; set; } = 0.0;

    /// <summary>
    /// Gap in mastery level.
    /// Formula: (1 - EffectiveKnowledge) × 100
    /// </summary>
    public double MasteryGap { get; set; } = 0.0;

    /// <summary>
    /// Amount of decay since last interaction.
    /// Formula: (1 - DecayFactor) × 100
    /// </summary>
    public double DecayAmount { get; set; } = 0.0;

    /// <summary>
    /// Weighted composite priority score.
    /// Formula: w1×DeadlineUrgency + w2×TopicRelevance + w3×MasteryGap + w4×DecayAmount + w5×BloomGap
    /// </summary>
    public double CompositeScore { get; set; } = 0.0;

    #endregion

    #region Bloom's Taxonomy Enhancement

    /// <summary>
    /// Current Bloom level of the entity
    /// </summary>
    public int CurrentBloomLevel { get; set; } = 1;

    /// <summary>
    /// Target Bloom level to achieve (based on upcoming assessment requirements)
    /// </summary>
    public int TargetBloomLevel { get; set; } = 3;

    /// <summary>
    /// Bloom level gap.
    /// Formula: (TargetBloomLevel - CurrentBloomLevel) / 5.0 × 100
    /// </summary>
    public double BloomGap { get; set; } = 0.0;

    #endregion

    #region Prerequisite Blocking

    /// <summary>
    /// Whether this priority is blocked by unmet prerequisites
    /// </summary>
    public bool IsBlocked { get; set; } = false;

    /// <summary>
    /// Human-readable reason why this priority is blocked
    /// </summary>
    [MaxLength(500)]
    public string? BlockReason { get; set; }

    /// <summary>
    /// JSON list of blocking prerequisite entity IDs and their status
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? BlockingPrerequisitesJson { get; set; }

    /// <summary>
    /// Get blocking prerequisites as list
    /// </summary>
    [NotMapped]
    public List<BlockingPrerequisiteInfo> BlockingPrerequisites
    {
        get
        {
            if (string.IsNullOrEmpty(BlockingPrerequisitesJson))
                return new List<BlockingPrerequisiteInfo>();
            try
            {
                return JsonSerializer.Deserialize<List<BlockingPrerequisiteInfo>>(BlockingPrerequisitesJson)
                       ?? new List<BlockingPrerequisiteInfo>();
            }
            catch
            {
                return new List<BlockingPrerequisiteInfo>();
            }
        }
        set
        {
            BlockingPrerequisitesJson = JsonSerializer.Serialize(value);
        }
    }

    /// <summary>
    /// Number of blocking prerequisites
    /// </summary>
    [NotMapped]
    public int BlockingCount => BlockingPrerequisites.Count;

    #endregion

    #region Context Information

    /// <summary>
    /// Subject area
    /// </summary>
    [MaxLength(100)]
    public string? Subject { get; set; }

    /// <summary>
    /// Topic within subject
    /// </summary>
    [MaxLength(200)]
    public string? Topic { get; set; }

    /// <summary>
    /// Entity name for display
    /// </summary>
    [MaxLength(255)]
    public string? EntityName { get; set; }

    /// <summary>
    /// Target deadline (if applicable)
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Days until deadline
    /// </summary>
    [NotMapped]
    public int? DaysUntilDeadline => Deadline.HasValue
        ? (int)Math.Max(0, (Deadline.Value - DateTime.UtcNow).TotalDays)
        : null;

    /// <summary>
    /// Related assignment/exam name for display
    /// </summary>
    [MaxLength(255)]
    public string? RelatedEventName { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// When this priority was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this priority is still active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority rank within user's list (1 = highest priority)
    /// </summary>
    public int? Rank { get; set; }

    #endregion

    #region Navigation Properties

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("UnifiedEntityId")]
    public virtual UnifiedKnowledgeEntity? UnifiedEntity { get; set; }

    [ForeignKey("MoodleAssignmentId")]
    public virtual MoodleAssignment? MoodleAssignment { get; set; }

    [ForeignKey("CalendarEventId")]
    public virtual CalendarEvent? CalendarEvent { get; set; }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calculate the composite score with all components
    /// </summary>
    public void CalculateCompositeScore()
    {
        CompositeScore = UnifiedPriorityWeights.Calculate(
            DeadlineUrgency,
            TopicRelevance,
            MasteryGap,
            DecayAmount,
            BloomGap
        );

        // Reduce score if blocked (but don't eliminate - user might want to work on it anyway)
        if (IsBlocked)
        {
            CompositeScore *= 0.5;
        }
    }

    /// <summary>
    /// Get urgency level as string
    /// </summary>
    [NotMapped]
    public string UrgencyLevel
    {
        get
        {
            if (!Deadline.HasValue) return "none";
            var days = DaysUntilDeadline ?? 0;
            return days switch
            {
                <= 1 => "critical",
                <= 3 => "high",
                <= 7 => "medium",
                <= 14 => "low",
                _ => "none"
            };
        }
    }

    /// <summary>
    /// Get recommended action based on priority state
    /// </summary>
    [NotMapped]
    public string RecommendedAction
    {
        get
        {
            if (IsBlocked)
                return $"Erst Voraussetzungen erfüllen: {BlockingCount} Themen fehlen";
            if (BloomGap > 40)
                return $"Bloom-Level von {CurrentBloomLevel} auf {TargetBloomLevel} steigern";
            if (DecayAmount > 50)
                return "Wiederholung empfohlen - Wissen verblasst";
            if (MasteryGap > 70)
                return "Grundlagen erlernen";
            if (DeadlineUrgency > 80)
                return "Dringend - Deadline naht!";
            return "Weiter üben für bessere Beherrschung";
        }
    }

    #endregion
}

/// <summary>
/// Information about a blocking prerequisite
/// </summary>
public class BlockingPrerequisiteInfo
{
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public double CurrentMastery { get; set; }
    public double RequiredMastery { get; set; }
    public double MasteryGap => RequiredMastery - CurrentMastery;
    public bool IsStrict { get; set; }
}

/// <summary>
/// Unified priority weight configuration (extended with Bloom)
/// </summary>
public static class UnifiedPriorityWeights
{
    public const double DeadlineUrgency = 0.30;
    public const double TopicRelevance = 0.20;
    public const double MasteryGap = 0.25;
    public const double DecayAmount = 0.15;
    public const double BloomGap = 0.10;

    public static double Calculate(double urgency, double relevance, double masteryGap, double decay, double bloomGap)
    {
        return DeadlineUrgency * urgency +
               TopicRelevance * relevance +
               MasteryGap * masteryGap +
               DecayAmount * decay +
               BloomGap * bloomGap;
    }

    /// <summary>
    /// Calculate with custom weights
    /// </summary>
    public static double CalculateCustom(
        double urgency, double relevance, double masteryGap, double decay, double bloomGap,
        double wUrgency, double wRelevance, double wMastery, double wDecay, double wBloom)
    {
        var total = wUrgency + wRelevance + wMastery + wDecay + wBloom;
        if (total == 0) total = 1;

        return (wUrgency * urgency +
                wRelevance * relevance +
                wMastery * masteryGap +
                wDecay * decay +
                wBloom * bloomGap) / total * 100;
    }
}
