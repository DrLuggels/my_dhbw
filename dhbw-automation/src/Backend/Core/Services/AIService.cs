using DHBWAutomation.Backend.Core.Interfaces;

namespace DHBWAutomation.Backend.Core.Services;

public class AIService : IAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIService> _logger;
    private readonly string? _openAiApiKey;

    public AIService(IHttpClientFactory httpClientFactory, ILogger<AIService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    public async Task<string> AnalyzeDocumentAsync(string documentText, string fileType)
    {
        try
        {
            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                _logger.LogWarning("OpenAI API Key not configured");
                return "AI-Analyse nicht verfügbar (API Key fehlt)";
            }

            // Simplified - real implementation would call OpenAI API
            _logger.LogInformation("Analyzing document with AI");
            return "Dokument wurde analysiert. Kategorie: Studium, Fach: unbekannt";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document");
            return "Fehler bei der Analyse";
        }
    }

    public async Task<string[]> GenerateTagsAsync(string text)
    {
        try
        {
            // Simplified implementation
            return await Task.FromResult(new[] { "studium", "dhbw", "dokument" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tags");
            return Array.Empty<string>();
        }
    }

    public async Task<string> SummarizeTextAsync(string text, int maxLength = 500)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Simple truncation for now
            return await Task.FromResult(
                text.Length <= maxLength 
                    ? text 
                    : text.Substring(0, maxLength) + "..."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing text");
            return string.Empty;
        }
    }

    public async Task<string> ExtractKeyConceptsAsync(string text)
    {
        try
        {
            // Simplified implementation
            return await Task.FromResult("Schlüsselkonzepte: Automatisierung, Studium, KI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting key concepts");
            return string.Empty;
        }
    }

    public async Task<string> ChatCompletionAsync(string prompt, string? context = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                _logger.LogWarning("OpenAI API Key not configured");
                return "AI-Chat nicht verfügbar (API Key fehlt)";
            }

            // Simplified - real implementation would call OpenAI API
            return await Task.FromResult($"Antwort auf: {prompt}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat completion");
            return "Fehler beim Chat";
        }
    }
}
