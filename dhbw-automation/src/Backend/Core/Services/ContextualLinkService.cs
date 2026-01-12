using System.Text.RegularExpressions;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for analyzing text and creating contextual links to calendar events
/// </summary>
public class ContextualLinkService : IContextualLinkService
{
    private readonly AppDbContext _context;
    private readonly ITemporalReferenceService _temporalService;
    private readonly ILogger<ContextualLinkService> _logger;

    private const double HighConfidenceThreshold = 0.8;
    private const double MediumConfidenceThreshold = 0.5;

    // Regex patterns for professor detection
    private static readonly Regex ProfessorPattern = new(
        @"\b(Prof\.?|Professor|Dozent|Dozentin|Herr|Frau)\s+([A-ZÄÖÜ][a-zäöüß]+(?:\s+[A-ZÄÖÜ][a-zäöüß]+)?)\b",
        RegexOptions.Compiled);

    private static readonly Regex ProfVonPattern = new(
        @"\b(der\s+)?Prof(\.?|essor)?\s+(von\s+)?(heute|gestern|letzte|diese|naechste)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Pattern for course codes (e.g., WDS125, INF123)
    private static readonly Regex CourseCodePattern = new(
        @"\b([A-Z]{2,4}\d{2,4})\b",
        RegexOptions.Compiled);

    public ContextualLinkService(
        AppDbContext context,
        ITemporalReferenceService temporalService,
        ILogger<ContextualLinkService> logger)
    {
        _context = context;
        _temporalService = temporalService;
        _logger = logger;
    }

    public async Task<ContextualAnalysisResult> AnalyzeTextAsync(string text, int userId, DateTime? referenceDate = null)
    {
        var result = new ContextualAnalysisResult();
        var refDate = referenceDate ?? DateTime.Now;

        try
        {
            // Build indices for efficient matching
            var professorIndex = await BuildProfessorIndexAsync(userId);
            var subjectIndex = await BuildSubjectIndexAsync(userId);

            // 1. Detect professor references
            await DetectProfessorReferencesAsync(text, userId, professorIndex, result, refDate);

            // 2. Detect subject/course references
            DetectSubjectReferences(text, subjectIndex, result);

            // 3. Detect course codes
            DetectCourseCodes(text, subjectIndex, result);

            // 4. Detect temporal references and resolve to events
            await DetectTemporalEventReferencesAsync(text, userId, result, refDate);

            // 5. Detect combined "Prof von heute morgen" patterns
            await DetectProfessorTemporalReferencesAsync(text, userId, professorIndex, result, refDate);

            // Calculate overall confidence
            if (result.References.Count > 0)
            {
                result.OverallConfidence = result.References.Average(r => r.Confidence);
            }

            _logger.LogDebug("Analyzed text for user {UserId}. Found {RefCount} references, {LinkCount} suggested links",
                userId, result.References.Count, result.SuggestedLinks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing text for contextual references");
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task DetectProfessorReferencesAsync(
        string text,
        int userId,
        Dictionary<string, ProfessorInfo> professorIndex,
        ContextualAnalysisResult result,
        DateTime refDate)
    {
        var matches = ProfessorPattern.Matches(text);
        foreach (Match match in matches)
        {
            var name = match.Groups[2].Value;
            var fullMatch = match.Value;

            // Try to match against known professors
            var matchedProf = FindMatchingProfessor(name, professorIndex);

            if (matchedProf != null)
            {
                var reference = new ContextualReference
                {
                    Type = ContextualReferenceType.Professor,
                    OriginalText = fullMatch,
                    NormalizedValue = matchedProf.Name,
                    ResolvedEntityId = matchedProf.MostRecentEventId,
                    ResolvedEntityType = KnowledgeEntityTypes.CalendarEvent,
                    Confidence = 0.9,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Metadata = new Dictionary<string, object>
                    {
                        { "subjects", matchedProf.Subjects },
                        { "eventCount", matchedProf.EventIds.Count }
                    }
                };
                result.References.Add(reference);

                // Add suggested link if we have a resolved event
                if (matchedProf.MostRecentEventId.HasValue)
                {
                    var ev = await _context.CalendarEvents.FindAsync(matchedProf.MostRecentEventId.Value);
                    result.SuggestedLinks.Add(new SuggestedLink
                    {
                        SourceType = KnowledgeEntityTypes.Note,
                        TargetType = KnowledgeEntityTypes.CalendarEvent,
                        TargetId = matchedProf.MostRecentEventId.Value,
                        LinkType = SmartReferenceLinkTypes.ProfessorReference,
                        Confidence = 0.85,
                        Reason = $"Erwähnung von {matchedProf.Name}",
                        ReferenceText = fullMatch,
                        TargetDisplayName = ev?.Title
                    });
                }
            }
            else
            {
                // Unknown professor, lower confidence
                result.References.Add(new ContextualReference
                {
                    Type = ContextualReferenceType.Professor,
                    OriginalText = fullMatch,
                    NormalizedValue = name,
                    Confidence = 0.5,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length
                });
            }
        }
    }

    private void DetectSubjectReferences(
        string text,
        Dictionary<string, SubjectInfo> subjectIndex,
        ContextualAnalysisResult result)
    {
        foreach (var (subjectName, info) in subjectIndex)
        {
            var pattern = new Regex($@"\b{Regex.Escape(subjectName)}\b", RegexOptions.IgnoreCase);
            var match = pattern.Match(text);

            if (match.Success)
            {
                result.References.Add(new ContextualReference
                {
                    Type = ContextualReferenceType.Subject,
                    OriginalText = match.Value,
                    NormalizedValue = info.Name,
                    ResolvedEntityId = info.EventIds.FirstOrDefault(),
                    ResolvedEntityType = KnowledgeEntityTypes.CalendarEvent,
                    Confidence = 0.85,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Metadata = new Dictionary<string, object>
                    {
                        { "courseCode", info.CourseCode ?? "" },
                        { "professors", info.Professors }
                    }
                });

                if (info.EventIds.Any())
                {
                    result.SuggestedLinks.Add(new SuggestedLink
                    {
                        SourceType = KnowledgeEntityTypes.Note,
                        TargetType = KnowledgeEntityTypes.CalendarEvent,
                        TargetId = info.EventIds.First(),
                        LinkType = SmartReferenceLinkTypes.SubjectReference,
                        Confidence = 0.8,
                        Reason = $"Erwähnung von Fach: {info.Name}",
                        ReferenceText = match.Value,
                        TargetDisplayName = info.Name
                    });
                }
            }
        }
    }

    private void DetectCourseCodes(
        string text,
        Dictionary<string, SubjectInfo> subjectIndex,
        ContextualAnalysisResult result)
    {
        var matches = CourseCodePattern.Matches(text);
        foreach (Match match in matches)
        {
            var code = match.Groups[1].Value;

            // Check if course code matches any known subject
            var matchedSubject = subjectIndex.Values
                .FirstOrDefault(s => s.CourseCode?.Equals(code, StringComparison.OrdinalIgnoreCase) == true);

            if (matchedSubject != null)
            {
                result.References.Add(new ContextualReference
                {
                    Type = ContextualReferenceType.CourseCode,
                    OriginalText = match.Value,
                    NormalizedValue = matchedSubject.Name,
                    ResolvedEntityId = matchedSubject.EventIds.FirstOrDefault(),
                    ResolvedEntityType = KnowledgeEntityTypes.CalendarEvent,
                    Confidence = 0.95,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length
                });
            }
        }
    }

    private async Task DetectTemporalEventReferencesAsync(
        string text,
        int userId,
        ContextualAnalysisResult result,
        DateTime refDate)
    {
        var temporalResult = await _temporalService.ParseTemporalExpressionAsync(text, refDate);

        if (temporalResult.Success)
        {
            foreach (var tempRef in temporalResult.References)
            {
                // Find events in the time range
                var events = await _temporalService.FindEventsInTimeRangeAsync(
                    userId, tempRef.ResolvedStart, tempRef.ResolvedEnd);

                if (events.Any())
                {
                    var firstEvent = events.First();
                    result.References.Add(new ContextualReference
                    {
                        Type = ContextualReferenceType.TemporalEvent,
                        OriginalText = tempRef.OriginalExpression,
                        NormalizedValue = $"{tempRef.ResolvedStart:dd.MM.yyyy HH:mm} - {tempRef.ResolvedEnd:HH:mm}",
                        ResolvedEntityId = firstEvent.Id,
                        ResolvedEntityType = KnowledgeEntityTypes.CalendarEvent,
                        Confidence = tempRef.Confidence * 0.9,
                        StartPosition = tempRef.StartPosition,
                        EndPosition = tempRef.EndPosition,
                        Metadata = new Dictionary<string, object>
                        {
                            { "eventCount", events.Count },
                            { "eventTitles", events.Select(e => e.Title).ToList() }
                        }
                    });

                    result.SuggestedLinks.Add(new SuggestedLink
                    {
                        SourceType = KnowledgeEntityTypes.Note,
                        TargetType = KnowledgeEntityTypes.CalendarEvent,
                        TargetId = firstEvent.Id,
                        LinkType = SmartReferenceLinkTypes.TemporalReference,
                        Confidence = tempRef.Confidence * 0.85,
                        Reason = $"Zeitreferenz: {tempRef.OriginalExpression}",
                        ReferenceText = tempRef.OriginalExpression,
                        TargetDisplayName = firstEvent.Title
                    });
                }
            }
        }
    }

    private async Task DetectProfessorTemporalReferencesAsync(
        string text,
        int userId,
        Dictionary<string, ProfessorInfo> professorIndex,
        ContextualAnalysisResult result,
        DateTime refDate)
    {
        var match = ProfVonPattern.Match(text);
        if (!match.Success) return;

        // Parse the temporal part
        var temporalPart = text.Substring(match.Index);
        var temporalResult = await _temporalService.ParseTemporalExpressionAsync(temporalPart, refDate);

        if (!temporalResult.Success || !temporalResult.References.Any()) return;

        var timeRange = temporalResult.References.First();
        var events = await _temporalService.FindEventsInTimeRangeAsync(
            userId, timeRange.ResolvedStart, timeRange.ResolvedEnd);

        // Find event with a professor
        var eventWithProf = events.FirstOrDefault(e => !string.IsNullOrEmpty(e.Professor));

        if (eventWithProf != null)
        {
            result.References.Add(new ContextualReference
            {
                Type = ContextualReferenceType.ProfessorTemporal,
                OriginalText = match.Value,
                NormalizedValue = eventWithProf.Professor!,
                ResolvedEntityId = eventWithProf.Id,
                ResolvedEntityType = KnowledgeEntityTypes.CalendarEvent,
                Confidence = 0.9,
                StartPosition = match.Index,
                EndPosition = match.Index + match.Length,
                Metadata = new Dictionary<string, object>
                {
                    { "professorName", eventWithProf.Professor! },
                    { "eventTitle", eventWithProf.Title },
                    { "eventTime", eventWithProf.StartTime }
                }
            });

            result.SuggestedLinks.Add(new SuggestedLink
            {
                SourceType = KnowledgeEntityTypes.Note,
                TargetType = KnowledgeEntityTypes.CalendarEvent,
                TargetId = eventWithProf.Id,
                LinkType = SmartReferenceLinkTypes.ProfessorTemporalReference,
                Confidence = 0.9,
                Reason = $"Prof. {eventWithProf.Professor} von {timeRange.OriginalExpression}",
                ReferenceText = match.Value,
                TargetDisplayName = $"{eventWithProf.Title} ({eventWithProf.Professor})"
            });
        }
    }

    public async Task<List<CalendarEvent>> FindEventsByProfessorAsync(int userId, string professorName)
    {
        var normalizedName = professorName.ToLower().Trim();

        return await _context.CalendarEvents
            .Where(e => e.UserId == userId)
            .Where(e => e.Professor != null && e.Professor.ToLower().Contains(normalizedName))
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<List<CalendarEvent>> FindEventsBySubjectAsync(int userId, string subject)
    {
        var normalizedSubject = subject.ToLower().Trim();

        return await _context.CalendarEvents
            .Where(e => e.UserId == userId)
            .Where(e =>
                (e.Subject != null && e.Subject.ToLower().Contains(normalizedSubject)) ||
                e.Title.ToLower().Contains(normalizedSubject))
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<CalendarEvent?> ResolveEventReferenceAsync(int userId, string reference, DateTime? referenceDate = null)
    {
        var analysisResult = await AnalyzeTextAsync(reference, userId, referenceDate);

        // Find the highest confidence reference with a resolved entity
        var bestReference = analysisResult.References
            .Where(r => r.ResolvedEntityId.HasValue)
            .OrderByDescending(r => r.Confidence)
            .FirstOrDefault();

        if (bestReference?.ResolvedEntityId != null)
        {
            return await _context.CalendarEvents.FindAsync(bestReference.ResolvedEntityId);
        }

        return null;
    }

    public async Task<int> AutoLinkNoteToEventsAsync(int userId, int eventId, string noteContent, bool autoConfirmHighConfidence = true)
    {
        var suggestions = await GetSuggestedLinksForNoteAsync(userId, noteContent, eventId);
        var linksCreated = 0;

        foreach (var suggestion in suggestions)
        {
            // Skip self-linking
            if (suggestion.TargetType == KnowledgeEntityTypes.CalendarEvent && suggestion.TargetId == eventId)
                continue;

            // Only auto-create high confidence links
            if (!autoConfirmHighConfidence || suggestion.Confidence < HighConfidenceThreshold)
                continue;

            // Set source to the event containing the note
            suggestion.SourceType = KnowledgeEntityTypes.CalendarEvent;
            suggestion.SourceId = eventId;

            await ConfirmSuggestedLinkAsync(userId, suggestion);
            linksCreated++;
        }

        _logger.LogInformation("Auto-linked note from event {EventId}. Created {Count} links", eventId, linksCreated);
        return linksCreated;
    }

    public async Task<List<SuggestedLink>> GetSuggestedLinksForNoteAsync(int userId, string noteContent, int? sourceEventId = null)
    {
        var analysisResult = await AnalyzeTextAsync(noteContent, userId);

        // Filter out self-references if sourceEventId is provided
        if (sourceEventId.HasValue)
        {
            analysisResult.SuggestedLinks = analysisResult.SuggestedLinks
                .Where(s => !(s.TargetType == KnowledgeEntityTypes.CalendarEvent && s.TargetId == sourceEventId))
                .ToList();
        }

        return analysisResult.SuggestedLinks;
    }

    public async Task<Dictionary<string, ProfessorInfo>> BuildProfessorIndexAsync(int userId)
    {
        var events = await _context.CalendarEvents
            .Where(e => e.UserId == userId)
            .Where(e => e.Professor != null && e.Professor != "")
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();

        var index = new Dictionary<string, ProfessorInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var ev in events)
        {
            var profName = ev.Professor!.Trim();
            var key = profName.ToLower();

            if (!index.ContainsKey(key))
            {
                index[key] = new ProfessorInfo
                {
                    Name = profName,
                    Variations = GenerateNameVariations(profName),
                    Subjects = new List<string>(),
                    EventIds = new List<int>(),
                    MostRecentEventId = ev.Id
                };
            }

            var info = index[key];
            info.EventIds.Add(ev.Id);

            if (!string.IsNullOrEmpty(ev.Subject) && !info.Subjects.Contains(ev.Subject))
            {
                info.Subjects.Add(ev.Subject);
            }
        }

        return index;
    }

    public async Task<Dictionary<string, SubjectInfo>> BuildSubjectIndexAsync(int userId)
    {
        var events = await _context.CalendarEvents
            .Where(e => e.UserId == userId)
            .Where(e => e.Subject != null && e.Subject != "")
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();

        var index = new Dictionary<string, SubjectInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var ev in events)
        {
            var subjectName = ev.Subject!.Trim();
            var key = subjectName.ToLower();

            if (!index.ContainsKey(key))
            {
                // Try to extract course code from title
                var codeMatch = CourseCodePattern.Match(ev.Title);
                var courseCode = codeMatch.Success ? codeMatch.Groups[1].Value : null;

                index[key] = new SubjectInfo
                {
                    Name = subjectName,
                    CourseCode = courseCode,
                    Professors = new List<string>(),
                    EventIds = new List<int>()
                };
            }

            var info = index[key];
            info.EventIds.Add(ev.Id);

            if (!string.IsNullOrEmpty(ev.Professor) && !info.Professors.Contains(ev.Professor))
            {
                info.Professors.Add(ev.Professor);
            }
        }

        return index;
    }

    public async Task<KnowledgeLink> ConfirmSuggestedLinkAsync(int userId, SuggestedLink suggestion)
    {
        // Check if link already exists
        var existing = await _context.KnowledgeLinks
            .FirstOrDefaultAsync(l =>
                l.SourceType == suggestion.SourceType &&
                l.SourceId == suggestion.SourceId &&
                l.TargetType == suggestion.TargetType &&
                l.TargetId == suggestion.TargetId);

        if (existing != null)
        {
            _logger.LogDebug("Link already exists: {SourceType}:{SourceId} -> {TargetType}:{TargetId}",
                suggestion.SourceType, suggestion.SourceId, suggestion.TargetType, suggestion.TargetId);
            return existing;
        }

        var link = new KnowledgeLink
        {
            UserId = userId,
            SourceType = suggestion.SourceType,
            SourceId = suggestion.SourceId,
            TargetType = suggestion.TargetType,
            TargetId = suggestion.TargetId,
            LinkType = suggestion.LinkType,
            Strength = suggestion.Confidence,
            IsAutoGenerated = true,
            IsConfirmed = true,
            IsBidirectional = true,
            Description = suggestion.Reason
        };

        _context.KnowledgeLinks.Add(link);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created smart reference link: {SourceType}:{SourceId} -> {TargetType}:{TargetId} ({Reason})",
            suggestion.SourceType, suggestion.SourceId, suggestion.TargetType, suggestion.TargetId, suggestion.Reason);

        return link;
    }

    public async Task<List<KnowledgeLink>> GetLinksForEventAsync(int eventId, int userId)
    {
        return await _context.KnowledgeLinks
            .Where(l => l.UserId == userId)
            .Where(l =>
                (l.SourceType == KnowledgeEntityTypes.CalendarEvent && l.SourceId == eventId) ||
                (l.TargetType == KnowledgeEntityTypes.CalendarEvent && l.TargetId == eventId))
            .Where(l => !l.IsRejected)
            .ToListAsync();
    }

    private ProfessorInfo? FindMatchingProfessor(string name, Dictionary<string, ProfessorInfo> index)
    {
        var normalizedName = name.ToLower().Trim();

        // Direct match
        if (index.TryGetValue(normalizedName, out var directMatch))
        {
            return directMatch;
        }

        // Check variations
        foreach (var (_, info) in index)
        {
            if (info.Variations.Any(v => v.Contains(normalizedName) || normalizedName.Contains(v)))
            {
                return info;
            }
        }

        // Partial match (last name only)
        foreach (var (key, info) in index)
        {
            var lastName = info.Name.Split(' ').LastOrDefault()?.ToLower();
            if (lastName != null && (lastName.Contains(normalizedName) || normalizedName.Contains(lastName)))
            {
                return info;
            }
        }

        return null;
    }

    private List<string> GenerateNameVariations(string fullName)
    {
        var variations = new List<string> { fullName.ToLower() };

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            // Last name only
            variations.Add(parts[^1].ToLower());

            // First name + last name
            variations.Add($"{parts[0]} {parts[^1]}".ToLower());
        }

        // With/without titles
        var withoutTitle = Regex.Replace(fullName, @"(Prof\.?|Dr\.?|Professor)\s*", "", RegexOptions.IgnoreCase);
        if (withoutTitle != fullName)
        {
            variations.Add(withoutTitle.ToLower().Trim());
        }

        return variations.Distinct().ToList();
    }
}
