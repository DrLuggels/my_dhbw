namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle wikis
/// </summary>

public class MoodleWikisResponse
{
    public List<MoodleWikiData>? Wikis { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleWikiData
{
    public int Id { get; set; }
    public int Coursemodule { get; set; }
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
    public string Firstpagetitle { get; set; } = string.Empty;
    public string Wikimode { get; set; } = string.Empty;
    public string Defaultformat { get; set; } = string.Empty;
    public int Forceformat { get; set; }
    public int Editbegin { get; set; }
    public int Editend { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public int Cancreatepages { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
}

public class MoodleWikiPagesResponse
{
    public List<MoodleWikiPage>? Pages { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleWikiPage
{
    public int Id { get; set; }
    public int Subwikiid { get; set; }
    public string Title { get; set; } = string.Empty;
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
    public long Timerendered { get; set; }
    public int Userid { get; set; }
    public int Pageviews { get; set; }
    public int Readonly { get; set; }
    public bool Caneditpage { get; set; }
    public bool Firstpage { get; set; }
    public string? Cachedcontent { get; set; }
    public int Contentformat { get; set; }
    public List<string>? Tags { get; set; }
}

public class MoodleWikiPageContentResponse
{
    public MoodleWikiPageContent? Page { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleWikiPageContent
{
    public int Id { get; set; }
    public int Wikiid { get; set; }
    public int Subwikiid { get; set; }
    public int Groupid { get; set; }
    public int Userid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Cachedcontent { get; set; } = string.Empty;
    public int Contentformat { get; set; }
    public bool Caneditpage { get; set; }
    public int Version { get; set; }
    public List<string>? Tags { get; set; }
}
