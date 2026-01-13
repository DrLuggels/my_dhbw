using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for managing prerequisite chains between knowledge topics.
/// Ensures users learn topics in the correct order.
/// </summary>
public interface IPrerequisiteService
{
    /// <summary>
    /// Checks if all prerequisites are met for a knowledge node.
    /// </summary>
    Task<PrerequisiteCheckResult> CheckPrerequisitesAsync(int userId, int nodeId);

    /// <summary>
    /// Gets missing prerequisites for a node.
    /// </summary>
    Task<List<MissingPrerequisite>> GetMissingPrerequisitesAsync(int userId, int nodeId);

    /// <summary>
    /// Creates a prerequisite chain between two nodes.
    /// </summary>
    Task<PrerequisiteChain> CreatePrerequisiteAsync(
        int prerequisiteNodeId,
        int dependentNodeId,
        double requiredMasteryLevel = 0.6,
        bool isStrict = true,
        string? description = null);

    /// <summary>
    /// Removes a prerequisite chain.
    /// </summary>
    Task<bool> RemovePrerequisiteAsync(int chainId);

    /// <summary>
    /// Gets all prerequisites for a node.
    /// </summary>
    Task<List<PrerequisiteChain>> GetPrerequisitesForNodeAsync(int nodeId);

    /// <summary>
    /// Gets all nodes that depend on this node.
    /// </summary>
    Task<List<PrerequisiteChain>> GetDependentsForNodeAsync(int nodeId);

    /// <summary>
    /// Automatically generates prerequisite chains based on semantic similarity.
    /// </summary>
    Task<int> GeneratePrerequisiteChainsAsync(int userId);

    /// <summary>
    /// Gets the next unlocked topic for a subject.
    /// </summary>
    Task<UserKnowledgeNode?> GetNextUnlockedTopicAsync(int userId, string? subject = null);

    /// <summary>
    /// Gets all unlocked topics for a user.
    /// </summary>
    Task<List<UserKnowledgeNode>> GetUnlockedTopicsAsync(int userId, string? subject = null);
}

// Note: PrerequisiteCheckResult and MissingPrerequisite are defined in Models/PrerequisiteChain.cs
