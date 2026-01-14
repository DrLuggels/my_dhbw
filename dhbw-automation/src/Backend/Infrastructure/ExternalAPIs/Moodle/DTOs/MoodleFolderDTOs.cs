namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle folders
/// </summary>

public class MoodleFoldersResponse
{
    public List<MoodleFolderData>? Folders { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleFolderData
{
    public int Id { get; set; }
    public int Coursemodule { get; set; }
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public int Revision { get; set; }
    public long Timemodified { get; set; }
    public int Display { get; set; }
    public int Showexpanded { get; set; }
    public int Showdownloadfolder { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
}
