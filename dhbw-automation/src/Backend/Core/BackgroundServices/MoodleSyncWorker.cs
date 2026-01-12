using DHBWAutomation.Backend.Core.Services;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.BackgroundServices;

/// <summary>
/// Background Worker für die periodische Moodle-Synchronisation
/// </summary>
public class MoodleSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MoodleSyncWorker> _logger;

    // Sync-Intervall: Alle 30 Minuten
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(
        int.TryParse(Environment.GetEnvironmentVariable("MOODLE_POLL_INTERVAL_MINUTES"), out var minutes)
            ? minutes
            : 30
    );

    // Initial Delay: 30 Sekunden nach Start
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(30);

    // Maximale Anzahl an parallelen Syncs
    private const int MaxConcurrentSyncs = 3;

    // Verzögerung zwischen User-Syncs
    private static readonly TimeSpan DelayBetweenUsers = TimeSpan.FromSeconds(5);

    public MoodleSyncWorker(
        IServiceProvider serviceProvider,
        ILogger<MoodleSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Moodle Sync Worker gestartet. Intervall: {Interval} Minuten",
            _syncInterval.TotalMinutes);

        // Warte kurz nach Startup
        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllUsersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Moodle Sync Worker wird beendet");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler im Moodle Sync Worker");
            }

            // Warte bis zum nächsten Sync
            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Moodle Sync Worker beendet");
    }

    private async Task SyncAllUsersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Hole alle User mit aktiviertem Moodle-Sync
        var usersToSync = await context.Users
            .Where(u => u.IsActive && u.MoodleSyncEnabled && u.MoodleToken != null)
            .Select(u => new { u.Id, u.Email, u.MoodleLastSync })
            .ToListAsync(cancellationToken);

        if (!usersToSync.Any())
        {
            _logger.LogDebug("Keine User mit aktiviertem Moodle-Sync gefunden");
            return;
        }

        _logger.LogInformation("Starte Moodle-Sync für {Count} User", usersToSync.Count);

        var syncedCount = 0;
        var errorCount = 0;

        // Synchronisiere User in Batches
        var semaphore = new SemaphoreSlim(MaxConcurrentSyncs);

        var tasks = usersToSync.Select(async user =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await SyncUserAsync(user.Id, user.Email, cancellationToken);
                Interlocked.Increment(ref syncedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Sync für User {UserId} ({Email})",
                    user.Id, user.Email);
                Interlocked.Increment(ref errorCount);
            }
            finally
            {
                semaphore.Release();
            }

            // Kurze Pause zwischen Syncs
            await Task.Delay(DelayBetweenUsers, cancellationToken);
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Moodle-Sync abgeschlossen: {Synced}/{Total} erfolgreich, {Errors} Fehler",
            syncedCount, usersToSync.Count, errorCount);
    }

    private async Task SyncUserAsync(int userId, string email, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starte Moodle-Sync für User {UserId} ({Email})", userId, email);

        using var scope = _serviceProvider.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IMoodleSyncService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // Zuerst Verbindung testen
            var connectionTest = await syncService.TestConnectionAsync(userId);
            if (!connectionTest.Success)
            {
                _logger.LogWarning("Moodle-Verbindung fehlgeschlagen für User {UserId}: {Error}",
                    userId, connectionTest.ErrorMessage);

                // Fehler im User speichern
                var user = await context.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (user != null)
                {
                    user.MoodleLastSyncError = connectionTest.ErrorMessage;
                    user.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);
                }
                return;
            }

            // Vollständige Synchronisation durchführen
            var result = await syncService.FullSyncAsync(userId);

            if (result.Success)
            {
                _logger.LogDebug(
                    "Moodle-Sync erfolgreich für User {UserId}: " +
                    "Kurse: +{CoursesAdded}/~{CoursesUpdated}, " +
                    "Aufgaben: +{AssignmentsAdded}/~{AssignmentsUpdated}, " +
                    "Ressourcen: +{ResourcesAdded}/~{ResourcesUpdated}",
                    userId,
                    result.CoursesResult?.Added ?? 0, result.CoursesResult?.Updated ?? 0,
                    result.AssignmentsResult?.Added ?? 0, result.AssignmentsResult?.Updated ?? 0,
                    result.ResourcesResult?.Added ?? 0, result.ResourcesResult?.Updated ?? 0);
            }
            else
            {
                _logger.LogWarning("Moodle-Sync teilweise fehlgeschlagen für User {UserId}: {Error}",
                    userId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception beim Moodle-Sync für User {UserId}", userId);

            // Fehler speichern
            var user = await context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user != null)
            {
                user.MoodleLastSyncError = ex.Message;
                user.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }

            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moodle Sync Worker wird gestoppt...");
        await base.StopAsync(cancellationToken);
    }
}
