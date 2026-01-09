using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IIntentAnalysisService
{
    /// <summary>
    /// Analyzes document text to determine user intent and extract structured information
    /// Uses Claude Sonnet 4.5 for complex reasoning
    /// </summary>
    Task<DocumentIntent> AnalyzeDocumentIntentAsync(string text, string documentType);

    /// <summary>
    /// Generates UserInteraction objects based on document intent
    /// These will be shown as questions/prompts in the dashboard
    /// </summary>
    Task<List<UserInteraction>> GenerateInteractionsAsync(
        DocumentIntent intent,
        int userId,
        int documentId
    );
}
