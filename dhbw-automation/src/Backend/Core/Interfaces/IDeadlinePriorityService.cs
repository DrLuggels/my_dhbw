using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for deadline-aware learning priority calculation.
/// Combines urgency, topic relevance, mastery gaps, and decay to prioritize learning.
/// </summary>
public interface IDeadlinePriorityService
{
    /// <summary>
    /// Calculates priorities for all knowledge nodes based on upcoming deadlines.
    /// </summary>
    Task<List<LearningPriority>> CalculatePrioritiesAsync(int userId);

    /// <summary>
    /// Links knowledge topics to Moodle assignments via semantic similarity.
    /// </summary>
    Task<int> LinkTopicsToAssignmentsAsync(int userId);

    /// <summary>
    /// Gets the top N recommended topics for learning, ordered by priority.
    /// </summary>
    Task<List<LearningRecommendation>> GetRecommendedTopicsAsync(int userId, int topN = 5);

    /// <summary>
    /// Calculates deadline urgency score (0-100).
    /// Higher score = more urgent.
    /// </summary>
    double CalculateUrgencyScore(DateTime? deadline);

    /// <summary>
    /// Gets upcoming deadlines with associated topics.
    /// </summary>
    Task<List<DeadlineWithTopics>> GetUpcomingDeadlinesAsync(int userId, int days = 30);

    /// <summary>
    /// Forces recalculation of all priorities for a user.
    /// </summary>
    Task RefreshPrioritiesAsync(int userId);

    /// <summary>
    /// Gets priority details for a specific node.
    /// </summary>
    Task<LearningPriority?> GetNodePriorityAsync(int userId, int nodeId);
}

/// <summary>
/// A learning topic recommendation with priority scoring.
/// </summary>
public class LearningRecommendation
{
    public int NodeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Subtopic { get; set; }
    public double PriorityScore { get; set; }
    public double MasteryLevel { get; set; }
    public double EffectiveStrength { get; set; }
    public DateTime? NearestDeadline { get; set; }
    public string? RelatedAssignment { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;

    // Component scores for transparency
    public double UrgencyScore { get; set; }
    public double RelevanceScore { get; set; }
    public double MasteryGapScore { get; set; }
    public double DecayScore { get; set; }
}

/// <summary>
/// A deadline with linked learning topics.
/// </summary>
public class DeadlineWithTopics
{
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string? CourseName { get; set; }
    public DateTime Deadline { get; set; }
    public int DaysUntilDeadline { get; set; }
    public bool IsSubmitted { get; set; }
    public double UrgencyScore { get; set; }
    public List<LinkedTopic> LinkedTopics { get; set; } = new();
}

/// <summary>
/// A knowledge topic linked to a deadline.
/// </summary>
public class LinkedTopic
{
    public int NodeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public double SemanticSimilarity { get; set; }
    public double MasteryLevel { get; set; }
    public bool NeedsAttention { get; set; }
}
