using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service für AI-Datenvalidierung mit Staging-System.
/// Ermöglicht Rückfragen bei unklaren Daten bevor diese in die Produktiv-DB geschrieben werden.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Erstellt Staging-Entitäten aus einem DocumentIntent
    /// </summary>
    /// <param name="intent">Der analysierte Document Intent mit extrahierten Entitäten</param>
    /// <param name="userId">User ID</param>
    /// <param name="documentId">Source Document ID</param>
    /// <returns>Liste der erstellten Staging-Entitäten</returns>
    Task<List<StagedEntity>> StageEntitiesAsync(DocumentIntent intent, int userId, int? documentId = null);

    /// <summary>
    /// Holt alle ausstehenden Staging-Entitäten für einen User
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional: Filter nach Status</param>
    /// <returns>Liste von Staging-Entitäten mit Fragen</returns>
    Task<List<StagedEntity>> GetPendingStagedEntitiesAsync(int userId, string? status = null);

    /// <summary>
    /// Beantwortet AI-Fragen für eine Staging-Entität
    /// </summary>
    /// <param name="stagedEntityId">ID der Staging-Entität</param>
    /// <param name="userId">User ID für Autorisierung</param>
    /// <param name="answers">Dictionary: FieldName -> UserAnswer</param>
    /// <returns>True wenn erfolgreich</returns>
    Task<bool> AnswerQuestionsAsync(int stagedEntityId, int userId, Dictionary<string, string> answers);

    /// <summary>
    /// Bestätigt eine Staging-Entität und überträgt sie in die Produktiv-DB
    /// </summary>
    /// <param name="stagedEntityId">ID der Staging-Entität</param>
    /// <param name="userId">User ID für Autorisierung</param>
    /// <param name="userNotes">Optional: Notizen/Korrekturen des Users</param>
    /// <returns>ID der erstellten Produktiv-Entität (z.B. Todo.Id, CalendarEvent.Id)</returns>
    Task<int?> ConfirmAndPromoteAsync(int stagedEntityId, int userId, string? userNotes = null);

    /// <summary>
    /// Lehnt eine Staging-Entität ab (wird nicht in Produktiv-DB übertragen)
    /// </summary>
    /// <param name="stagedEntityId">ID der Staging-Entität</param>
    /// <param name="userId">User ID für Autorisierung</param>
    /// <param name="reason">Grund der Ablehnung</param>
    /// <returns>True wenn erfolgreich</returns>
    Task<bool> RejectStagedEntityAsync(int stagedEntityId, int userId, string? reason = null);

    /// <summary>
    /// Ändert die Daten einer Staging-Entität (User-Korrektur)
    /// </summary>
    /// <param name="stagedEntityId">ID der Staging-Entität</param>
    /// <param name="userId">User ID für Autorisierung</param>
    /// <param name="modifiedData">Geänderte JSON-Daten</param>
    /// <returns>True wenn erfolgreich</returns>
    Task<bool> ModifyStagedEntityAsync(int stagedEntityId, int userId, string modifiedData);

    /// <summary>
    /// Statistiken über Staging-Qualität (für Monitoring)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Zeitraum in Tagen</param>
    /// <returns>Statistiken über Confidence Scores, Fragen, etc.</returns>
    Task<StagingStatistics> GetStagingStatisticsAsync(int userId, int days = 30);
}

public class StagingStatistics
{
    public int TotalStaged { get; set; }
    public int TotalConfirmed { get; set; }
    public int TotalRejected { get; set; }
    public int TotalModified { get; set; }
    public double AverageConfidenceScore { get; set; }
    public int TotalQuestions { get; set; }
    public double AverageQuestionsPerEntity { get; set; }
    public Dictionary<string, int> QuestionsByPriority { get; set; } = new();
}
