using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.BackgroundServices;

/// <summary>
/// Background service that recalculates learning priorities based on deadlines.
/// Runs every hour to:
/// - Fetch upcoming Moodle assignments
/// - Link topics to assignments via semantic similarity
/// - Recalculate priority scores
/// - Update LearningPriority table
/// </summary>
public class PriorityCalculationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PriorityCalculationBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public PriorityCalculationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PriorityCalculationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Priority Calculation Background Service started (interval: {Interval})", _interval);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPriorityCalculationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Priority Calculation Background Service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Priority Calculation Background Service stopped");
    }

    private async Task ProcessPriorityCalculationsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting priority calculation processing");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var priorityService = scope.ServiceProvider.GetRequiredService<IDeadlinePriorityService>();

        try
        {
            // Get users with upcoming deadlines (within 30 days)
            var cutoffDate = DateTime.UtcNow.AddDays(30);
            var usersWithDeadlines = await context.MoodleAssignments
                .Where(a => !a.IsSubmitted &&
                            a.DueDate.HasValue &&
                            a.DueDate <= cutoffDate &&
                            a.DueDate > DateTime.UtcNow)
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync(stoppingToken);

            _logger.LogInformation("Processing priorities for {Count} users with upcoming deadlines", usersWithDeadlines.Count);

            var totalPrioritiesUpdated = 0;
            var totalLinksCreated = 0;

            foreach (var userId in usersWithDeadlines)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Link topics to assignments (semantic matching)
                    var linksCreated = await priorityService.LinkTopicsToAssignmentsAsync(userId);
                    totalLinksCreated += linksCreated;

                    // Calculate priorities
                    var priorities = await priorityService.CalculatePrioritiesAsync(userId);
                    totalPrioritiesUpdated += priorities.Count;

                    if (priorities.Count > 0)
                    {
                        var topPriority = priorities.FirstOrDefault();
                        _logger.LogDebug(
                            "User {UserId}: Updated {Count} priorities. Top priority: Node {NodeId} (score: {Score:F1})",
                            userId, priorities.Count, topPriority?.UserKnowledgeNodeId, topPriority?.CompositeScore);
                    }

                    // Small delay between users
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing priorities for user {UserId}", userId);
                }
            }

            // Also process users with knowledge nodes but no deadlines (basic priority based on mastery)
            var usersWithNodes = await context.UserKnowledgeNodes
                .Select(n => n.UserId)
                .Distinct()
                .Where(id => !usersWithDeadlines.Contains(id))
                .ToListAsync(stoppingToken);

            foreach (var userId in usersWithNodes)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    var priorities = await priorityService.CalculatePrioritiesAsync(userId);
                    totalPrioritiesUpdated += priorities.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing basic priorities for user {UserId}", userId);
                }
            }

            _logger.LogInformation(
                "Priority calculation completed. Updated {Priorities} priorities, created {Links} topic-assignment links",
                totalPrioritiesUpdated, totalLinksCreated);

            // Clean up old priorities for nodes that no longer exist
            await CleanupOrphanedPrioritiesAsync(context, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in priority calculation processing");
        }
    }

    /// <summary>
    /// Removes priorities for nodes that have been deleted.
    /// </summary>
    private async Task CleanupOrphanedPrioritiesAsync(AppDbContext context, CancellationToken stoppingToken)
    {
        try
        {
            // Find priorities with missing nodes
            var orphanedPriorities = await context.LearningPriorities
                .Where(p => p.UserKnowledgeNodeId.HasValue &&
                            !context.UserKnowledgeNodes.Any(n => n.Id == p.UserKnowledgeNodeId))
                .ToListAsync(stoppingToken);

            if (orphanedPriorities.Count > 0)
            {
                context.LearningPriorities.RemoveRange(orphanedPriorities);
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Cleaned up {Count} orphaned priorities", orphanedPriorities.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned priorities");
        }
    }
}
