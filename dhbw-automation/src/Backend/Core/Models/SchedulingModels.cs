namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Scheduling constraints for finding available time slots
/// </summary>
public class SchedulingConstraints
{
    public TimeOnly EarliestStart { get; set; } = new(8, 0); // 08:00
    public TimeOnly LatestEnd { get; set; } = new(22, 0); // 22:00

    public int MinSleepHours { get; set; } = 8;
    public TimeOnly PreferredSleepTime { get; set; } = new(23, 0); // 23:00
    public TimeOnly PreferredWakeTime { get; set; } = new(7, 0); // 07:00

    public int CommuteMinutes { get; set; } = 60; // Anfahrt zur Uni
    public List<DayOfWeek> UniversityDays { get; set; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };

    public int BufferBetweenEventsMinutes { get; set; } = 15;
    public int MaxEventsPerDay { get; set; } = 5;

    // Wochenend-Logik für Projekte
    public int MaxWeekendHoursPerDay { get; set; } = 6; // Bis zu 6h am Samstag/Sonntag
    public bool AllowWeekendWork { get; set; } = true;
}

/// <summary>
/// Represents an available time slot
/// </summary>
public class TimeSlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int DurationMinutes => (int)(End - Start).TotalMinutes;
    public string QualityScore { get; set; } = "good"; // "excellent", "good", "acceptable", "poor"
    public string Reason { get; set; } = ""; // Explanation for the quality score
}

/// <summary>
/// Represents a scheduled learning session
/// </summary>
public class LearningSession
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int PriorityScore { get; set; } = 50; // 0-100
}
