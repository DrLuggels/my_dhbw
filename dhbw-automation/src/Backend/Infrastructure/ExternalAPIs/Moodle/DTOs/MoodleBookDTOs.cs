namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle books
/// </summary>

public class MoodleBooksResponse
{
    public List<MoodleBookData>? Books { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleBookData
{
    public int Id { get; set; }
    public int Coursemodule { get; set; }
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public int Numbering { get; set; }
    public int Navstyle { get; set; }
    public string? Customtitles { get; set; }
    public int Revision { get; set; }
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
}
