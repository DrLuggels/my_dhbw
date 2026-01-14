using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.IntentAnalysis;

public partial class IntentAnalysisService
{
    public async Task<DocumentIntent> AnalyzeDocumentIntentAsync(string text, string documentType, int? userId = null)
    {
        return await _aiMetrics.TrackAsync("AnalyzeIntent", "Anthropic", AnthropicModel, async () =>
        {
            try
            {
                _logger.LogInformation("Analyzing document intent with Claude Sonnet 4.5 for user {UserId}", userId);

                var anthropicKey = await GetApiKeyAsync(userId);
                if (string.IsNullOrEmpty(anthropicKey))
                {
                    _logger.LogError("Anthropic API Key not configured for user {UserId}", userId);
                    return new DocumentIntent { PrimaryIntent = "unknown", ActionRequired = "none", ConfidenceScore = 0 };
                }

                var systemPrompt = GetSystemPrompt();
                var userMessage = $"Analysiere dieses {documentType}-Dokument:\n\n{text.Substring(0, Math.Min(text.Length, 8000))}";

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", anthropicKey);
                client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

                var requestBody = new
                {
                    model = AnthropicModel,
                    max_tokens = 4096,
                    system = systemPrompt,
                    messages = new[] { new { role = "user", content = userMessage } }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(AnthropicEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Anthropic API error: Status={Status}, Content={Content}", response.StatusCode, errorContent);
                    throw new InvalidOperationException($"Anthropic API error: {response.StatusCode}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Anthropic response received, parsing JSON...");

                var responseDoc = JsonDocument.Parse(responseJson);
                var contentArray = responseDoc.RootElement.GetProperty("content");
                var textContent = contentArray[0].GetProperty("text").GetString() ?? "{}";

                textContent = RemoveMarkdownCodeBlocks(textContent);

                var intentDoc = JsonDocument.Parse(textContent);
                var intent = ParseIntentFromJsonDocument(intentDoc);

                _logger.LogInformation($"Intent analysis complete: PrimaryIntent={intent.PrimaryIntent}");
                return intent;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("unavailable"))
            {
                _logger.LogWarning(ex, "Anthropic service unavailable - returning fallback intent");
                return new DocumentIntent { PrimaryIntent = "unknown", ActionRequired = "none" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document intent");
                return new DocumentIntent { PrimaryIntent = "unknown", ActionRequired = "none" };
            }
        });
    }

    private string RemoveMarkdownCodeBlocks(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        text = text.Trim();
        if (text.StartsWith("```json"))
            text = text.Substring(7).TrimStart();
        else if (text.StartsWith("```"))
            text = text.Substring(3).TrimStart();

        var closingBacktickIndex = text.LastIndexOf("```");
        if (closingBacktickIndex >= 0)
            text = text.Substring(0, closingBacktickIndex).TrimEnd();

        return text.Trim();
    }
}
