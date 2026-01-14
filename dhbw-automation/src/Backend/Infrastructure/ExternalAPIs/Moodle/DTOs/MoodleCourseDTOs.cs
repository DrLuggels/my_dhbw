namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle course contents
/// </summary>

public class MoodleCourseSection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModule>? Modules { get; set; }
}

public class MoodleModule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Modname { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int? Instance { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Contents { get; set; }
}

public class MoodleModuleContent
{
    public string Type { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string? Filepath { get; set; }
    public long Filesize { get; set; }
    public string? Fileurl { get; set; }
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
    public string? Mimetype { get; set; }
    public string? Author { get; set; }
}
