namespace DHBWAutomation.Core.Interfaces;

public interface IRaplaService
{
    /// <summary>
    /// Synchronisiert Rapla-Stundenplan mit dem lokalen Kalender
    /// </summary>
    Task<bool> SyncCalendarAsync(int userId);
    
    /// <summary>
    /// Ruft alle Termine für eine bestimmte Woche ab
    /// </summary>
    Task<IEnumerable<RaplaEvent>> GetWeekScheduleAsync(DateTime weekStart);
    
    /// <summary>
    /// Ruft den aktuellen Stundenplan ab
    /// </summary>
    Task<string> GetRawCalendarDataAsync();
}

public class RaplaEvent
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public string? Lecturer { get; set; }
    public string? CourseCode { get; set; }
}
