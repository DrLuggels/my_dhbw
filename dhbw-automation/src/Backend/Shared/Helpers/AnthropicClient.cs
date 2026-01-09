using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;

namespace DHBWAutomation.Backend.Shared.Helpers;

/// <summary>
/// Wiederverwendbarer Client für Anthropic Claude API mit Resilience-Patterns
/// </summary>
public class AnthropicClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicClient> _logger;
    private readonly string? _apiKey;
    
    // Rate Limiter: 50 Requests pro Minute (Anthropic Tier 1 - Claude Sonnet)
    // Tier 1: 50 RPM, 40,000 TPM, 200,000 TPD
    private static readonly RateLimiter _rateLimiter = new(50, TimeSpan.FromMinutes(1));
    
    // Retry Policy mit Exponential Backoff
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => 
            r.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
            r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
            r.StatusCode == System.Net.HttpStatusCode.RequestTimeout
        )
        .Or<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timeSpan, retryCount, context) =>
            {
                var statusCode = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message ?? "Unknown";
                Console.WriteLine($"⚠️  Anthropic Retry {retryCount}/3 after {timeSpan.TotalSeconds}s (Reason: {statusCode})");
            }
        );
    
    // Circuit Breaker: Öffnet nach 5 Fehlern für 1 Minute
    private static readonly AsyncCircuitBreakerPolicy _circuitBreaker = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1),
            onBreak: (exception, duration) =>
            {
                Console.WriteLine($"🔴 Circuit OPEN: Anthropic Claude unavailable for {duration.TotalMinutes} minute(s)");
            },
            onReset: () =>
            {
                Console.WriteLine("🟢 Circuit CLOSED: Anthropic Claude available again");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("🟡 Circuit HALF-OPEN: Testing Anthropic Claude availability...");
            }
        );
    
    // Kombinierte Policy: Circuit Breaker → Retry
    private static readonly AsyncPolicyWrap<HttpResponseMessage> _resiliencePolicy = 
        Policy.WrapAsync(_circuitBreaker.AsAsyncPolicy<HttpResponseMessage>(), _retryPolicy);

    public AnthropicClient(IHttpClientFactory httpClientFactory, ILogger<AnthropicClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }

    /// <summary>
    /// Sendet eine Chat-Anfrage an Claude mit System- und User-Prompt
    /// </summary>
    /// <param name="systemPrompt">System-Prompt (Instruktionen für Claude)</param>
    /// <param name="userMessage">User-Message (eigentliche Anfrage)</param>
    /// <param name="model">Claude-Modell (Standard: claude-sonnet-4.5)</param>
    /// <param name="maxTokens">Maximale Anzahl Tokens für die Antwort</param>
    /// <param name="temperature">Temperature für Kreativität (0.0 - 1.0)</param>
    /// <returns>Claude's Antwort-Text</returns>
    public async Task<string> ChatAsync(
        string systemPrompt, 
        string userMessage, 
        string model = "claude-sonnet-4.5",
        int maxTokens = 1024,
        double temperature = 0.3)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogError("Anthropic API Key not configured");
            throw new InvalidOperationException("ANTHROPIC_API_KEY environment variable not set");
        }

        return await _rateLimiter.ExecuteAsync(async () =>
        {
            try
            {
                var response = await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var client = _httpClientFactory.CreateClient("Anthropic");
                    
                    var requestBody = new
                    {
                        model,
                        max_tokens = maxTokens,
                        temperature,
                        system = systemPrompt,
                        messages = new[]
                        {
                            new { role = "user", content = userMessage }
                        }
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json"
                    );

                    _logger.LogInformation("Calling Anthropic Claude API (Model: {Model}, MaxTokens: {MaxTokens})", 
                        model, maxTokens);

                    return await client.PostAsync("messages", content);
                });

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("content", out var contentArray) && contentArray.GetArrayLength() > 0)
                {
                    var firstContent = contentArray[0];
                    if (firstContent.TryGetProperty("text", out var textElement))
                    {
                        var responseText = textElement.GetString() ?? string.Empty;
                        _logger.LogInformation("Anthropic API call successful ({Length} chars)", responseText.Length);
                        return responseText;
                    }
                }

                _logger.LogWarning("Unexpected Anthropic response format");
                return string.Empty;
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Circuit breaker is OPEN - Anthropic service unavailable");
                throw new InvalidOperationException("Anthropic Claude service is currently unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Anthropic Claude API");
                throw;
            }
        });
    }

    /// <summary>
    /// Sendet eine Anfrage an Claude und erwartet eine JSON-Antwort
    /// </summary>
    /// <param name="systemPrompt">System-Prompt mit JSON-Format-Instruktionen</param>
    /// <param name="userMessage">User-Message</param>
    /// <param name="model">Claude-Modell</param>
    /// <param name="maxTokens">Maximale Tokens</param>
    /// <returns>Parsed JsonDocument</returns>
    public async Task<JsonDocument> ChatJsonAsync(
        string systemPrompt,
        string userMessage,
        string model = "claude-sonnet-4.5",
        int maxTokens = 4096)
    {
        var responseText = await ChatAsync(systemPrompt, userMessage, model, maxTokens, temperature: 0.3);
        
        // Extrahiere JSON aus Markdown-Code-Blöcken falls vorhanden
        var jsonText = ExtractJsonFromMarkdown(responseText);
        
        try
        {
            return JsonDocument.Parse(jsonText);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response as JSON. Response: {Response}", jsonText);
            throw new InvalidOperationException("Claude returned invalid JSON", ex);
        }
    }

    /// <summary>
    /// Extrahiert JSON aus Markdown-Code-Blöcken (```json ... ```)
    /// </summary>
    private string ExtractJsonFromMarkdown(string text)
    {
        var trimmed = text.Trim();
        
        // Entferne Markdown-Code-Blöcke
        if (trimmed.StartsWith("```json"))
        {
            var startIndex = trimmed.IndexOf('\n') + 1;
            var endIndex = trimmed.LastIndexOf("```");
            if (startIndex > 0 && endIndex > startIndex)
            {
                return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }
        else if (trimmed.StartsWith("```"))
        {
            var startIndex = trimmed.IndexOf('\n') + 1;
            var endIndex = trimmed.LastIndexOf("```");
            if (startIndex > 0 && endIndex > startIndex)
            {
                return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }
        
        return trimmed;
    }

    /// <summary>
    /// Gibt den aktuellen Status des Circuit Breakers zurück
    /// </summary>
    public static string CircuitBreakerState => _circuitBreaker.CircuitState.ToString();

    /// <summary>
    /// Gibt die Anzahl verfügbarer Rate Limit Slots zurück
    /// </summary>
    public static int AvailableSlots => _rateLimiter.AvailableSlots;
}
