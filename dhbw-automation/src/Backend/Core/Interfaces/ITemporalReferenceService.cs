using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for parsing German temporal expressions and resolving them to concrete date/time ranges
/// </summary>
public interface ITemporalReferenceService
{
    /// <summary>
    /// Parses temporal expressions from text and resolves them to date/time ranges
    /// </summary>
    /// <param name="text">Text containing temporal expressions (e.g., "heute morgen", "letzte Woche")</param>
    /// <param name="referenceDate">Reference date for relative expressions (defaults to now)</param>
    /// <returns>Parse result with resolved temporal references</returns>
    Task<TemporalParseResult> ParseTemporalExpressionAsync(string text, DateTime? referenceDate = null);

    /// <summary>
    /// Finds calendar events within a specific time range
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="start">Start of time range</param>
    /// <param name="end">End of time range</param>
    /// <returns>List of matching calendar events</returns>
    Task<List<CalendarEvent>> FindEventsInTimeRangeAsync(int userId, DateTime start, DateTime end);

    /// <summary>
    /// Resolves "der Termin von eben" or similar recent event references
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="context">Optional context (professor name, subject) to filter events</param>
    /// <param name="referenceDate">Reference date (defaults to now)</param>
    /// <returns>Most recent matching event or null</returns>
    Task<CalendarEvent?> ResolveMostRecentEventAsync(int userId, string? context = null, DateTime? referenceDate = null);

    /// <summary>
    /// Resolves a time-of-day expression to a time range for a specific date
    /// </summary>
    /// <param name="expression">Time-of-day expression (e.g., "morgen", "nachmittag")</param>
    /// <param name="date">Date to apply the time range to</param>
    /// <returns>Tuple of start and end times, or null if not recognized</returns>
    (DateTime Start, DateTime End)? ResolveTimeOfDay(string expression, DateTime date);

    /// <summary>
    /// Parses a German weekday name to DayOfWeek
    /// </summary>
    /// <param name="weekday">German weekday name (e.g., "Montag", "Di")</param>
    /// <returns>DayOfWeek or null if not recognized</returns>
    DayOfWeek? ParseGermanWeekday(string weekday);

    /// <summary>
    /// Finds the next or previous occurrence of a weekday
    /// </summary>
    /// <param name="weekday">Target weekday</param>
    /// <param name="referenceDate">Reference date</param>
    /// <param name="findNext">True to find next occurrence, false for previous</param>
    /// <returns>Date of the weekday</returns>
    DateTime FindWeekday(DayOfWeek weekday, DateTime referenceDate, bool findNext = true);
}
