namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IAIService
{
    Task<string> AnalyzeDocumentAsync(string documentText, string fileType);
    Task<string[]> GenerateTagsAsync(string text);
    Task<string> SummarizeTextAsync(string text, int maxLength = 500);
    Task<string> ExtractKeyConceptsAsync(string text);
    Task<string> ChatCompletionAsync(string prompt, string? context = null);
}
