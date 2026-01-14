namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle URLs and Labels
/// </summary>

public class MoodleUrlsResponse
{
    public List<MoodleUrlData>? Urls { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleUrlData
{
    public int Id { get; set; }
    public int Coursemodule { get; set; }
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public string Externalurl { get; set; } = string.Empty;
    public int Display { get; set; }
    public string? Displayoptions { get; set; }
    public string? Parameters { get; set; }
    public long Timemodified { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
}

public class MoodleLabelsResponse
{
    public List<MoodleLabelData>? Labels { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleLabelData
{
    public int Id { get; set; }
    public int Coursemodule { get; set; }
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public long Timemodified { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
}
