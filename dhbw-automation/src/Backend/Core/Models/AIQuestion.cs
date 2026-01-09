using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Eine Frage der AI zu unklaren/fehlenden Daten in einer StagedEntity
/// </summary>
public class AIQuestion
{
    [Key]
    public int Id { get; set; }

    public int StagedEntityId { get; set; }

    [ForeignKey(nameof(StagedEntityId))]
    public StagedEntity StagedEntity { get; set; } = null!;

    /// <summary>
    /// Feld-Name in der EntityData, das unklar ist (z.B. "suggestedDate", "priority", "personName")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Die Frage, die der User sieht (z.B. "Wann genau möchtest du das Meeting planen?")
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// JSON-Array von vorgeschlagenen Antworten (z.B. ["Montag 14:00", "Mittwoch 16:00", "Freitag 10:00"])
    /// </summary>
    public string? SuggestedAnswers { get; set; }

    /// <summary>
    /// Priorität der Frage: "low", "medium", "high", "critical"
    /// - critical: Muss beantwortet werden (z.B. fehlende Person beim Meeting)
    /// - high: Stark empfohlen (z.B. fehlendes Datum)
    /// - medium: Hilfreich (z.B. fehlende Dauer)
    /// - low: Optional (z.B. fehlende Beschreibung)
    /// </summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    /// <summary>
    /// Wurde diese Frage bereits vom User beantwortet?
    /// </summary>
    public bool IsAnswered { get; set; } = false;

    /// <summary>
    /// Die Antwort des Users (kann aus SuggestedAnswers stammen oder freie Eingabe sein)
    /// </summary>
    public string? UserAnswer { get; set; }

    /// <summary>
    /// Typ der erwarteten Antwort: "text", "date", "time", "datetime", "choice", "number"
    /// </summary>
    [MaxLength(20)]
    public string AnswerType { get; set; } = "text";

    /// <summary>
    /// Validierungs-Regex für freie Text-Antworten (optional)
    /// </summary>
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// Wann wurde diese Frage erstellt?
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Wann wurde diese Frage beantwortet?
    /// </summary>
    public DateTime? AnsweredAt { get; set; }
}
