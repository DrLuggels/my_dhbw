using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.BackgroundServices;

/// <summary>
/// Background service that applies time decay to knowledge nodes and edges.
/// Runs every 6 hours to:
/// - Recalculate effective strength for all nodes
/// - Identify nodes/edges below threshold (fading knowledge)
/// - Update learning priorities
/// - Optionally notify users about fading knowledge
/// </summary>
public class KnowledgeDecayBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnowledgeDecayBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    // Thresholds for notifications
    private const double WeakNodeThreshold = 0.4;
    private const double FadingEdgeThreshold = 0.3;

    public KnowledgeDecayBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<KnowledgeDecayBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Knowledge Decay Background Service started (interval: {Interval})", _interval);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDecayForAllUsersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Knowledge Decay Background Service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Knowledge Decay Background Service stopped");
    }

    private async Task ProcessDecayForAllUsersAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting knowledge decay processing");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pkgService = scope.ServiceProvider.GetRequiredService<IPersonalKnowledgeGraphService>();

        try
        {
            // Get all users with knowledge nodes
            var userIds = await context.UserKnowledgeNodes
                .Select(n => n.UserId)
                .Distinct()
                .ToListAsync(stoppingToken);

            _logger.LogInformation("Processing decay for {Count} users with knowledge graphs", userIds.Count);

            var totalWeakNodes = 0;
            var totalFadingEdges = 0;

            foreach (var userId in userIds)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Apply time decay
                    await pkgService.ApplyTimeDecayAsync(userId);

                    // Get weak nodes and fading edges for statistics
                    var weakNodes = await pkgService.GetWeakNodesAsync(userId, WeakNodeThreshold);
                    var fadingEdges = await pkgService.GetFadingEdgesAsync(userId, FadingEdgeThreshold);

                    totalWeakNodes += weakNodes.Count;
                    totalFadingEdges += fadingEdges.Count;

                    if (weakNodes.Count > 0 || fadingEdges.Count > 0)
                    {
                        _logger.LogDebug(
                            "User {UserId}: {WeakNodes} weak nodes, {FadingEdges} fading edges",
                            userId, weakNodes.Count, fadingEdges.Count);
                    }

                    // Small delay between users
                    await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing decay for user {UserId}", userId);
                }
            }

            _logger.LogInformation(
                "Knowledge decay processing completed. Total: {WeakNodes} weak nodes, {FadingEdges} fading edges across {UserCount} users",
                totalWeakNodes, totalFadingEdges, userIds.Count);

            // Update streak freeze regeneration (weekly)
            await RegenerateStreakFreezesAsync(context, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in decay processing");
        }
    }

    /// <summary>
    /// Regenerates streak freezes for users (one per week).
    /// </summary>
    private async Task RegenerateStreakFreezesAsync(AppDbContext context, CancellationToken stoppingToken)
    {
        try
        {
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

            // Find streaks where freeze was used more than a week ago and user has 0 freezes
            var streaksToRegenerate = await context.LearningStreaks
                .Where(s => s.StreakFreezes == 0 &&
                            s.LastFreezeUsed.HasValue &&
                            s.LastFreezeUsed < oneWeekAgo)
                .ToListAsync(stoppingToken);

            foreach (var streak in streaksToRegenerate)
            {
                streak.StreakFreezes = 1;
                streak.UpdatedAt = DateTime.UtcNow;
            }

            if (streaksToRegenerate.Count > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Regenerated streak freezes for {Count} users", streaksToRegenerate.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating streak freezes");
        }
    }
}
