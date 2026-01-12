using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DHBWAutomation.Backend.Core.BackgroundServices;
using DHBWAutomation.Backend.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for handling external webhooks (GitHub, etc.)
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _logger;
    private readonly JavaDocsSyncBackgroundService _syncService;
    private readonly JavaDocsSyncOptions _options;

    public WebhooksController(
        ILogger<WebhooksController> logger,
        JavaDocsSyncBackgroundService syncService,
        IOptions<JavaDocsSyncOptions> options)
    {
        _logger = logger;
        _syncService = syncService;
        _options = options.Value;
    }

    /// <summary>
    /// GitHub webhook endpoint for JavaDocs repository push events
    /// </summary>
    /// <remarks>
    /// Configure this endpoint in your GitHub repository settings:
    /// - URL: https://your-domain/api/webhooks/github/javadocs
    /// - Content type: application/json
    /// - Secret: Same value as WebhookSecret in appsettings.json
    /// - Events: Just the push event
    /// </remarks>
    [HttpPost("github/javadocs")]
    public async Task<IActionResult> HandleGitHubWebhook()
    {
        if (!_options.WebhookEnabled)
        {
            _logger.LogWarning("GitHub webhook received but webhooks are disabled");
            return NotFound(new { error = "Webhooks are disabled" });
        }

        // Read the raw body
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        // Verify signature
        if (!VerifyGitHubSignature(body))
        {
            _logger.LogWarning("GitHub webhook signature verification failed");
            return Unauthorized(new { error = "Invalid signature" });
        }

        // Parse event type
        var eventType = Request.Headers["X-GitHub-Event"].ToString();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].ToString();

        _logger.LogInformation(
            "Received GitHub webhook: Event={EventType}, Delivery={DeliveryId}",
            eventType, deliveryId);

        if (eventType != "push")
        {
            _logger.LogDebug("Ignoring non-push event: {EventType}", eventType);
            return Ok(new { message = $"Event '{eventType}' ignored" });
        }

        try
        {
            // Parse payload to get branch info
            var payload = JsonSerializer.Deserialize<GitHubPushPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Only sync on main/master branch pushes
            var branch = payload?.Ref?.Replace("refs/heads/", "") ?? "";
            if (branch != "main" && branch != "master")
            {
                _logger.LogDebug("Ignoring push to non-main branch: {Branch}", branch);
                return Ok(new { message = $"Branch '{branch}' ignored" });
            }

            _logger.LogInformation(
                "GitHub push event for {Repo} on branch {Branch}. Triggering async sync...",
                payload?.Repository?.FullName ?? "unknown",
                branch);

            // Trigger async sync (don't wait for completion)
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _syncService.TriggerSyncAsync();
                    _logger.LogInformation(
                        "Webhook-triggered sync completed: Added={Added}, Updated={Updated}, Embeddings={Embeddings}",
                        result.Added, result.Updated, result.EmbeddingsGenerated);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Webhook-triggered sync failed");
                }
            });

            return Ok(new
            {
                message = "Sync triggered",
                status = "processing",
                repository = payload?.Repository?.FullName,
                branch = branch
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook");
            return BadRequest(new { error = "Failed to process webhook payload" });
        }
    }

    /// <summary>
    /// Test endpoint to verify webhook configuration
    /// </summary>
    [HttpGet("github/javadocs/status")]
    public IActionResult GetWebhookStatus()
    {
        return Ok(new
        {
            enabled = _options.WebhookEnabled,
            secretConfigured = !string.IsNullOrEmpty(_options.WebhookSecret),
            syncStatus = _syncService.GetStatus()
        });
    }

    private bool VerifyGitHubSignature(string payload)
    {
        if (string.IsNullOrEmpty(_options.WebhookSecret))
        {
            _logger.LogWarning("Webhook secret not configured - accepting webhook without verification (NOT recommended for production!)");
            return true;
        }

        var signature = Request.Headers["X-Hub-Signature-256"].ToString();
        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("No X-Hub-Signature-256 header present");
            return false;
        }

        // Remove "sha256=" prefix
        var expectedSignature = signature.Replace("sha256=", "");

        // Calculate HMAC-SHA256
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();

        // Use constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(computedSignature));
    }
}

/// <summary>
/// GitHub push event payload (minimal fields)
/// </summary>
public class GitHubPushPayload
{
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("repository")]
    public GitHubRepository? Repository { get; set; }

    [JsonPropertyName("pusher")]
    public GitHubPusher? Pusher { get; set; }

    [JsonPropertyName("commits")]
    public List<GitHubCommit>? Commits { get; set; }
}

public class GitHubRepository
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class GitHubPusher
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class GitHubCommit
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}
