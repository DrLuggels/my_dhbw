namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Result of smart reference resolution using AI
/// </summary>
public class SmartReferenceResult
{
    /// <summary>
    /// List of resolved smart references
    /// </summary>
    public List<SmartReference> References { get; set; } = new();

    /// <summary>
    /// Overall confidence of the resolution
    /// </summary>
    public double OverallConfidence { get; set; }

    /// <summary>
    /// Whether the resolution was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if resolution failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// A single resolved smart reference
/// </summary>
public class SmartReference
{
    /// <summary>
    /// Original text of the reference
    /// </summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// Type of reference
    /// </summary>
    public SmartReferenceType ReferenceType { get; set; }

    /// <summary>
    /// Resolved entity information
    /// </summary>
    public ResolvedEntity? ResolvedTo { get; set; }

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Alternative resolutions (if ambiguous)
    /// </summary>
    public List<AlternativeResolution>? Alternatives { get; set; }

    /// <summary>
    /// Position in original text
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// End position in original text
    /// </summary>
    public int EndPosition { get; set; }
}

/// <summary>
/// Type of smart reference
/// </summary>
public enum SmartReferenceType
{
    /// <summary>
    /// Reference to a professor
    /// </summary>
    Professor,

    /// <summary>
    /// Reference to a calendar event
    /// </summary>
    Event,

    /// <summary>
    /// Temporal reference (e.g., "heute morgen")
    /// </summary>
    Temporal,

    /// <summary>
    /// Combined professor + temporal (e.g., "der Prof von heute morgen")
    /// </summary>
    ProfessorTemporal,

    /// <summary>
    /// Reference to a subject/course
    /// </summary>
    Subject,

    /// <summary>
    /// Reference to a document
    /// </summary>
    Document,

    /// <summary>
    /// Reference to a location
    /// </summary>
    Location
}

/// <summary>
/// Information about a resolved entity
/// </summary>
public class ResolvedEntity
{
    /// <summary>
    /// Entity type (calendar_event, document, etc.)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID in the database
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Human-readable display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata about the entity
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// An alternative resolution for ambiguous references
/// </summary>
public class AlternativeResolution
{
    /// <summary>
    /// Resolved entity
    /// </summary>
    public ResolvedEntity Entity { get; set; } = new();

    /// <summary>
    /// Confidence for this alternative
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Reason this is a potential match
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
