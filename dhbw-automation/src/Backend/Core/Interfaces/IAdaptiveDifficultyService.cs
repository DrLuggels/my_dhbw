namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for adaptive difficulty selection following the 20/40/40 rule.
/// Ensures users always learn at their optimal challenge level (~70% success probability).
/// </summary>
public interface IAdaptiveDifficultyService
{
    /// <summary>
    /// Selects the appropriate difficulty for the next exercise.
    /// Considers both the target distribution (20/40/40) and user's current mastery.
    /// </summary>
    Task<DifficultySelection> SelectDifficultyAsync(int userId, int nodeId);

    /// <summary>
    /// Gets the current difficulty distribution for a user.
    /// </summary>
    Task<DifficultyDistribution> GetDistributionAsync(int userId, string? subject = null);

    /// <summary>
    /// Estimates success probability for a given node and difficulty.
    /// </summary>
    Task<double> EstimateSuccessProbabilityAsync(int nodeId, string difficulty);

    /// <summary>
    /// Records an exercise result to update difficulty statistics.
    /// </summary>
    Task UpdateDifficultyStatsAsync(int nodeId, string difficulty, bool isCorrect, double? responseTimeSeconds = null);

    /// <summary>
    /// Gets the challenge zone difficulty (targets ~70% success rate).
    /// </summary>
    Task<string> GetChallengeZoneDifficultyAsync(int nodeId);

    /// <summary>
    /// Checks if difficulty distribution needs rebalancing.
    /// </summary>
    Task<bool> NeedsRebalancingAsync(int userId, string? subject = null, double threshold = 0.1);
}

/// <summary>
/// Result of difficulty selection with reasoning.
/// </summary>
public class DifficultySelection
{
    public string Difficulty { get; set; } = "medium";
    public string Reason { get; set; } = string.Empty;
    public double EstimatedSuccessProbability { get; set; }
    public bool IsForRebalancing { get; set; }
    public DifficultyDistribution CurrentDistribution { get; set; } = new();
}

/// <summary>
/// Distribution of exercises across difficulty levels.
/// </summary>
public class DifficultyDistribution
{
    public int EasyCount { get; set; }
    public int MediumCount { get; set; }
    public int HardCount { get; set; }

    public int TotalCount => EasyCount + MediumCount + HardCount;

    public double EasyPercentage => TotalCount > 0 ? (double)EasyCount / TotalCount : 0.0;
    public double MediumPercentage => TotalCount > 0 ? (double)MediumCount / TotalCount : 0.0;
    public double HardPercentage => TotalCount > 0 ? (double)HardCount / TotalCount : 0.0;

    // Target distribution: 20% easy, 40% medium, 40% hard
    public double EasyDeviation => 0.20 - EasyPercentage;
    public double MediumDeviation => 0.40 - MediumPercentage;
    public double HardDeviation => 0.40 - HardPercentage;

    public double MaxDeviation => Math.Max(Math.Abs(EasyDeviation),
        Math.Max(Math.Abs(MediumDeviation), Math.Abs(HardDeviation)));

    /// <summary>
    /// Gets the difficulty that needs most balancing (highest positive deviation).
    /// </summary>
    public string? GetUnderrepresentedDifficulty()
    {
        if (EasyDeviation > MediumDeviation && EasyDeviation > HardDeviation && EasyDeviation > 0)
            return "easy";
        if (MediumDeviation > EasyDeviation && MediumDeviation > HardDeviation && MediumDeviation > 0)
            return "medium";
        if (HardDeviation > 0)
            return "hard";
        return null;
    }
}
