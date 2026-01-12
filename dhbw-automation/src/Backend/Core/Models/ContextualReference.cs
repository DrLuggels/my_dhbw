namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Represents a contextual reference detected in text (professor, subject, event, etc.)
/// </summary>
public class ContextualReference
{
    /// <summary>
    /// Type of reference detected
    /// </summary>
    public ContextualReferenceType Type { get; set; }

    /// <summary>
    /// Original text of the reference as it appears in the source
    /// </summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// Normalized/standardized value (e.g., full professor name)
    /// </summary>
    public string NormalizedValue { get; set; } = string.Empty;

    /// <summary>
    /// Resolved entity ID (if successfully resolved)
    /// </summary>
    public int? ResolvedEntityId { get; set; }

    /// <summary>
    /// Resolved entity type (calendar_event, document, etc.)
    /// </summary>
    public string? ResolvedEntityType { get; set; }

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Position in original text where reference starts
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// Position in original text where reference ends
    /// </summary>
    public int EndPosition { get; set; }

    /// <summary>
    /// Additional metadata about the reference
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Type of contextual reference
/// </summary>
public enum ContextualReferenceType
{
    /// <summary>
    /// Reference to a professor/lecturer (e.g., "Prof. Mueller", "der Dozent")
    /// </summary>
    Professor,

    /// <summary>
    /// Reference to a subject/course (e.g., "Programmieren", "WDS125")
    /// </summary>
    Subject,

    /// <summary>
    /// Reference to a course code (e.g., "WDS125")
    /// </summary>
    CourseCode,

    /// <summary>
    /// Reference to a temporal event (e.g., "die Vorlesung von heute morgen")
    /// </summary>
    TemporalEvent,

    /// <summary>
    /// Reference to a location (e.g., "im Raum A123")
    /// </summary>
    Location,

    /// <summary>
    /// Reference to a document
    /// </summary>
    Document,

    /// <summary>
    /// Combined professor + temporal reference (e.g., "der Prof von heute morgen")
    /// </summary>
    ProfessorTemporal
}

/// <summary>
/// Result of analyzing text for contextual references
/// </summary>
public class ContextualAnalysisResult
{
    /// <summary>
    /// List of detected contextual references
    /// </summary>
    public List<ContextualReference> References { get; set; } = new();

    /// <summary>
    /// Suggested links based on detected references
    /// </summary>
    public List<SuggestedLink> SuggestedLinks { get; set; } = new();

    /// <summary>
    /// Overall confidence of the analysis
    /// </summary>
    public double OverallConfidence { get; set; }

    /// <summary>
    /// Whether the analysis was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if analysis failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// A suggested link between content and a resolved entity
/// </summary>
public class SuggestedLink
{
    /// <summary>
    /// Source entity type (e.g., "note", "document")
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Source entity ID
    /// </summary>
    public int SourceId { get; set; }

    /// <summary>
    /// Target entity type (e.g., "calendar_event")
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Target entity ID
    /// </summary>
    public int TargetId { get; set; }

    /// <summary>
    /// Suggested link type
    /// </summary>
    public string LinkType { get; set; } = "related";

    /// <summary>
    /// Confidence score for this suggestion (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Human-readable reason for the suggestion
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Original text that triggered this suggestion
    /// </summary>
    public string? ReferenceText { get; set; }

    /// <summary>
    /// Display name of the target entity
    /// </summary>
    public string? TargetDisplayName { get; set; }
}

/// <summary>
/// Professor information extracted from calendar events
/// </summary>
public class ProfessorInfo
{
    /// <summary>
    /// Full professor name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Name variations for matching
    /// </summary>
    public List<string> Variations { get; set; } = new();

    /// <summary>
    /// Subject(s) this professor teaches
    /// </summary>
    public List<string> Subjects { get; set; } = new();

    /// <summary>
    /// Calendar event IDs where this professor appears
    /// </summary>
    public List<int> EventIds { get; set; } = new();

    /// <summary>
    /// Most recent event ID for this professor
    /// </summary>
    public int? MostRecentEventId { get; set; }
}

/// <summary>
/// Subject information extracted from calendar events
/// </summary>
public class SubjectInfo
{
    /// <summary>
    /// Subject name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Course code (if available)
    /// </summary>
    public string? CourseCode { get; set; }

    /// <summary>
    /// Professor(s) teaching this subject
    /// </summary>
    public List<string> Professors { get; set; } = new();

    /// <summary>
    /// Calendar event IDs for this subject
    /// </summary>
    public List<int> EventIds { get; set; } = new();
}
