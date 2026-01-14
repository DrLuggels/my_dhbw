namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// Basic DTOs for Moodle API responses
/// </summary>

public class MoodleUsersResponse
{
    public List<MoodleUser>? Users { get; set; }
}

public class MoodleUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Institution { get; set; }
}

public class MoodleCourse
{
    public int Id { get; set; }
    public string Shortname { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public long Startdate { get; set; }
    public long Enddate { get; set; }
    public int Visible { get; set; }
    public string? Format { get; set; }
    public int? Progress { get; set; }
}

public class MoodleSiteInfo
{
    public string Sitename { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public int Userid { get; set; }
    public string Siteurl { get; set; } = string.Empty;
}

public class MoodleWarning
{
    public string? Item { get; set; }
    public int? Itemid { get; set; }
    public string? Warningcode { get; set; }
    public string? Message { get; set; }
}
