using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace DHBWAutomation.Backend.Core.BackgroundServices;

/// <summary>
/// Background service for processing documents in a queue
/// Prevents rate limit issues by processing documents sequentially with delays
/// </summary>
public class DocumentProcessingBackgroundService : BackgroundService
{
    private readonly ILogger<DocumentProcessingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<DocumentProcessingRequest> _queue;

    // Configuration
    private const int DelayBetweenDocumentsMs = 2000; // 2 seconds between documents
    private const int MaxRetries = 3;

    public DocumentProcessingBackgroundService(
        ILogger<DocumentProcessingBackgroundService> _logger,
        IServiceProvider serviceProvider)
    {
        this._logger = _logger;
        _serviceProvider = serviceProvider;

        // Create unbounded channel for queuing documents
        _queue = Channel.CreateUnbounded<DocumentProcessingRequest>(new UnboundedChannelOptions
        {
            SingleReader = true, // Only this service reads from the queue
            SingleWriter = false // Multiple threads can add to queue
        });
    }

    /// <summary>
    /// Queue a document for background processing
    /// </summary>
    public async Task QueueDocumentAsync(int documentId, ProcessingOptions? options = null, int priority = 0)
    {
        var request = new DocumentProcessingRequest
        {
            DocumentId = documentId,
            Options = options ?? ProcessingOptions.Default,
            Priority = priority,
            QueuedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        await _queue.Writer.WriteAsync(request);
        _logger.LogInformation($"Queued document {documentId} for processing (priority: {priority})");
    }

    /// <summary>
    /// Get current queue statistics
    /// </summary>
    public int GetQueueCount()
    {
        return _queue.Reader.Count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Processing Background Service started");

        try
        {
            await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessDocumentWithRetryAsync(request, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process document {request.DocumentId} after {MaxRetries} retries");

                    // Mark document as failed
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var document = await context.Documents.FindAsync(request.DocumentId);
                        if (document != null)
                        {
                            document.IsProcessed = true; // Mark as processed to prevent re-queuing
                            document.ProcessedAt = DateTime.UtcNow;
                            document.Summary = $"Fehler bei der Verarbeitung: {ex.Message}";
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, $"Failed to mark document {request.DocumentId} as failed");
                    }
                }

                // Wait between documents to respect rate limits
                if (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(DelayBetweenDocumentsMs, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Document Processing Background Service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Document Processing Background Service crashed");
            throw;
        }
    }

    private async Task ProcessDocumentWithRetryAsync(DocumentProcessingRequest request, CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                _logger.LogInformation($"Processing document {request.DocumentId} (attempt {attempt}/{MaxRetries})");

                var success = await fileService.ProcessDocumentAsync(request.DocumentId, request.Options);

                if (success)
                {
                    _logger.LogInformation($"Successfully processed document {request.DocumentId}");
                    return;
                }
                else
                {
                    _logger.LogWarning($"Document {request.DocumentId} processing returned false");

                    // If it returned false, it might already be processed or not found
                    // Don't retry in this case
                    return;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("too many requests"))
            {
                // Rate limit hit - wait longer before retry
                _logger.LogWarning($"Rate limit hit for document {request.DocumentId}, waiting before retry");

                if (attempt < MaxRetries)
                {
                    var waitTime = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 10); // Exponential backoff: 20s, 40s, 80s
                    await Task.Delay(waitTime, stoppingToken);
                }
                else
                {
                    throw;
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // HTTP 429 - Too Many Requests
                _logger.LogWarning($"HTTP 429 for document {request.DocumentId}, waiting before retry");

                if (attempt < MaxRetries)
                {
                    var waitTime = TimeSpan.FromSeconds(30 * attempt); // 30s, 60s, 90s
                    await Task.Delay(waitTime, stoppingToken);
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing document {request.DocumentId} (attempt {attempt}/{MaxRetries})");

                if (attempt >= MaxRetries)
                {
                    throw;
                }

                // Wait before retry (exponential backoff)
                var waitTime = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 2); // 4s, 8s, 16s
                await Task.Delay(waitTime, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Document Processing Background Service stopping. {_queue.Reader.Count} documents remaining in queue");

        // Complete the channel to signal no more items will be added
        _queue.Writer.Complete();

        await base.StopAsync(stoppingToken);
    }
}

/// <summary>
/// Request object for queuing documents
/// </summary>
public class DocumentProcessingRequest
{
    public int DocumentId { get; set; }
    public ProcessingOptions Options { get; set; } = ProcessingOptions.Default;
    public int Priority { get; set; } = 0; // Higher priority = processed first (future feature)
    public DateTime QueuedAt { get; set; }
    public int RetryCount { get; set; } = 0;
}
