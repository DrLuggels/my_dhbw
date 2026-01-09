using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Shared.Helpers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;

namespace DHBWAutomation.Backend.Core.Services;

public class AIService : IAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIService> _logger;
    private readonly AiMetrics _aiMetrics;

    // API Keys for different providers
    private readonly string? _openAiApiKey;
    private readonly string? _anthropicApiKey;
    private readonly string? _geminiApiKey;

    // Rate Limiters
    // OpenAI GPT-4/5 mini: Tier 2 hat 10,000 RPM / 2M TPM - wir nutzen 50 RPM konservativ
    private static readonly RateLimiter _openAiLimiter = new(50, TimeSpan.FromMinutes(1));
    // Gemini 3 Flash: 60 RPM (Free Tier ausreichend für Bulk Operations)
    private static readonly RateLimiter _geminiLimiter = new(60, TimeSpan.FromMinutes(1));

    // Retry Policy for OpenAI
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _openAiRetryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r =>
            r.StatusCode == HttpStatusCode.TooManyRequests ||
            r.StatusCode == HttpStatusCode.ServiceUnavailable ||
            r.StatusCode == HttpStatusCode.RequestTimeout
        )
        .Or<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"⚠️  OpenAI Retry {retryCount}/3 after {timeSpan.TotalSeconds}s");
            }
        );

    // Circuit Breaker for OpenAI
    private static readonly AsyncCircuitBreakerPolicy _openAiCircuitBreaker = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1),
            onBreak: (ex, duration) => Console.WriteLine($"🔴 OpenAI Circuit OPEN for {duration.TotalMinutes}min"),
            onReset: () => Console.WriteLine("🟢 OpenAI Circuit CLOSED"),
            onHalfOpen: () => Console.WriteLine("🟡 OpenAI Circuit HALF-OPEN")
        );

    // Combined OpenAI Policy
    private static readonly AsyncPolicyWrap<HttpResponseMessage> _openAiResiliencePolicy =
        Policy.WrapAsync(_openAiCircuitBreaker.AsAsyncPolicy<HttpResponseMessage>(), _openAiRetryPolicy);

    // API Endpoints - Latest models (January 2026)
    private const string OpenAiEndpoint = "https://api.openai.com/v1/chat/completions";
    private const string OpenAiModel = "gpt-5-mini"; // Cost-effective for standard tasks

    private const string AnthropicEndpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicModel = "claude-sonnet-4.5"; // Best for reasoning
    private const string AnthropicVersion = "2023-06-01";

    private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string GeminiModel = "gemini-3-flash-preview"; // Best for vision/extraction

    public AIService(IHttpClientFactory httpClientFactory, ILogger<AIService> logger, AiMetrics aiMetrics)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _aiMetrics = aiMetrics;

        // Load API keys from environment variables
        _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        _geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public async Task<string> AnalyzeDocumentAsync(string documentText, string fileType)
    {
        try
        {
            // Use Gemini 3 Flash for document analysis (best for multimodal/extraction)
            if (string.IsNullOrEmpty(_geminiApiKey))
            {
                _logger.LogWarning("Gemini API Key not configured");
                return "AI-Analyse nicht verfügbar (Gemini API Key fehlt)";
            }

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{GeminiEndpoint}/{GeminiModel}:generateContent?key={_geminiApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = $@"Analysiere dieses Dokument und extrahiere folgende Informationen:
1. Kategorie (z.B. Studium, Persönlich, Finanzen, etc.)
2. Fach/Bereich (falls zutreffend)
3. Wichtige Themen
4. Dokumenttyp

Dokument ({fileType}):
{documentText.Substring(0, Math.Min(documentText.Length, 4000))}"
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 500
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(requestUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseJson);

            var analysisText = result.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "Keine Analyse verfügbar";

            _logger.LogInformation("Document analyzed successfully with Gemini 3 Flash");
            return analysisText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document with Gemini");
            return "Fehler bei der Analyse";
        }
    }

    public async Task<string[]> GenerateTagsAsync(string text)
    {
        return await _aiMetrics.TrackAsync("GenerateTags", "OpenAI", OpenAiModel, async () =>
        {
            try
            {
                // Use GPT-5 mini for tag generation (cost-effective for simple tasks)
                if (string.IsNullOrEmpty(_openAiApiKey))
                {
                    _logger.LogWarning("OpenAI API Key not configured");
                    return new[] { "studium", "dhbw", "dokument" };
                }

                return await _openAiLimiter.ExecuteAsync(async () =>
                {
                    var response = await _openAiResiliencePolicy.ExecuteAsync(async () =>
                    {
                        var client = _httpClientFactory.CreateClient("OpenAI");

                        var requestBody = new
                        {
                            model = OpenAiModel,
                            messages = new[]
                            {
                                new
                                {
                                    role = "system",
                                    content = "Du bist ein Experte für Dokumentenklassifizierung. Generiere 5-10 relevante Tags für das gegebene Dokument. Antworte NUR mit einer kommagetrennten Liste von Tags auf Deutsch."
                                },
                                new
                                {
                                    role = "user",
                                    content = $"Generiere Tags für diesen Text:\n\n{text.Substring(0, Math.Min(text.Length, 2000))}"
                                }
                            },
                            temperature = 0.5,
                            max_tokens = 100
                        };

                        var json = JsonSerializer.Serialize(requestBody);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        return await client.PostAsync("chat/completions", content);
                    });

                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(responseJson);

                    var tagsText = result.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "";

                    var tags = tagsText
                        .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().ToLower())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToArray();

                    _logger.LogInformation($"Generated {tags.Length} tags with GPT-5 mini");
                    return tags.Length > 0 ? tags : new[] { "studium", "dhbw", "dokument" };
                });
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Circuit breaker OPEN - OpenAI unavailable");
                return new[] { "studium", "dhbw", "dokument" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tags with OpenAI");
                return new[] { "studium", "dhbw", "dokument" };
            }
        });
    }

    public async Task<string> SummarizeTextAsync(string text, int maxLength = 500)
    {
        return await _aiMetrics.TrackAsync("SummarizeText", "OpenAI", OpenAiModel, async () =>
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return string.Empty;

                // If text is already short enough, return as is
                if (text.Length <= maxLength)
                    return text;

                // Use GPT-5 mini for summarization (cost-effective)
                if (string.IsNullOrEmpty(_openAiApiKey))
                {
                    _logger.LogWarning("OpenAI API Key not configured");
                    return text.Substring(0, maxLength) + "...";
                }

                return await _openAiLimiter.ExecuteAsync(async () =>
                {
                    var response = await _openAiResiliencePolicy.ExecuteAsync(async () =>
                    {
                        var client = _httpClientFactory.CreateClient("OpenAI");

                        var requestBody = new
                        {
                            model = OpenAiModel,
                            messages = new[]
                            {
                                new
                                {
                                    role = "system",
                                    content = $"Du bist ein Experte für Textzusammenfassungen. Erstelle eine prägnante Zusammenfassung (maximal {maxLength} Zeichen) auf Deutsch."
                                },
                                new
                                {
                                    role = "user",
                                    content = $"Fasse diesen Text zusammen:\n\n{text.Substring(0, Math.Min(text.Length, 3000))}"
                                }
                            },
                            temperature = 0.3,
                            max_tokens = maxLength / 2
                        };

                        var json = JsonSerializer.Serialize(requestBody);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        return await client.PostAsync("chat/completions", content);
                    });

                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(responseJson);

                    var summary = result.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? text.Substring(0, Math.Min(text.Length, maxLength)) + "...";

                    _logger.LogInformation("Text summarized successfully with GPT-5 mini");
                    return summary;
                });
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Circuit breaker OPEN - OpenAI unavailable");
                return text.Substring(0, Math.Min(text.Length, maxLength)) + "...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing text with OpenAI");
                return text.Substring(0, Math.Min(text.Length, maxLength)) + "...";
            }
        });
    }

    public async Task<string> ExtractKeyConceptsAsync(string text)
    {
        try
        {
            // Use GPT-5 mini for key concept extraction
            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                _logger.LogWarning("OpenAI API Key not configured");
                return "Schlüsselkonzepte: Automatisierung, Studium, KI";
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);

            var requestBody = new
            {
                model = OpenAiModel,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Du bist ein Experte für Textanalyse. Extrahiere die wichtigsten Konzepte und Themen aus dem gegebenen Text. Liste sie als kommagetrennte Begriffe auf."
                    },
                    new
                    {
                        role = "user",
                        content = $"Extrahiere die Schlüsselkonzepte aus diesem Text:\n\n{text.Substring(0, Math.Min(text.Length, 2000))}"
                    }
                },
                temperature = 0.3,
                max_tokens = 150
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(OpenAiEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseJson);

            var concepts = result.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Schlüsselkonzepte: Automatisierung, Studium, KI";

            _logger.LogInformation("Key concepts extracted successfully with GPT-5 mini");
            return concepts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting key concepts with OpenAI");
            return "Schlüsselkonzepte: Automatisierung, Studium, KI";
        }
    }

    public async Task<string> ChatCompletionAsync(string prompt, string? context = null)
    {
        try
        {
            // Use Claude Sonnet 4.5 for complex reasoning and chat (the "brain")
            if (string.IsNullOrEmpty(_anthropicApiKey))
            {
                _logger.LogWarning("Anthropic API Key not configured");
                return "AI-Chat nicht verfügbar (Anthropic API Key fehlt)";
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", _anthropicApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

            var systemMessage = "Du bist ein hilfreicher AI-Assistent für DHBW-Studenten. " +
                "Du hilfst bei der Organisation von Studienunterlagen, Kalenderverwaltung und allgemeinen Fragen zum Studium.";

            var userMessage = string.IsNullOrEmpty(context)
                ? prompt
                : $"Kontext: {context}\n\nFrage: {prompt}";

            var requestBody = new
            {
                model = AnthropicModel,
                max_tokens = 1024,
                system = systemMessage,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = userMessage
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(AnthropicEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseJson);

            var answer = result.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "Keine Antwort verfügbar";

            _logger.LogInformation("Chat completion successful with Claude Sonnet 4.5");
            return answer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat completion with Claude");
            return "Fehler beim Chat";
        }
    }
}
