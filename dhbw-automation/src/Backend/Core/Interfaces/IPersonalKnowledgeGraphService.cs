using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for managing the user's personal knowledge graph.
/// Handles knowledge nodes, edges with time-decay, and mastery tracking.
/// </summary>
public interface IPersonalKnowledgeGraphService
{
    #region Node Management

    /// <summary>
    /// Gets or creates a knowledge node for the given topic.
    /// </summary>
    Task<UserKnowledgeNode> GetOrCreateNodeAsync(int userId, string subject, string topic, string? subtopic = null);

    /// <summary>
    /// Gets a specific node by ID.
    /// </summary>
    Task<UserKnowledgeNode?> GetNodeAsync(int nodeId);

    /// <summary>
    /// Gets all knowledge nodes for a user, optionally filtered by subject.
    /// </summary>
    Task<List<UserKnowledgeNode>> GetUserNodesAsync(int userId, string? subject = null);

    /// <summary>
    /// Updates mastery level after an exercise is completed.
    /// </summary>
    Task<UserKnowledgeNode> UpdateMasteryAsync(int nodeId, bool isCorrect, string difficulty, double? responseTimeSeconds = null);

    /// <summary>
    /// Gets nodes below a mastery threshold (weak areas).
    /// </summary>
    Task<List<UserKnowledgeNode>> GetWeakNodesAsync(int userId, double threshold = 0.4);

    /// <summary>
    /// Gets nodes with low effective strength (need reinforcement).
    /// </summary>
    Task<List<UserKnowledgeNode>> GetFadingNodesAsync(int userId, double threshold = 0.5);

    #endregion

    #region Edge Management

    /// <summary>
    /// Creates or reinforces an edge between two nodes.
    /// </summary>
    Task<UserKnowledgeEdge> CreateOrReinforceEdgeAsync(int userId, int sourceNodeId, int targetNodeId, string edgeType = "related");

    /// <summary>
    /// Weakens an edge (e.g., after incorrect answer on related topic).
    /// </summary>
    Task WeakenEdgeAsync(int edgeId, double amount = 0.1);

    /// <summary>
    /// Gets all edges for a node (both incoming and outgoing).
    /// </summary>
    Task<List<UserKnowledgeEdge>> GetNodeEdgesAsync(int nodeId);

    /// <summary>
    /// Gets edges with low current strength (fading connections).
    /// </summary>
    Task<List<UserKnowledgeEdge>> GetFadingEdgesAsync(int userId, double threshold = 0.3);

    #endregion

    #region Decay Operations

    /// <summary>
    /// Applies time decay to all nodes and edges for a user.
    /// Called periodically by background service.
    /// </summary>
    Task ApplyTimeDecayAsync(int userId);

    /// <summary>
    /// Calculates effective strength with decay applied.
    /// </summary>
    double CalculateEffectiveStrength(double baseStrength, double decayRate, DateTime lastInteraction);

    /// <summary>
    /// Gets the personal decay rate for a user+subject (adaptive decay).
    /// </summary>
    Task<double> GetPersonalDecayRateAsync(int userId, string subject);

    #endregion

    #region Graph Operations

    /// <summary>
    /// Gets the complete knowledge graph for visualization.
    /// </summary>
    Task<PersonalKnowledgeGraphDto> GetUserGraphAsync(int userId);

    /// <summary>
    /// Generates semantic edges based on topic similarity.
    /// </summary>
    Task<int> GenerateSemanticEdgesAsync(int userId, double similarityThreshold = 0.7);

    /// <summary>
    /// Records an exercise result and updates the graph accordingly.
    /// </summary>
    Task<ExerciseImpact> RecordExerciseResultAsync(int userId, int nodeId, bool isCorrect, string difficulty, double? responseTimeSeconds = null);

    #endregion
}

/// <summary>
/// DTO for the complete knowledge graph visualization.
/// </summary>
public class PersonalKnowledgeGraphDto
{
    public int UserId { get; set; }
    public List<KnowledgeNodeDto> Nodes { get; set; } = new();
    public List<KnowledgeEdgeDto> Edges { get; set; } = new();
    public GraphStatistics Statistics { get; set; } = new();
}

public class KnowledgeNodeDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Subtopic { get; set; }
    public double MasteryLevel { get; set; }
    public double EffectiveStrength { get; set; }
    public DateTime LastInteraction { get; set; }
    public int TotalExercises { get; set; }
    public int CorrectExercises { get; set; }
    public bool IsWeak { get; set; }
    public bool IsFading { get; set; }
}

public class KnowledgeEdgeDto
{
    public int Id { get; set; }
    public int SourceNodeId { get; set; }
    public int TargetNodeId { get; set; }
    public string EdgeType { get; set; } = string.Empty;
    public double CurrentStrength { get; set; }
    public bool IsFading { get; set; }
}

public class GraphStatistics
{
    public int TotalNodes { get; set; }
    public int TotalEdges { get; set; }
    public double AverageMastery { get; set; }
    public double AverageStrength { get; set; }
    public int WeakNodes { get; set; }
    public int FadingEdges { get; set; }
    public int SubjectsCount { get; set; }
}

/// <summary>
/// Result of recording an exercise, showing impact on the graph.
/// </summary>
public class ExerciseImpact
{
    public int NodeId { get; set; }
    public double PreviousMastery { get; set; }
    public double NewMastery { get; set; }
    public double MasteryChange { get; set; }
    public double PreviousStrength { get; set; }
    public double NewStrength { get; set; }
    public double StrengthChange { get; set; }
    public int EdgesReinforced { get; set; }
    public int EdgesWeakened { get; set; }
    public double StreakMultiplier { get; set; }
    public string Message { get; set; } = string.Empty;
}
