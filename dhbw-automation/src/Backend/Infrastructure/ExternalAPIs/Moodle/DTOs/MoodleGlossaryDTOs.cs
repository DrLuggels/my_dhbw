namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle glossaries
/// </summary>

public class MoodleGlossariesResponse
{
    public List<MoodleGlossaryData>? Glossaries { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleGlossaryData
{
    public int Id { get; set; }
    public int Coursemodule { get; set; }
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public int Allowduplicatedentries { get; set; }
    public string Displayformat { get; set; } = string.Empty;
    public int Mainglossary { get; set; }
    public int Showspecial { get; set; }
    public int Showalphabet { get; set; }
    public int Showall { get; set; }
    public int Allowcomments { get; set; }
    public int Allowprintview { get; set; }
    public int Usedynalink { get; set; }
    public int Defaultapproval { get; set; }
    public int Approvaldisplayformat { get; set; }
    public int Globalglossary { get; set; }
    public int Entbypage { get; set; }
    public int Editalways { get; set; }
    public int Rsstype { get; set; }
    public int Rssarticles { get; set; }
    public int Assessed { get; set; }
    public int Assesstimestart { get; set; }
    public int Assesstimefinish { get; set; }
    public int Scale { get; set; }
    public int Entrycount { get; set; }
    public int Completionentries { get; set; }
    public bool Canaddentry { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
}

public class MoodleGlossaryEntriesResponse
{
    public int Count { get; set; }
    public List<MoodleGlossaryEntry>? Entries { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleGlossaryEntry
{
    public int Id { get; set; }
    public int Glossaryid { get; set; }
    public int Userid { get; set; }
    public string Userfullname { get; set; } = string.Empty;
    public string? Userpictureurl { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string? Definition { get; set; }
    public int Definitionformat { get; set; }
    public int Definitiontrust { get; set; }
    public int Attachment { get; set; }
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
    public int Teacherentry { get; set; }
    public int Sourceglossaryid { get; set; }
    public int Usedynalink { get; set; }
    public int Casesensitive { get; set; }
    public int Fullmatch { get; set; }
    public int Approved { get; set; }
    public string? Tags { get; set; }
    public List<MoodleModuleContent>? Definitioninlinefiles { get; set; }
    public List<MoodleModuleContent>? Attachments { get; set; }
}
