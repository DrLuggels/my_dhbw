using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Tracks individual user's forgetting curve per subject.
/// The system learns the optimal decay rate by analyzing performance over time.
/// </summary>
[Table("user_decay_profiles")]
public class UserDecayProfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    // Learned decay parameters
    /// <summary>
    /// The learned decay rate for this user+subject combination.
    /// Starts at default (0.05 = 5%/day) and adjusts based on observed performance.
    /// </summary>
    public double LearnedDecayRate { get; set; } = 0.05;

    /// <summary>
    /// Confidence in the learned decay rate (0-1).
    /// Increases as more data points are collected.
    /// </summary>
    public double DecayConfidence { get; set; } = 0.0;

    /// <summary>
    /// Number of performance data points used for learning.
    /// Minimum 10 needed before adjusting from default.
    /// </summary>
    public int DataPoints { get; set; } = 0;

    /// <summary>
    /// JSON-serialized array of PerformancePoint records.
    /// Used to fit the exponential decay curve.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string PerformanceHistory { get; set; } = "[]";

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// A single performance measurement for decay rate learning.
/// </summary>
public class PerformancePoint
{
    /// <summary>
    /// Days since last interaction with this topic.
    /// </summary>
    public double DaysSinceLastInteraction { get; set; }

    /// <summary>
    /// Expected performance based on current decay model (0-1).
    /// </summary>
    public double ExpectedPerformance { get; set; }

    /// <summary>
    /// Actual performance in the exercise (0-1).
    /// </summary>
    public double ActualPerformance { get; set; }

    /// <summary>
    /// When this measurement was taken.
    /// </summary>
    public DateTime MeasuredAt { get; set; }

    /// <summary>
    /// Topic that was tested.
    /// </summary>
    public string Topic { get; set; } = string.Empty;
}
