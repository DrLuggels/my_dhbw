using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for analyzing text and creating contextual links to calendar events
/// </summary>
public interface IContextualLinkService
{
    /// <summary>
    /// Analyzes text for contextual references (professors, subjects, temporal expressions)
    /// </summary>
    /// <param name="text">Text to analyze</param>
    /// <param name="userId">User ID for context</param>
    /// <param name="referenceDate">Reference date for temporal expressions</param>
    /// <returns>Analysis result with detected references and suggested links</returns>
    Task<ContextualAnalysisResult> AnalyzeTextAsync(string text, int userId, DateTime? referenceDate = null);

    /// <summary>
    /// Finds calendar events by professor name
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="professorName">Professor name (partial match supported)</param>
    /// <returns>List of matching calendar events</returns>
    Task<List<CalendarEvent>> FindEventsByProfessorAsync(int userId, string professorName);

    /// <summary>
    /// Finds calendar events by subject name
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="subject">Subject name (partial match supported)</param>
    /// <returns>List of matching calendar events</returns>
    Task<List<CalendarEvent>> FindEventsBySubjectAsync(int userId, string subject);

    /// <summary>
    /// Resolves a natural language event reference to a specific calendar event
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="reference">Natural language reference (e.g., "der Prof von heute morgen")</param>
    /// <param name="referenceDate">Reference date for temporal expressions</param>
    /// <returns>Resolved calendar event or null</returns>
    Task<CalendarEvent?> ResolveEventReferenceAsync(int userId, string reference, DateTime? referenceDate = null);

    /// <summary>
    /// Automatically links a note (from calendar event) to related events based on content
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="eventId">Calendar event ID containing the note</param>
    /// <param name="noteContent">Note content to analyze</param>
    /// <param name="autoConfirmHighConfidence">Auto-confirm links with confidence >= 0.8</param>
    /// <returns>Number of links created</returns>
    Task<int> AutoLinkNoteToEventsAsync(int userId, int eventId, string noteContent, bool autoConfirmHighConfidence = true);

    /// <summary>
    /// Gets suggested links for note content without creating them
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="noteContent">Note content to analyze</param>
    /// <param name="sourceEventId">Optional: Event ID if note is from a calendar event</param>
    /// <returns>List of suggested links</returns>
    Task<List<SuggestedLink>> GetSuggestedLinksForNoteAsync(int userId, string noteContent, int? sourceEventId = null);

    /// <summary>
    /// Builds a professor index from calendar events for efficient matching
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Dictionary of professor names to their info</returns>
    Task<Dictionary<string, ProfessorInfo>> BuildProfessorIndexAsync(int userId);

    /// <summary>
    /// Builds a subject index from calendar events for efficient matching
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Dictionary of subject names to their info</returns>
    Task<Dictionary<string, SubjectInfo>> BuildSubjectIndexAsync(int userId);

    /// <summary>
    /// Confirms a suggested link (creates it as a KnowledgeLink)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="suggestion">The suggested link to confirm</param>
    /// <returns>Created KnowledgeLink</returns>
    Task<KnowledgeLink> ConfirmSuggestedLinkAsync(int userId, SuggestedLink suggestion);

    /// <summary>
    /// Gets all links related to a specific calendar event
    /// </summary>
    /// <param name="eventId">Calendar event ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>List of related knowledge links</returns>
    Task<List<KnowledgeLink>> GetLinksForEventAsync(int eventId, int userId);
}
