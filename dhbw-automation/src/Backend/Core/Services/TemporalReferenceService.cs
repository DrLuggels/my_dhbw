using System.Text.RegularExpressions;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for parsing German temporal expressions and resolving them to concrete date/time ranges
/// </summary>
public class TemporalReferenceService : ITemporalReferenceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TemporalReferenceService> _logger;

    // German weekday mappings
    private static readonly Dictionary<string, DayOfWeek> GermanWeekdays = new(StringComparer.OrdinalIgnoreCase)
    {
        { "montag", DayOfWeek.Monday }, { "mo", DayOfWeek.Monday },
        { "dienstag", DayOfWeek.Tuesday }, { "di", DayOfWeek.Tuesday },
        { "mittwoch", DayOfWeek.Wednesday }, { "mi", DayOfWeek.Wednesday },
        { "donnerstag", DayOfWeek.Thursday }, { "do", DayOfWeek.Thursday },
        { "freitag", DayOfWeek.Friday }, { "fr", DayOfWeek.Friday },
        { "samstag", DayOfWeek.Saturday }, { "sa", DayOfWeek.Saturday },
        { "sonntag", DayOfWeek.Sunday }, { "so", DayOfWeek.Sunday }
    };

    // German time-of-day mappings
    private static readonly Dictionary<string, (TimeSpan Start, TimeSpan End)> TimeOfDayMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "morgen", (new TimeSpan(7, 0, 0), new TimeSpan(12, 0, 0)) },
        { "morgens", (new TimeSpan(7, 0, 0), new TimeSpan(12, 0, 0)) },
        { "frueh", (new TimeSpan(6, 0, 0), new TimeSpan(9, 0, 0)) },
        { "vormittag", (new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0)) },
        { "vormittags", (new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0)) },
        { "mittag", (new TimeSpan(12, 0, 0), new TimeSpan(14, 0, 0)) },
        { "mittags", (new TimeSpan(12, 0, 0), new TimeSpan(14, 0, 0)) },
        { "nachmittag", (new TimeSpan(14, 0, 0), new TimeSpan(18, 0, 0)) },
        { "nachmittags", (new TimeSpan(14, 0, 0), new TimeSpan(18, 0, 0)) },
        { "abend", (new TimeSpan(18, 0, 0), new TimeSpan(22, 0, 0)) },
        { "abends", (new TimeSpan(18, 0, 0), new TimeSpan(22, 0, 0)) },
        { "nacht", (new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0)) },
        { "nachts", (new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0)) }
    };

    // Regex patterns for temporal expressions
    private static readonly Regex HeuteMorgenPattern = new(@"\bheute\s+(morgen|vormittag|mittag|nachmittag|abend|nacht)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GesternPattern = new(@"\bgestern\s*(morgen|vormittag|mittag|nachmittag|abend|nacht)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MorgenPattern = new(@"\bmorgen\s*(frueh|vormittag|mittag|nachmittag|abend|nacht)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VorXTagenPattern = new(@"\bvor\s+(\d+)\s+(tag|tagen|woche|wochen|monat|monaten)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InXTagenPattern = new(@"\bin\s+(\d+)\s+(tag|tagen|woche|wochen|monat|monaten)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LetzteWochePattern = new(@"\b(letzte|letzten|vergangene|vergangenen)\s+(woche|montag|dienstag|mittwoch|donnerstag|freitag|samstag|sonntag)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DieseWochePattern = new(@"\b(diese|diesen|dieses)\s+(woche|montag|dienstag|mittwoch|donnerstag|freitag|samstag|sonntag)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NaechsteWochePattern = new(@"\b(naechste|naechsten|kommende|kommenden)\s+(woche|montag|dienstag|mittwoch|donnerstag|freitag|samstag|sonntag)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AmWochentagPattern = new(@"\bam\s+(montag|dienstag|mittwoch|donnerstag|freitag|samstag|sonntag)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TerminVonEbenPattern = new(@"\b(termin|vorlesung|veranstaltung|kurs)\s+(von\s+)?(eben|gerade|vorhin)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HeutePattern = new(@"\bheute\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public TemporalReferenceService(
        AppDbContext context,
        ILogger<TemporalReferenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TemporalParseResult> ParseTemporalExpressionAsync(string text, DateTime? referenceDate = null)
    {
        var result = new TemporalParseResult { Success = true };
        var refDate = referenceDate ?? DateTime.Now;

        try
        {
            // Check for "heute morgen/nachmittag/etc."
            var heuteMorgenMatch = HeuteMorgenPattern.Match(text);
            if (heuteMorgenMatch.Success)
            {
                var timeOfDay = heuteMorgenMatch.Groups[1].Value.ToLower();
                var range = ResolveTimeOfDay(timeOfDay, refDate.Date);
                if (range.HasValue)
                {
                    result.References.Add(new TemporalReference
                    {
                        OriginalExpression = heuteMorgenMatch.Value,
                        ResolvedStart = range.Value.Start,
                        ResolvedEnd = range.Value.End,
                        Type = TemporalType.Contextual,
                        Confidence = 0.95,
                        StartPosition = heuteMorgenMatch.Index,
                        EndPosition = heuteMorgenMatch.Index + heuteMorgenMatch.Length
                    });
                }
            }

            // Check for simple "heute"
            if (!heuteMorgenMatch.Success)
            {
                var heuteMatch = HeutePattern.Match(text);
                if (heuteMatch.Success)
                {
                    result.References.Add(new TemporalReference
                    {
                        OriginalExpression = heuteMatch.Value,
                        ResolvedStart = refDate.Date,
                        ResolvedEnd = refDate.Date.AddDays(1).AddTicks(-1),
                        Type = TemporalType.Contextual,
                        Confidence = 0.9,
                        StartPosition = heuteMatch.Index,
                        EndPosition = heuteMatch.Index + heuteMatch.Length
                    });
                }
            }

            // Check for "gestern"
            var gesternMatch = GesternPattern.Match(text);
            if (gesternMatch.Success)
            {
                var yesterday = refDate.Date.AddDays(-1);
                DateTime start, end;

                if (gesternMatch.Groups[1].Success)
                {
                    var timeOfDay = gesternMatch.Groups[1].Value.ToLower();
                    var range = ResolveTimeOfDay(timeOfDay, yesterday);
                    if (range.HasValue)
                    {
                        start = range.Value.Start;
                        end = range.Value.End;
                    }
                    else
                    {
                        start = yesterday;
                        end = yesterday.AddDays(1).AddTicks(-1);
                    }
                }
                else
                {
                    start = yesterday;
                    end = yesterday.AddDays(1).AddTicks(-1);
                }

                result.References.Add(new TemporalReference
                {
                    OriginalExpression = gesternMatch.Value,
                    ResolvedStart = start,
                    ResolvedEnd = end,
                    Type = TemporalType.Contextual,
                    Confidence = 0.95,
                    StartPosition = gesternMatch.Index,
                    EndPosition = gesternMatch.Index + gesternMatch.Length
                });
            }

            // Check for "vor X Tagen/Wochen"
            var vorXMatch = VorXTagenPattern.Match(text);
            if (vorXMatch.Success)
            {
                var number = int.Parse(vorXMatch.Groups[1].Value);
                var unit = vorXMatch.Groups[2].Value.ToLower();
                var targetDate = unit switch
                {
                    "tag" or "tagen" => refDate.Date.AddDays(-number),
                    "woche" or "wochen" => refDate.Date.AddDays(-number * 7),
                    "monat" or "monaten" => refDate.Date.AddMonths(-number),
                    _ => refDate.Date
                };

                result.References.Add(new TemporalReference
                {
                    OriginalExpression = vorXMatch.Value,
                    ResolvedStart = targetDate,
                    ResolvedEnd = targetDate.AddDays(1).AddTicks(-1),
                    Type = TemporalType.Relative,
                    Confidence = 0.9,
                    StartPosition = vorXMatch.Index,
                    EndPosition = vorXMatch.Index + vorXMatch.Length
                });
            }

            // Check for "in X Tagen/Wochen"
            var inXMatch = InXTagenPattern.Match(text);
            if (inXMatch.Success)
            {
                var number = int.Parse(inXMatch.Groups[1].Value);
                var unit = inXMatch.Groups[2].Value.ToLower();
                var targetDate = unit switch
                {
                    "tag" or "tagen" => refDate.Date.AddDays(number),
                    "woche" or "wochen" => refDate.Date.AddDays(number * 7),
                    "monat" or "monaten" => refDate.Date.AddMonths(number),
                    _ => refDate.Date
                };

                result.References.Add(new TemporalReference
                {
                    OriginalExpression = inXMatch.Value,
                    ResolvedStart = targetDate,
                    ResolvedEnd = targetDate.AddDays(1).AddTicks(-1),
                    Type = TemporalType.Relative,
                    Confidence = 0.9,
                    StartPosition = inXMatch.Index,
                    EndPosition = inXMatch.Index + inXMatch.Length
                });
            }

            // Check for "letzte Woche/Montag/etc."
            var letzteMatch = LetzteWochePattern.Match(text);
            if (letzteMatch.Success)
            {
                var target = letzteMatch.Groups[2].Value.ToLower();
                DateTime start, end;

                if (target == "woche")
                {
                    var startOfThisWeek = refDate.Date.AddDays(-(int)refDate.DayOfWeek + (int)DayOfWeek.Monday);
                    start = startOfThisWeek.AddDays(-7);
                    end = start.AddDays(7).AddTicks(-1);
                }
                else if (GermanWeekdays.TryGetValue(target, out var dayOfWeek))
                {
                    var targetDay = FindWeekday(dayOfWeek, refDate, findNext: false);
                    start = targetDay;
                    end = targetDay.AddDays(1).AddTicks(-1);
                }
                else
                {
                    start = refDate.Date;
                    end = refDate.Date.AddDays(1).AddTicks(-1);
                }

                result.References.Add(new TemporalReference
                {
                    OriginalExpression = letzteMatch.Value,
                    ResolvedStart = start,
                    ResolvedEnd = end,
                    Type = TemporalType.Contextual,
                    Confidence = 0.85,
                    StartPosition = letzteMatch.Index,
                    EndPosition = letzteMatch.Index + letzteMatch.Length
                });
            }

            // Check for "diese Woche/Montag/etc."
            var dieseMatch = DieseWochePattern.Match(text);
            if (dieseMatch.Success)
            {
                var target = dieseMatch.Groups[2].Value.ToLower();
                DateTime start, end;

                if (target == "woche")
                {
                    start = refDate.Date.AddDays(-(int)refDate.DayOfWeek + (int)DayOfWeek.Monday);
                    if (refDate.DayOfWeek == DayOfWeek.Sunday) start = start.AddDays(-7);
                    end = start.AddDays(7).AddTicks(-1);
                }
                else if (GermanWeekdays.TryGetValue(target, out var dayOfWeek))
                {
                    var startOfWeek = refDate.Date.AddDays(-(int)refDate.DayOfWeek + (int)DayOfWeek.Monday);
                    if (refDate.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-7);
                    var daysToAdd = ((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                    start = startOfWeek.AddDays(daysToAdd);
                    end = start.AddDays(1).AddTicks(-1);
                }
                else
                {
                    start = refDate.Date;
                    end = refDate.Date.AddDays(1).AddTicks(-1);
                }

                result.References.Add(new TemporalReference
                {
                    OriginalExpression = dieseMatch.Value,
                    ResolvedStart = start,
                    ResolvedEnd = end,
                    Type = TemporalType.Contextual,
                    Confidence = 0.9,
                    StartPosition = dieseMatch.Index,
                    EndPosition = dieseMatch.Index + dieseMatch.Length
                });
            }

            // Check for "naechste Woche/Montag/etc."
            var naechsteMatch = NaechsteWochePattern.Match(text);
            if (naechsteMatch.Success)
            {
                var target = naechsteMatch.Groups[2].Value.ToLower();
                DateTime start, end;

                if (target == "woche")
                {
                    var startOfThisWeek = refDate.Date.AddDays(-(int)refDate.DayOfWeek + (int)DayOfWeek.Monday);
                    if (refDate.DayOfWeek == DayOfWeek.Sunday) startOfThisWeek = startOfThisWeek.AddDays(-7);
                    start = startOfThisWeek.AddDays(7);
                    end = start.AddDays(7).AddTicks(-1);
                }
                else if (GermanWeekdays.TryGetValue(target, out var dayOfWeek))
                {
                    var targetDay = FindWeekday(dayOfWeek, refDate, findNext: true);
                    start = targetDay;
                    end = targetDay.AddDays(1).AddTicks(-1);
                }
                else
                {
                    start = refDate.Date;
                    end = refDate.Date.AddDays(1).AddTicks(-1);
                }

                result.References.Add(new TemporalReference
                {
                    OriginalExpression = naechsteMatch.Value,
                    ResolvedStart = start,
                    ResolvedEnd = end,
                    Type = TemporalType.Contextual,
                    Confidence = 0.85,
                    StartPosition = naechsteMatch.Index,
                    EndPosition = naechsteMatch.Index + naechsteMatch.Length
                });
            }

            // Check for "am Montag/etc."
            var amMatch = AmWochentagPattern.Match(text);
            if (amMatch.Success)
            {
                var weekdayName = amMatch.Groups[1].Value;
                if (GermanWeekdays.TryGetValue(weekdayName, out var dayOfWeek))
                {
                    var targetDay = FindWeekday(dayOfWeek, refDate, findNext: true);
                    result.References.Add(new TemporalReference
                    {
                        OriginalExpression = amMatch.Value,
                        ResolvedStart = targetDay,
                        ResolvedEnd = targetDay.AddDays(1).AddTicks(-1),
                        Type = TemporalType.Contextual,
                        Confidence = 0.8,
                        StartPosition = amMatch.Index,
                        EndPosition = amMatch.Index + amMatch.Length
                    });
                }
            }

            // Check for "der Termin von eben"
            var terminMatch = TerminVonEbenPattern.Match(text);
            if (terminMatch.Success)
            {
                result.References.Add(new TemporalReference
                {
                    OriginalExpression = terminMatch.Value,
                    ResolvedStart = refDate.AddHours(-4),
                    ResolvedEnd = refDate,
                    Type = TemporalType.RecentEvent,
                    Confidence = 0.7,
                    StartPosition = terminMatch.Index,
                    EndPosition = terminMatch.Index + terminMatch.Length
                });
            }

            // Calculate overall confidence
            if (result.References.Count > 0)
            {
                result.OverallConfidence = result.References.Average(r => r.Confidence);
            }
            else
            {
                result.Success = false;
                result.Error = "Keine Zeitreferenz erkannt";
            }

            _logger.LogDebug("Parsed temporal expressions from text. Found {Count} references", result.References.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing temporal expression: {Text}", text);
            result.Success = false;
            result.Error = ex.Message;
        }

        return await Task.FromResult(result);
    }

    public async Task<List<CalendarEvent>> FindEventsInTimeRangeAsync(int userId, DateTime start, DateTime end)
    {
        return await _context.CalendarEvents
            .Where(e => e.UserId == userId)
            .Where(e => e.StartTime >= start && e.StartTime <= end)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<CalendarEvent?> ResolveMostRecentEventAsync(int userId, string? context = null, DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? DateTime.Now;

        var query = _context.CalendarEvents
            .Where(e => e.UserId == userId)
            .Where(e => e.EndTime <= refDate)
            .OrderByDescending(e => e.EndTime);

        if (!string.IsNullOrWhiteSpace(context))
        {
            var contextLower = context.ToLower();
            query = (IOrderedQueryable<CalendarEvent>)query
                .Where(e =>
                    (e.Professor != null && e.Professor.ToLower().Contains(contextLower)) ||
                    (e.Subject != null && e.Subject.ToLower().Contains(contextLower)) ||
                    e.Title.ToLower().Contains(contextLower));
        }

        return await query.FirstOrDefaultAsync();
    }

    public (DateTime Start, DateTime End)? ResolveTimeOfDay(string expression, DateTime date)
    {
        var normalizedExpression = expression.ToLower()
            .Replace("ae", "a").Replace("oe", "o").Replace("ue", "u")
            .Replace("ä", "a").Replace("ö", "o").Replace("ü", "u");

        if (TimeOfDayMappings.TryGetValue(normalizedExpression, out var range))
        {
            var start = date.Date.Add(range.Start);
            var end = date.Date.Add(range.End);

            // Handle night spanning to next day
            if (range.End < range.Start)
            {
                end = date.Date.AddDays(1).Add(range.End);
            }

            return (start, end);
        }

        // Try without normalization
        if (TimeOfDayMappings.TryGetValue(expression, out range))
        {
            var start = date.Date.Add(range.Start);
            var end = date.Date.Add(range.End);
            if (range.End < range.Start)
            {
                end = date.Date.AddDays(1).Add(range.End);
            }
            return (start, end);
        }

        return null;
    }

    public DayOfWeek? ParseGermanWeekday(string weekday)
    {
        var normalized = weekday.ToLower().Trim()
            .Replace("ae", "a").Replace("oe", "o").Replace("ue", "u")
            .Replace("ä", "a").Replace("ö", "o").Replace("ü", "u");

        return GermanWeekdays.TryGetValue(normalized, out var dayOfWeek) ? dayOfWeek : null;
    }

    public DateTime FindWeekday(DayOfWeek weekday, DateTime referenceDate, bool findNext = true)
    {
        var daysUntilTarget = ((int)weekday - (int)referenceDate.DayOfWeek + 7) % 7;

        if (findNext)
        {
            if (daysUntilTarget == 0)
            {
                daysUntilTarget = 7; // Next week's same day
            }
            return referenceDate.Date.AddDays(daysUntilTarget);
        }
        else
        {
            var daysSinceTarget = ((int)referenceDate.DayOfWeek - (int)weekday + 7) % 7;
            if (daysSinceTarget == 0)
            {
                daysSinceTarget = 7; // Last week's same day
            }
            return referenceDate.Date.AddDays(-daysSinceTarget);
        }
    }
}
