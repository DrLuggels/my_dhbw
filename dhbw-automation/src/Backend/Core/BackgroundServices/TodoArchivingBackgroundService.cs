using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.BackgroundServices;

/// <summary>
/// Background service for automatic archiving of completed todos
/// and deletion of old archived todos
/// </summary>
public class TodoArchivingBackgroundService : BackgroundService
{
    private readonly ILogger<TodoArchivingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Configuration
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24); // Run once per day
    private static readonly TimeSpan RunTime = TimeSpan.FromHours(3); // Run at 3:00 AM
    private const int DaysBeforeAutoArchive = 1; // Archive completed todos after 1 day
    private const int DefaultAutoDeleteDays = 30; // Delete archived todos after 30 days

    public TodoArchivingBackgroundService(
        ILogger<TodoArchivingBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Todo Archiving Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate time until next run (3:00 AM)
                var now = DateTime.UtcNow;
                var nextRun = now.Date.Add(RunTime);
                if (nextRun <= now)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delayUntilNextRun = nextRun - now;
                _logger.LogInformation($"Next archiving run scheduled at {nextRun:yyyy-MM-dd HH:mm} UTC");

                await Task.Delay(delayUntilNextRun, stoppingToken);

                // Run archiving process
                await RunArchivingProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Todo Archiving Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Todo Archiving Background Service");
                // Wait before retry
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task RunArchivingProcessAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting todo archiving process");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var archivedCount = 0;
        var deletedCount = 0;

        try
        {
            // 1. Auto-archive completed todos older than DaysBeforeAutoArchive
            var archiveCutoff = DateTime.UtcNow.AddDays(-DaysBeforeAutoArchive);
            var todosToArchive = await context.Todos
                .Where(t => t.Status == "completed"
                    && t.ArchivedAt == null
                    && t.CompletedAt != null
                    && t.CompletedAt < archiveCutoff)
                .ToListAsync(stoppingToken);

            foreach (var todo in todosToArchive)
            {
                todo.ArchivedAt = DateTime.UtcNow;

                // Move to archive list if exists
                var archiveList = await context.TodoLists
                    .FirstOrDefaultAsync(l => l.UserId == todo.UserId && l.IsArchiveList, stoppingToken);

                if (archiveList != null)
                {
                    todo.ListId = archiveList.Id;
                }

                archivedCount++;
            }

            if (archivedCount > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Auto-archived {archivedCount} completed todos");
            }

            // 2. Delete old archived todos based on AutoDeleteAfterDays
            var todosToDelete = await context.Todos
                .Where(t => t.ArchivedAt != null)
                .ToListAsync(stoppingToken);

            var todosActuallyDelete = new List<Todo>();
            foreach (var todo in todosToDelete)
            {
                var deleteAfterDate = todo.ArchivedAt!.Value.AddDays(todo.AutoDeleteAfterDays);
                if (DateTime.UtcNow > deleteAfterDate)
                {
                    todosActuallyDelete.Add(todo);
                }
            }

            if (todosActuallyDelete.Count > 0)
            {
                context.Todos.RemoveRange(todosActuallyDelete);
                await context.SaveChangesAsync(stoppingToken);
                deletedCount = todosActuallyDelete.Count;
                _logger.LogInformation($"Deleted {deletedCount} expired archived todos");
            }

            _logger.LogInformation($"Archiving process completed. Archived: {archivedCount}, Deleted: {deletedCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during archiving process");
            throw;
        }
    }

    /// <summary>
    /// Manually trigger archiving (for testing or admin use)
    /// </summary>
    public async Task TriggerArchivingAsync()
    {
        _logger.LogInformation("Manual archiving triggered");
        await RunArchivingProcessAsync(CancellationToken.None);
    }
}
