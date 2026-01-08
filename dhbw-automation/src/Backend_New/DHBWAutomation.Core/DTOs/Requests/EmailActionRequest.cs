using System.ComponentModel.DataAnnotations;

namespace DHBWAutomation.Core.DTOs.Requests;

public class EmailActionRequest
{
    [Required]
    public int EmailId { get; set; }

    /// <summary>
    /// Aktion: accept, decline, snooze, archive, delete, mark_read
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Zeitpunkt für Snooze (remind later)
    /// </summary>
    public DateTime? SnoozeUntil { get; set; }

    /// <summary>
    /// Optional: Notiz des Benutzers
    /// </summary>
    [MaxLength(1000)]
    public string? UserNote { get; set; }

    /// <summary>
    /// Bei Termin-Aktionen: Soll Kalendereintrag erstellt werden?
    /// </summary>
    public bool CreateCalendarEvent { get; set; } = true;
}
