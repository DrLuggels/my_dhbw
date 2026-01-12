namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Represents a parsed temporal reference from natural language
/// </summary>
public class TemporalReference
{
    /// <summary>
    /// Start of the resolved time range
    /// </summary>
    public DateTime ResolvedStart { get; set; }

    /// <summary>
    /// End of the resolved time range
    /// </summary>
    public DateTime ResolvedEnd { get; set; }

    /// <summary>
    /// Original text expression (e.g., "heute morgen")
    /// </summary>
    public string OriginalExpression { get; set; } = string.Empty;

    /// <summary>
    /// Type of temporal reference
    /// </summary>
    public TemporalType Type { get; set; }

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Position in original text where expression starts
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// Position in original text where expression ends
    /// </summary>
    public int EndPosition { get; set; }
}

/// <summary>
/// Type of temporal reference
/// </summary>
public enum TemporalType
{
    /// <summary>
    /// Absolute date/time (e.g., "am 15. Januar")
    /// </summary>
    Absolute,

    /// <summary>
    /// Relative to current time (e.g., "vor 3 Tagen")
    /// </summary>
    Relative,

    /// <summary>
    /// Contextual/named periods (e.g., "heute morgen", "letzte Woche")
    /// </summary>
    Contextual,

    /// <summary>
    /// Reference to recent event (e.g., "der Termin von eben")
    /// </summary>
    RecentEvent
}

/// <summary>
/// Result of parsing temporal expressions from text
/// </summary>
public class TemporalParseResult
{
    /// <summary>
    /// Whether parsing was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of parsed temporal references
    /// </summary>
    public List<TemporalReference> References { get; set; } = new();

    /// <summary>
    /// Error message if parsing failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Overall confidence of the parsing
    /// </summary>
    public double OverallConfidence { get; set; }
}

/// <summary>
/// Common German time-of-day periods
/// </summary>
public static class GermanTimePeriods
{
    public static readonly (TimeSpan Start, TimeSpan End) Morgen = (new TimeSpan(7, 0, 0), new TimeSpan(12, 0, 0));
    public static readonly (TimeSpan Start, TimeSpan End) Mittag = (new TimeSpan(12, 0, 0), new TimeSpan(14, 0, 0));
    public static readonly (TimeSpan Start, TimeSpan End) Nachmittag = (new TimeSpan(14, 0, 0), new TimeSpan(18, 0, 0));
    public static readonly (TimeSpan Start, TimeSpan End) Abend = (new TimeSpan(18, 0, 0), new TimeSpan(22, 0, 0));
    public static readonly (TimeSpan Start, TimeSpan End) Nacht = (new TimeSpan(22, 0, 0), new TimeSpan(7, 0, 0));
}
