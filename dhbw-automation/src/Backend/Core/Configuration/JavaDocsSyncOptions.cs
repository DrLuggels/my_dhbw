namespace DHBWAutomation.Backend.Core.Configuration;

/// <summary>
/// Configuration options for JavaDocs sync background service
/// </summary>
public class JavaDocsSyncOptions
{
    public const string SectionName = "JavaDocsSync";

    /// <summary>
    /// Enable/disable the background sync service
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Sync interval in hours (default: 12 hours)
    /// </summary>
    public int SyncIntervalHours { get; set; } = 12;

    /// <summary>
    /// Run initial sync on server startup
    /// </summary>
    public bool SyncOnStartup { get; set; } = true;

    /// <summary>
    /// Delay before initial sync in seconds (allows other services to start)
    /// </summary>
    public int StartupDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Maximum embeddings to process per sync run (rate limiting)
    /// </summary>
    public int MaxEmbeddingsPerRun { get; set; } = 50;

    /// <summary>
    /// Delay between embedding API calls in milliseconds
    /// </summary>
    public int EmbeddingDelayMs { get; set; } = 200;

    /// <summary>
    /// Enable GitHub webhook for instant updates
    /// </summary>
    public bool WebhookEnabled { get; set; } = false;

    /// <summary>
    /// GitHub webhook secret for signature verification
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Maximum retry attempts for failed syncs
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in seconds (exponential backoff applied)
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 60;
}
