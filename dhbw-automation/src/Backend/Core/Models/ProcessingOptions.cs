namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Options for controlling document processing behavior
/// Allows selective enabling/disabling of AI features to manage rate limits
/// </summary>
public class ProcessingOptions
{
    /// <summary>
    /// Enable automatic text correction for detected errors (uses Claude API)
    /// Default: false (disabled to save API calls)
    /// </summary>
    public bool EnableTextCorrection { get; set; } = false;

    /// <summary>
    /// Generate tags for the document (uses OpenAI API)
    /// Default: true (tags are useful for search/filtering)
    /// </summary>
    public bool GenerateTags { get; set; } = true;

    /// <summary>
    /// Generate summary for the document (uses OpenAI API)
    /// Default: true (summary is important for quick overview)
    /// </summary>
    public bool GenerateSummary { get; set; } = true;

    /// <summary>
    /// Enable intent analysis with Claude (extracts TODOs, meetings, projects)
    /// Default: true (this is a core feature)
    /// </summary>
    public bool EnableIntentAnalysis { get; set; } = true;

    /// <summary>
    /// Enable learning analytics (error tracking and deficit creation)
    /// Default: true (important for learning system)
    /// </summary>
    public bool EnableLearningAnalytics { get; set; } = true;

    /// <summary>
    /// Enable automatic user interaction generation
    /// Default: true (for conversational dashboard)
    /// </summary>
    public bool GenerateInteractions { get; set; } = true;

    /// <summary>
    /// Automatically promote entities with very high confidence (>=95%) to production DB
    /// Default: false (require user confirmation for all entities)
    /// </summary>
    public bool AutoPromoteHighConfidence { get; set; } = false;

    /// <summary>
    /// Default options - balanced between features and API usage
    /// </summary>
    public static ProcessingOptions Default => new()
    {
        EnableTextCorrection = false, // Disabled by default (expensive)
        GenerateTags = true,
        GenerateSummary = true,
        EnableIntentAnalysis = true,
        EnableLearningAnalytics = true,
        GenerateInteractions = true,
        AutoPromoteHighConfidence = false // Require user confirmation
    };

    /// <summary>
    /// Fast processing - minimal AI calls
    /// Use for bulk operations or when rate limits are an issue
    /// </summary>
    public static ProcessingOptions Fast => new()
    {
        EnableTextCorrection = false,
        GenerateTags = false, // Skip tags
        GenerateSummary = true, // Keep summary (essential)
        EnableIntentAnalysis = true, // Keep intent (core feature)
        EnableLearningAnalytics = false, // Skip learning analytics
        GenerateInteractions = false, // Skip interactions
        AutoPromoteHighConfidence = true // Auto-promote to save time
    };

    /// <summary>
    /// Full processing - all features enabled
    /// Use when processing priority documents
    /// </summary>
    public static ProcessingOptions Full => new()
    {
        EnableTextCorrection = true,
        GenerateTags = true,
        GenerateSummary = true,
        EnableIntentAnalysis = true,
        EnableLearningAnalytics = true,
        GenerateInteractions = true,
        AutoPromoteHighConfidence = false // Manual review for important docs
    };

    /// <summary>
    /// Minimal processing - only text extraction
    /// Use for archived documents or backup purposes
    /// </summary>
    public static ProcessingOptions Minimal => new()
    {
        EnableTextCorrection = false,
        GenerateTags = false,
        GenerateSummary = false,
        EnableIntentAnalysis = false,
        EnableLearningAnalytics = false,
        GenerateInteractions = false,
        AutoPromoteHighConfidence = false
    };
}
