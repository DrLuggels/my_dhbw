namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle calendar events
/// </summary>

public class MoodleCalendarEventsResponse
{
    public List<MoodleCalendarEvent>? Events { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleActionEventsResponse
{
    public List<MoodleCalendarEvent>? Events { get; set; }
    public bool Firstid { get; set; }
    public bool Lastid { get; set; }
}

public class MoodleCalendarEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Format { get; set; }
    public int Courseid { get; set; }
    public string? Categoryid { get; set; }
    public int? Groupid { get; set; }
    public int? Userid { get; set; }
    public int? Instance { get; set; }
    public string? Modulename { get; set; }
    public long Timestart { get; set; }
    public int Timeduration { get; set; }
    public bool Visible { get; set; }
    public string? Eventtype { get; set; }
    public MoodleEventAction? Action { get; set; }
    public MoodleEventCourse? Course { get; set; }
}

public class MoodleEventAction
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Itemcount { get; set; }
    public bool Actionable { get; set; }
}

public class MoodleEventCourse
{
    public int Id { get; set; }
    public string Fullname { get; set; } = string.Empty;
    public string Shortname { get; set; } = string.Empty;
}
