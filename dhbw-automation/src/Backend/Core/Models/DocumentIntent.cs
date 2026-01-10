namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Represents the analyzed intent of a document
/// </summary>
public class DocumentIntent
{
    public string PrimaryIntent { get; set; } = string.Empty; // "schedule_meeting", "learning_content", "project_idea", "todo", "question"
    public List<string> SecondaryIntents { get; set; } = new();

    // Extracted Information
    public List<ExtractedMeeting> Meetings { get; set; } = new(); // Changed from single Meeting to List
    public List<ExtractedTodo> Todos { get; set; } = new();
    public ExtractedProject? Project { get; set; }
    public List<DetectedError> Errors { get; set; } = new();
    public LearningContent? LearningInfo { get; set; }

    public string ActionRequired { get; set; } = "none"; // "ask_user", "auto_create", "none"
    public string Urgency { get; set; } = "low"; // "low", "medium", "high", "urgent"

    // NEW: AI Confidence & Questions System
    /// <summary>
    /// Overall confidence score (0-100) für die gesamte Extraktion
    /// </summary>
    public int ConfidenceScore { get; set; } = 100;

    /// <summary>
    /// Klärungsfragen der AI zu unklaren/fehlenden Feldern
    /// </summary>
    public List<ExtractedQuestion> Questions { get; set; } = new();
}

/// <summary>
/// Meeting information extracted from document
/// </summary>
public class ExtractedMeeting
{
    public string PersonName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime? SuggestedDate { get; set; }
    public string? SuggestedTime { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 60;

    /// <summary>
    /// AI Confidence Score (0-100) für diese Meeting-Extraktion
    /// </summary>
    public int ConfidenceScore { get; set; } = 100;
}

/// <summary>
/// TODO/Task extracted from document
/// </summary>
public class ExtractedTodo
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "medium"; // "low", "medium", "high", "urgent"
    public DateTime? SuggestedDeadline { get; set; }
    public string Category { get; set; } = "general"; // "meeting", "learning", "project", "general"

    /// <summary>
    /// AI Confidence Score (0-100) für diese TODO-Extraktion
    /// </summary>
    public int ConfidenceScore { get; set; } = 100;
}

/// <summary>
/// Project information extracted from document
/// </summary>
public class ExtractedProject
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public List<string> Ideas { get; set; } = new();
    public string EstimatedPriority { get; set; } = "medium"; // "low", "medium", "high"

    /// <summary>
    /// AI Confidence Score (0-100) für diese Projekt-Extraktion
    /// </summary>
    public int ConfidenceScore { get; set; } = 100;
}

/// <summary>
/// Error detected in document (for learning analytics)
/// </summary>
public class DetectedError
{
    public string ErrorType { get; set; } = string.Empty; // "spelling", "concept", "calculation", "logic"
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Original { get; set; } = string.Empty;
    public string Corrected { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Severity { get; set; } = "low"; // "low", "medium", "high"
}

/// <summary>
/// Learning content information
/// </summary>
public class LearningContent
{
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public List<string> KeyConcepts { get; set; } = new();
    public string ComprehensionLevel { get; set; } = "good"; // "good", "partial", "poor"
    public bool NeedsMoreStudy { get; set; } = false;
}

/// <summary>
/// Eine Klärungsfrage der AI zu unklaren/fehlenden Daten
/// </summary>
public class ExtractedQuestion
{
    /// <summary>
    /// Entitätstyp + Feld-Name (z.B. "meeting.suggestedDate", "todo.dueDate")
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Die Frage, die der User sieht
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Vorgeschlagene Antwort-Optionen
    /// </summary>
    public List<string> SuggestedAnswers { get; set; } = new();

    /// <summary>
    /// Priorität: "critical", "high", "medium", "low"
    /// </summary>
    public string Priority { get; set; } = "medium";

    /// <summary>
    /// Erwarteter Antworttyp: "text", "date", "time", "datetime", "choice", "number"
    /// </summary>
    public string AnswerType { get; set; } = "text";

    /// <summary>
    /// Index der Entität (bei Listen wie Todos), null bei einzelnen Entitäten
    /// </summary>
    public int? EntityIndex { get; set; }
}
