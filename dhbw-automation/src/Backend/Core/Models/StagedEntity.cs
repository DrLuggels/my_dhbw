using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Staging-Entität für AI-extrahierte Daten, die auf User-Bestätigung warten
/// </summary>
public class StagedEntity
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>
    /// ID des Dokuments, aus dem diese Entität extrahiert wurde
    /// </summary>
    public int? SourceDocumentId { get; set; }

    [ForeignKey(nameof(SourceDocumentId))]
    public Document? SourceDocument { get; set; }

    /// <summary>
    /// Art der Entität: "todo", "meeting", "project", "learning_deficit"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialisierte Entitätsdaten (ExtractedTodo, ExtractedMeeting, etc.)
    /// </summary>
    [Required]
    public string EntityData { get; set; } = string.Empty;

    /// <summary>
    /// AI Confidence Score (0-100): Wie sicher ist die AI mit dieser Extraktion?
    /// >90: Sehr sicher, aber trotzdem Staging
    /// 70-90: Unsicher, Rückfragen empfohlen
    /// <70: Sehr unklar, kritische Rückfragen notwendig
    /// </summary>
    [Range(0, 100)]
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// Status: "pending_review", "confirmed", "modified", "rejected"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending_review";

    /// <summary>
    /// Priorität für User-Review: "low", "medium", "high", "urgent"
    /// </summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    /// <summary>
    /// Offene Fragen der AI zu unklaren Feldern
    /// </summary>
    public List<AIQuestion> Questions { get; set; } = new();

    /// <summary>
    /// Wurde diese Entität bereits in die Produktiv-DB übertragen?
    /// </summary>
    public bool IsPromoted { get; set; } = false;

    /// <summary>
    /// ID in der Produktiv-Tabelle nach erfolgreicher Bestätigung (z.B. Todo.Id)
    /// </summary>
    public int? PromotedEntityId { get; set; }

    /// <summary>
    /// Wann wurde diese Entität erstellt?
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Wann wurde diese Entität vom User überprüft?
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Wann wurde diese Entität in die Produktiv-DB übertragen?
    /// </summary>
    public DateTime? PromotedAt { get; set; }

    /// <summary>
    /// Notizen/Korrekturen des Users
    /// </summary>
    public string? UserNotes { get; set; }
}
