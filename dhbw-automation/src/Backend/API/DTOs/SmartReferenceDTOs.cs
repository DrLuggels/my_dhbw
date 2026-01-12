namespace DHBWAutomation.Backend.API.DTOs;

/// <summary>
/// Request to resolve natural language references in text
/// </summary>
public class ResolveReferencesRequest
{
    /// <summary>
    /// Text containing natural language references (e.g., "der Prof von heute morgen")
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference date for temporal expressions (defaults to now)
    /// </summary>
    public DateTime? ReferenceDate { get; set; }
}

/// <summary>
/// Request to automatically link a note to related events
/// </summary>
public class AutoLinkRequest
{
    /// <summary>
    /// Calendar event ID containing the note
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Content of the note to analyze
    /// </summary>
    public string NoteContent { get; set; } = string.Empty;

    /// <summary>
    /// Whether to auto-confirm high confidence links (>= 0.8)
    /// </summary>
    public bool AutoConfirmHighConfidence { get; set; } = true;
}

/// <summary>
/// Response for auto-link operation
/// </summary>
public class AutoLinkResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of links created
    /// </summary>
    public int LinksCreated { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Request to get link suggestions for note content
/// </summary>
public class GetSuggestionsRequest
{
    /// <summary>
    /// Content of the note to analyze
    /// </summary>
    public string NoteContent { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Event ID if note is from a calendar event (to exclude self-references)
    /// </summary>
    public int? SourceEventId { get; set; }
}

/// <summary>
/// Request to confirm a suggested link
/// </summary>
public class ConfirmLinkRequest
{
    /// <summary>
    /// Source entity type (e.g., "calendar_event", "note")
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Source entity ID
    /// </summary>
    public int SourceId { get; set; }

    /// <summary>
    /// Target entity type
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Target entity ID
    /// </summary>
    public int TargetId { get; set; }

    /// <summary>
    /// Type of link to create
    /// </summary>
    public string LinkType { get; set; } = "related";

    /// <summary>
    /// Confidence score
    /// </summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// Optional reason for the link
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Response for confirm link operation
/// </summary>
public class ConfirmLinkResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID of the created link
    /// </summary>
    public int? LinkId { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Request to parse a temporal expression
/// </summary>
public class ParseTemporalRequest
{
    /// <summary>
    /// Temporal expression to parse (e.g., "heute morgen", "letzte Woche")
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference date (defaults to now)
    /// </summary>
    public DateTime? ReferenceDate { get; set; }
}
