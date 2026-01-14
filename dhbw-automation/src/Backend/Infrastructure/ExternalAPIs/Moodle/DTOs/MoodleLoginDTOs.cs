namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle login and authentication
/// </summary>

public class MoodleTokenResponse
{
    public string? Token { get; set; }
    public string? Error { get; set; }
    public string? Errorcode { get; set; }
}

public class MoodleLoginResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
}
