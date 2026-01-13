using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for adaptive difficulty selection following the 20/40/40 rule.
/// Targets ~70% success probability to keep users in their optimal challenge zone.
/// </summary>
public class AdaptiveDifficultyService : IAdaptiveDifficultyService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdaptiveDifficultyService> _logger;

    // Target distribution constants
    private const double TargetEasy = 0.20;
    private const double TargetMedium = 0.40;
    private const double TargetHard = 0.40;

    // Rebalancing threshold - if any difficulty deviates more than this, force rebalance
    private const double RebalanceThreshold = 0.10;

    // Target success probability for challenge zone
    private const double TargetSuccessProbability = 0.70;
    private const double SuccessProbabilityTolerance = 0.15;

    public AdaptiveDifficultyService(AppDbContext dbContext, ILogger<AdaptiveDifficultyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DifficultySelection> SelectDifficultyAsync(int userId, int nodeId)
    {
        var node = await _dbContext.UserKnowledgeNodes.FindAsync(nodeId);
        if (node == null)
        {
            return new DifficultySelection
            {
                Difficulty = "medium",
                Reason = "Node not found, defaulting to medium",
                EstimatedSuccessProbability = 0.5
            };
        }

        // Get current distribution for the user's subject
        var distribution = await GetDistributionAsync(userId, node.Subject);

        // Step 1: Check if we need to rebalance (>10% deviation)
        if (distribution.MaxDeviation > RebalanceThreshold)
        {
            var underrepresented = distribution.GetUnderrepresentedDifficulty();
            if (underrepresented != null)
            {
                var successProb = await EstimateSuccessProbabilityAsync(nodeId, underrepresented);
                return new DifficultySelection
                {
                    Difficulty = underrepresented,
                    Reason = $"Rebalancing: {underrepresented} is underrepresented by {GetDeviation(distribution, underrepresented):P0}",
                    EstimatedSuccessProbability = successProb,
                    IsForRebalancing = true,
                    CurrentDistribution = distribution
                };
            }
        }

        // Step 2: Select difficulty based on challenge zone (~70% success probability)
        var challengeDifficulty = await GetChallengeZoneDifficultyAsync(nodeId);
        var challengeSuccessProb = await EstimateSuccessProbabilityAsync(nodeId, challengeDifficulty);

        return new DifficultySelection
        {
            Difficulty = challengeDifficulty,
            Reason = $"Challenge zone: targeting ~{TargetSuccessProbability:P0} success rate",
            EstimatedSuccessProbability = challengeSuccessProb,
            IsForRebalancing = false,
            CurrentDistribution = distribution
        };
    }

    /// <inheritdoc />
    public async Task<DifficultyDistribution> GetDistributionAsync(int userId, string? subject = null)
    {
        var query = _dbContext.UserKnowledgeNodes
            .Where(n => n.UserId == userId);

        if (!string.IsNullOrEmpty(subject))
        {
            query = query.Where(n => n.Subject == subject);
        }

        var nodes = await query.ToListAsync();

        return new DifficultyDistribution
        {
            EasyCount = nodes.Sum(n => n.EasyTotal),
            MediumCount = nodes.Sum(n => n.MediumTotal),
            HardCount = nodes.Sum(n => n.HardTotal)
        };
    }

    /// <inheritdoc />
    public async Task<double> EstimateSuccessProbabilityAsync(int nodeId, string difficulty)
    {
        var node = await _dbContext.UserKnowledgeNodes.FindAsync(nodeId);
        if (node == null)
            return 0.5;

        // Calculate success rate from historical data
        var (correct, total) = difficulty.ToLower() switch
        {
            "easy" => (node.EasyCorrect, node.EasyTotal),
            "medium" => (node.MediumCorrect, node.MediumTotal),
            "hard" => (node.HardCorrect, node.HardTotal),
            _ => (node.MediumCorrect, node.MediumTotal)
        };

        // Not enough data - estimate based on mastery level and effective strength
        if (total < 3)
        {
            return EstimateFromMastery(node, difficulty);
        }

        // Use historical success rate with Bayesian smoothing
        // Add prior of 2 correct, 4 total (50% baseline)
        var smoothedRate = (correct + 2.0) / (total + 4.0);

        // Factor in time decay - lower effective strength = lower success probability
        var decayFactor = node.EffectiveStrength;
        return smoothedRate * (0.7 + 0.3 * decayFactor);
    }

    /// <inheritdoc />
    public async Task UpdateDifficultyStatsAsync(int nodeId, string difficulty, bool isCorrect, double? responseTimeSeconds = null)
    {
        var node = await _dbContext.UserKnowledgeNodes.FindAsync(nodeId);
        if (node == null)
            return;

        switch (difficulty.ToLower())
        {
            case "easy":
                node.EasyTotal++;
                if (isCorrect) node.EasyCorrect++;
                break;
            case "medium":
                node.MediumTotal++;
                if (isCorrect) node.MediumCorrect++;
                break;
            case "hard":
                node.HardTotal++;
                if (isCorrect) node.HardCorrect++;
                break;
        }

        // Update response time tracking if provided
        if (responseTimeSeconds.HasValue)
        {
            UpdateResponseTimeStats(node, difficulty, responseTimeSeconds.Value, isCorrect);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogDebug(
            "Updated difficulty stats for node {NodeId}: {Difficulty} {Result}",
            nodeId, difficulty, isCorrect ? "correct" : "incorrect");
    }

    /// <inheritdoc />
    public async Task<string> GetChallengeZoneDifficultyAsync(int nodeId)
    {
        var node = await _dbContext.UserKnowledgeNodes.FindAsync(nodeId);
        if (node == null)
            return "medium";

        // Estimate success probability for each difficulty
        var easyProb = await EstimateSuccessProbabilityAsync(nodeId, "easy");
        var mediumProb = await EstimateSuccessProbabilityAsync(nodeId, "medium");
        var hardProb = await EstimateSuccessProbabilityAsync(nodeId, "hard");

        // Find difficulty closest to 70% success rate
        var targetLow = TargetSuccessProbability - SuccessProbabilityTolerance;
        var targetHigh = TargetSuccessProbability + SuccessProbabilityTolerance;

        // Prefer harder difficulty when in the challenge zone
        if (hardProb >= targetLow && hardProb <= targetHigh)
            return "hard";
        if (mediumProb >= targetLow && mediumProb <= targetHigh)
            return "medium";
        if (easyProb >= targetLow && easyProb <= targetHigh)
            return "easy";

        // If no difficulty is in the zone, select based on mastery
        if (node.MasteryLevel < 0.4)
            return "easy";
        if (node.MasteryLevel < 0.7)
            return "medium";
        return "hard";
    }

    /// <inheritdoc />
    public async Task<bool> NeedsRebalancingAsync(int userId, string? subject = null, double threshold = 0.1)
    {
        var distribution = await GetDistributionAsync(userId, subject);

        // Need at least 10 exercises before considering rebalancing
        if (distribution.TotalCount < 10)
            return false;

        return distribution.MaxDeviation > threshold;
    }

    #region Private Helper Methods

    /// <summary>
    /// Estimates success probability based on mastery level when historical data is insufficient.
    /// </summary>
    private double EstimateFromMastery(UserKnowledgeNode node, string difficulty)
    {
        var baseProbability = node.MasteryLevel * node.EffectiveStrength;

        // Adjust based on difficulty
        return difficulty.ToLower() switch
        {
            "easy" => Math.Min(0.95, baseProbability + 0.3),
            "medium" => Math.Min(0.85, baseProbability + 0.1),
            "hard" => Math.Max(0.1, baseProbability - 0.1),
            _ => baseProbability
        };
    }

    /// <summary>
    /// Gets the deviation for a specific difficulty from the distribution.
    /// </summary>
    private double GetDeviation(DifficultyDistribution distribution, string difficulty)
    {
        return difficulty.ToLower() switch
        {
            "easy" => distribution.EasyDeviation,
            "medium" => distribution.MediumDeviation,
            "hard" => distribution.HardDeviation,
            _ => 0.0
        };
    }

    /// <summary>
    /// Updates response time statistics for performance tracking.
    /// </summary>
    private void UpdateResponseTimeStats(UserKnowledgeNode node, string difficulty, double responseTimeSeconds, bool isCorrect)
    {
        // Response time tracking could be extended here
        // For now, we just log for analysis
        _logger.LogDebug(
            "Response time for {Difficulty} on node {NodeId}: {Time:F1}s ({Result})",
            difficulty, node.Id, responseTimeSeconds, isCorrect ? "correct" : "incorrect");

        // Fast correct answers boost mastery slightly
        if (isCorrect && responseTimeSeconds < 30)
        {
            var bonus = difficulty.ToLower() switch
            {
                "easy" => 0.01,
                "medium" => 0.02,
                "hard" => 0.03,
                _ => 0.01
            };
            node.MasteryLevel = Math.Min(1.0, node.MasteryLevel + bonus);
        }
    }

    #endregion
}
