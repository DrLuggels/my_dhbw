using System.Text.Json;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IAIService
{
    Task<string> AnalyzeDocumentAsync(string documentText, string fileType, int? userId = null);
    Task<string[]> GenerateTagsAsync(string text, int? userId = null);
    Task<string> SummarizeTextAsync(string text, int maxLength = 500, int? userId = null);
    Task<string> ExtractKeyConceptsAsync(string text, int? userId = null);
    Task<string> ChatCompletionAsync(string prompt, string? context = null, int? userId = null);
    Task<JsonDocument?> GenerateJsonWithGeminiAsync(string systemPrompt, string userPrompt, int? userId = null);
}
