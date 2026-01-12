using DHBWAutomation.Backend.Core.Configuration;
using DHBWAutomation.Backend.Core.Services;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DHBWAutomation.Backend.Core.BackgroundServices;

/// <summary>
/// Background service for automatic synchronization of JavaDocs exercises
/// from GitHub repository and embedding generation
/// </summary>
public class JavaDocsSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JavaDocsSyncBackgroundService> _logger;
    private readonly JavaDocsSyncOptions _options;

    // Semaphore to prevent concurrent sync operations
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private DateTime _lastSyncTime = DateTime.MinValue;
    private SyncStatus _lastSyncStatus = new();

    public JavaDocsSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<JavaDocsSyncBackgroundService> logger,
        IOptions<JavaDocsSyncOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Get the current sync status for health checks
    /// </summary>
    public SyncStatus GetStatus() => _lastSyncStatus;

    /// <summary>
    /// Manually trigger a sync (called by webhook or admin API)
    /// </summary>
    public async Task<SyncExercisesResult> TriggerSyncAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteSyncWithLockAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("JavaDocs Sync Background Service is disabled");
            return;
        }

        _logger.LogInformation(
            "JavaDocs Sync Background Service started. Interval: {Hours}h, SyncOnStartup: {Startup}",
            _options.SyncIntervalHours, _options.SyncOnStartup);

        // Initial startup delay
        await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

        // Optional: Sync on startup
        if (_options.SyncOnStartup)
        {
            _logger.LogInformation("Performing initial JavaDocs sync on startup...");
            await ExecuteSyncWithRetryAsync(stoppingToken);
        }

        // Periodic sync loop
        var interval = TimeSpan.FromHours(_options.SyncIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "Next JavaDocs sync scheduled in {Hours} hours at {Time}",
                    _options.SyncIntervalHours,
                    DateTime.Now.Add(interval).ToString("yyyy-MM-dd HH:mm:ss"));

                await Task.Delay(interval, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ExecuteSyncWithRetryAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("JavaDocs Sync Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in JavaDocs Sync Background Service");
                // Wait before retry
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("JavaDocs Sync Background Service stopped");
    }

    private async Task ExecuteSyncWithRetryAsync(CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                var result = await ExecuteSyncWithLockAsync(stoppingToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "JavaDocs sync completed successfully. Added: {Added}, Updated: {Updated}, Embeddings: {Embeddings}",
                        result.Added, result.Updated, result.EmbeddingsGenerated);
                    return;
                }
                else
                {
                    _logger.LogWarning("JavaDocs sync failed: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JavaDocs sync attempt {Attempt}/{Max} failed",
                    attempt, _options.MaxRetryAttempts);
            }

            if (attempt < _options.MaxRetryAttempts)
            {
                var delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds * Math.Pow(2, attempt - 1));
                _logger.LogInformation("Retrying in {Seconds} seconds...", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }

        _logger.LogError("JavaDocs sync failed after {Max} attempts", _options.MaxRetryAttempts);
    }

    private async Task<SyncExercisesResult> ExecuteSyncWithLockAsync(CancellationToken stoppingToken)
    {
        // Prevent concurrent syncs
        if (!await _syncLock.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken))
        {
            _logger.LogWarning("JavaDocs sync already in progress, skipping");
            return new SyncExercisesResult { Error = "Sync already in progress" };
        }

        try
        {
            _lastSyncStatus.IsRunning = true;
            _lastSyncStatus.StartedAt = DateTime.UtcNow;

            using var scope = _serviceProvider.CreateScope();
            var scraperService = scope.ServiceProvider.GetRequiredService<IJavaDocsScraperService>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Step 1: Sync exercises from GitHub
            _logger.LogInformation("Starting JavaDocs sync from GitHub...");
            var result = await scraperService.SyncExercisesToDatabaseAsync();

            if (!result.Success)
            {
                _lastSyncStatus.LastError = result.Error;
                return result;
            }

            // Step 2: Process remaining embeddings (beyond the 50 from SyncExercisesToDatabaseAsync)
            var remainingWithoutEmbedding = await context.JavaDocsExercises
                .Where(e => !e.HasEmbedding)
                .CountAsync(stoppingToken);

            if (remainingWithoutEmbedding > 0)
            {
                _logger.LogInformation(
                    "Processing {Count} remaining exercises without embeddings...",
                    remainingWithoutEmbedding);

                var additionalEmbeddings = await ProcessRemainingEmbeddingsAsync(
                    embeddingService, context, stoppingToken);

                result.EmbeddingsGenerated += additionalEmbeddings;
            }

            // Update status
            _lastSyncTime = DateTime.UtcNow;
            _lastSyncStatus.LastSuccessfulSync = _lastSyncTime;
            _lastSyncStatus.LastResult = result;
            _lastSyncStatus.LastError = null;

            return result;
        }
        finally
        {
            _lastSyncStatus.IsRunning = false;
            _lastSyncStatus.CompletedAt = DateTime.UtcNow;
            _syncLock.Release();
        }
    }

    private async Task<int> ProcessRemainingEmbeddingsAsync(
        IEmbeddingService embeddingService,
        AppDbContext context,
        CancellationToken stoppingToken)
    {
        int processed = 0;
        int batchSize = _options.MaxEmbeddingsPerRun;

        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await context.JavaDocsExercises
                .Where(e => !e.HasEmbedding)
                .Take(batchSize)
                .Select(e => e.Id)
                .ToListAsync(stoppingToken);

            if (batch.Count == 0)
                break;

            foreach (var exerciseId in batch)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await embeddingService.ProcessExerciseEmbeddingAsync(exerciseId);
                    processed++;

                    // Rate limiting delay
                    await Task.Delay(_options.EmbeddingDelayMs, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for exercise {Id}", exerciseId);
                }
            }

            _logger.LogInformation("Processed {Count} embeddings in this batch, total: {Total}", batch.Count, processed);
        }

        return processed;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JavaDocs Sync Background Service stopping...");

        // Wait for current sync to complete (with timeout)
        if (_lastSyncStatus.IsRunning)
        {
            _logger.LogInformation("Waiting for current sync to complete...");
            await _syncLock.WaitAsync(TimeSpan.FromSeconds(30), stoppingToken);
            _syncLock.Release();
        }

        await base.StopAsync(stoppingToken);
    }
}

/// <summary>
/// Status information for monitoring
/// </summary>
public class SyncStatus
{
    public bool IsRunning { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public SyncExercisesResult? LastResult { get; set; }
    public string? LastError { get; set; }
}
