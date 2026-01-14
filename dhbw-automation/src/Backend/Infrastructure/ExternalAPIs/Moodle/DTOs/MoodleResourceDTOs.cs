namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle resources
/// </summary>

public class MoodleResourcesResponse
{
    public List<MoodleResourceData>? Resources { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleResourceData
{
    public int Id { get; set; }
    public int Coursemodule { get; set; }
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public string? Tobemigrated { get; set; }
    public string? Legacyfiles { get; set; }
    public string? Legacyfileslast { get; set; }
    public int Display { get; set; }
    public string? Displayoptions { get; set; }
    public string? Filterfiles { get; set; }
    public int Revision { get; set; }
    public long Timemodified { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
    public List<MoodleModuleContent>? Contentfiles { get; set; }
}
