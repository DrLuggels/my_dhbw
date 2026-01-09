using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.BackgroundServices;

/// <summary>
/// Background service that generates periodic review exercises for fundamental knowledge
/// Runs daily at 6:00 AM to check for stale knowledge items and generate exercises
/// </summary>
public class PeriodicReviewBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PeriodicReviewBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Daily check
    private static readonly TimeOnly TargetTime = new TimeOnly(6, 0); // 6:00 AM

    public PeriodicReviewBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PeriodicReviewBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Periodic Review Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var targetDateTime = now.Date.Add(TargetTime.ToTimeSpan());

                // If we've already passed today's target time, schedule for tomorrow
                if (now > targetDateTime)
                {
                    targetDateTime = targetDateTime.AddDays(1);
                }

                var delay = targetDateTime - now;

                _logger.LogInformation(
                    $"Next periodic review check scheduled for {targetDateTime:yyyy-MM-dd HH:mm:ss} " +
                    $"(in {delay.TotalHours:F1} hours)");

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessPeriodicReviewsAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Periodic Review Background Service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry in 1 hour on error
            }
        }

        _logger.LogInformation("Periodic Review Background Service stopped");
    }

    private async Task ProcessPeriodicReviewsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting periodic review processing");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var learningService = scope.ServiceProvider.GetRequiredService<ILearningAnalyticsService>();

        try
        {
            // Get all active users
            var activeUserIds = await context.Users
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(stoppingToken);

            _logger.LogInformation($"Processing periodic reviews for {activeUserIds.Count} active users");

            foreach (var userId in activeUserIds)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Check for stale knowledge items (not tested in 30+ days)
                    var staleItems = await learningService.GetStaleKnowledgeItemsAsync(userId, 30);

                    if (staleItems.Count > 0)
                    {
                        _logger.LogInformation(
                            $"User {userId} has {staleItems.Count} stale knowledge items - " +
                            $"generating 5 review exercises");

                        // Generate 5 exercises for the most important/oldest topics
                        var exercises = await learningService.GeneratePeriodicReviewExercisesAsync(userId, 5);

                        _logger.LogInformation(
                            $"Generated {exercises.Count} periodic review exercises for user {userId}");

                        // Optional: Send notification to user
                        // await _notificationService.SendPeriodicReviewNotificationAsync(userId, exercises.Count);
                    }
                    else
                    {
                        _logger.LogInformation($"User {userId} has no stale knowledge items - skipping");
                    }

                    // Small delay between users to avoid overwhelming the system
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing periodic review for user {userId}");
                    // Continue with next user
                }
            }

            _logger.LogInformation("Periodic review processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in periodic review processing");
        }
    }
}
