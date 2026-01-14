namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle forums
/// </summary>

public class MoodleForumData
{
    public int Id { get; set; }
    public int Course { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public int Assessed { get; set; }
    public int Assesstimestart { get; set; }
    public int Assesstimefinish { get; set; }
    public int Scale { get; set; }
    public int Maxbytes { get; set; }
    public int Maxattachments { get; set; }
    public int Forcesubscribe { get; set; }
    public int Trackingtype { get; set; }
    public int Rsstype { get; set; }
    public int Rssarticles { get; set; }
    public long Timemodified { get; set; }
    public int Warnafter { get; set; }
    public int Blockafter { get; set; }
    public int Blockperiod { get; set; }
    public int Completiondiscussions { get; set; }
    public int Completionreplies { get; set; }
    public int Completionposts { get; set; }
    public int Cmid { get; set; }
    public int Numdiscussions { get; set; }
    public int Cancreatediscussions { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
}

public class MoodleForumDiscussionsResponse
{
    public List<MoodleForumDiscussion>? Discussions { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleForumDiscussion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Groupid { get; set; }
    public int Timemodified { get; set; }
    public int Usermodified { get; set; }
    public int Timestart { get; set; }
    public int Timeend { get; set; }
    public int Discussion { get; set; }
    public int Parent { get; set; }
    public int Userid { get; set; }
    public long Created { get; set; }
    public long Modified { get; set; }
    public int Mailed { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int Messageformat { get; set; }
    public string? Messagetrust { get; set; }
    public int Attachment { get; set; }
    public int Totalscore { get; set; }
    public int Mailnow { get; set; }
    public string Userfullname { get; set; } = string.Empty;
    public string? Usermodifiedfullname { get; set; }
    public string? Userpictureurl { get; set; }
    public string? Usermodifiedpictureurl { get; set; }
    public int Numreplies { get; set; }
    public int Numunread { get; set; }
    public bool Pinned { get; set; }
    public bool Locked { get; set; }
    public bool Starred { get; set; }
    public bool Canreply { get; set; }
    public bool Canlock { get; set; }
    public bool Canfavourite { get; set; }
    public List<MoodleModuleContent>? Attachments { get; set; }
}
