using System.Text.Json;
using System.Text.Json.Serialization;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// Client für die Moodle-API (Web Services)
/// Dokumentation: https://docs.moodle.org/dev/Web_services
/// </summary>
public class MoodleApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MoodleApiClient>? _logger;
    private readonly string _baseUrl;
    private string _token;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public MoodleApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<MoodleApiClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = Environment.GetEnvironmentVariable("MOODLE_BASE_URL")
                   ?? configuration["Moodle:BaseUrl"]
                   ?? "https://moodle.dhbw-ravensburg.de";
        _token = Environment.GetEnvironmentVariable("MOODLE_TOKEN")
                 ?? configuration["Moodle:Token"]
                 ?? "";

        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    /// <summary>
    /// Setzt den Token für API-Anfragen (z.B. nach Login)
    /// </summary>
    public void SetToken(string token)
    {
        _token = token;
    }

    /// <summary>
    /// Prüft ob ein gültiger Token vorhanden ist
    /// </summary>
    public bool HasToken => !string.IsNullOrEmpty(_token);

    /// <summary>
    /// Erstellt die Moodle-API-URL mit Token
    /// </summary>
    private string BuildApiUrl(string function, Dictionary<string, string>? parameters = null)
    {
        var url = $"/webservice/rest/server.php?wstoken={_token}&wsfunction={function}&moodlewsrestformat=json";

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
            }
        }

        return url;
    }

    /// <summary>
    /// Holt Benutzerinformationen basierend auf der E-Mail-Adresse
    /// Verwendet DHBWAuthHelper um den korrekten Username zu generieren
    /// </summary>
    /// <param name="email">Die E-Mail-Adresse des Benutzers</param>
    public async Task<MoodleUser?> GetUserByEmailAsync(string email)
    {
        try
        {
            // Konvertiere E-Mail zu Moodle-Username (z.B. student123@dhbw-ravensburg.de -> student123)
            var username = DHBWAuthHelper.GetMoodleUsername(email);

            var parameters = new Dictionary<string, string>
            {
                { "criteria[0][key]", "username" },
                { "criteria[0][value]", username }
            };

            var url = BuildApiUrl("core_user_get_users", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MoodleUsersResponse>(json);

            return result?.Users?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Abrufen des Moodle-Benutzers: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Holt alle Kurse eines Benutzers
    /// </summary>
    /// <param name="userId">Die Moodle-User-ID</param>
    public async Task<List<MoodleCourse>> GetUserCoursesAsync(int userId)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "userid", userId.ToString() }
            };

            var url = BuildApiUrl("core_enrol_get_users_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var courses = JsonSerializer.Deserialize<List<MoodleCourse>>(json);

            return courses ?? new List<MoodleCourse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Abrufen der Moodle-Kurse: {ex.Message}");
            return new List<MoodleCourse>();
        }
    }

    /// <summary>
    /// Testet die Verbindung zur Moodle-API
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var url = BuildApiUrl("core_webservice_get_site_info");
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Holt Site-Informationen (zum Testen der Verbindung)
    /// </summary>
    public async Task<MoodleSiteInfo?> GetSiteInfoAsync()
    {
        try
        {
            var url = BuildApiUrl("core_webservice_get_site_info");
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MoodleSiteInfo>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fehler beim Abrufen der Moodle-Site-Info");
            return null;
        }
    }

    #region Auto-Login

    /// <summary>
    /// Meldet sich mit Username und Passwort an und holt einen Token
    /// Endpoint: /login/token.php
    /// </summary>
    /// <param name="username">Moodle-Username (z.B. student123)</param>
    /// <param name="password">Moodle-Passwort</param>
    /// <param name="service">Service-Name (Standard: moodle_mobile_app)</param>
    /// <returns>Token wenn erfolgreich, null bei Fehler</returns>
    public async Task<MoodleLoginResult> LoginAndGetTokenAsync(string username, string password, string service = "moodle_mobile_app")
    {
        try
        {
            var loginParams = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "service", service }
            };

            var response = await _httpClient.PostAsync(
                "/login/token.php",
                new FormUrlEncodedContent(loginParams));

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Moodle Login Response: {Json}", json);

            var result = JsonSerializer.Deserialize<MoodleTokenResponse>(json, JsonOptions);

            if (result?.Token != null)
            {
                _token = result.Token;
                return new MoodleLoginResult
                {
                    Success = true,
                    Token = result.Token
                };
            }

            return new MoodleLoginResult
            {
                Success = false,
                ErrorMessage = result?.Error ?? "Unbekannter Fehler beim Login"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fehler beim Moodle-Login für User {Username}", username);
            return new MoodleLoginResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Kursinhalte

    /// <summary>
    /// Holt alle Inhalte eines Kurses (Sektionen, Module, Dateien)
    /// API: core_course_get_contents
    /// </summary>
    /// <param name="courseId">Die Moodle-Kurs-ID</param>
    public async Task<List<MoodleCourseSection>> GetCourseContentsAsync(int courseId)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "courseid", courseId.ToString() }
            };

            var url = BuildApiUrl("core_course_get_contents", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Course contents response for {CourseId}: {Length} chars", courseId, json.Length);

            var sections = JsonSerializer.Deserialize<List<MoodleCourseSection>>(json, JsonOptions);
            return sections ?? new List<MoodleCourseSection>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fehler beim Abrufen der Kursinhalte für Kurs {CourseId}", courseId);
            return new List<MoodleCourseSection>();
        }
    }

    #endregion

    #region Aufgaben (Assignments)

    /// <summary>
    /// Holt alle Aufgaben für die angegebenen Kurse
    /// API: mod_assign_get_assignments
    /// </summary>
    /// <param name="courseIds">Liste von Kurs-IDs</param>
    public async Task<MoodleAssignmentsResponse> GetAssignmentsAsync(params int[] courseIds)
    {
        try
        {
            var parameters = new Dictionary<string, string>();
            for (int i = 0; i < courseIds.Length; i++)
            {
                parameters[$"courseids[{i}]"] = courseIds[i].ToString();
            }

            var url = BuildApiUrl("mod_assign_get_assignments", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Assignments response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleAssignmentsResponse>(json, JsonOptions);
            return result ?? new MoodleAssignmentsResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fehler beim Abrufen der Aufgaben für Kurse {CourseIds}", string.Join(", ", courseIds));
            return new MoodleAssignmentsResponse();
        }
    }

    /// <summary>
    /// Holt den Einreichungsstatus für eine Aufgabe
    /// API: mod_assign_get_submission_status
    /// </summary>
    public async Task<MoodleSubmissionStatus?> GetSubmissionStatusAsync(int assignmentId, int userId)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "assignid", assignmentId.ToString() },
                { "userid", userId.ToString() }
            };

            var url = BuildApiUrl("mod_assign_get_submission_status", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MoodleSubmissionStatus>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fehler beim Abrufen des Submission-Status für Assignment {AssignmentId}", assignmentId);
            return null;
        }
    }

    #endregion

    #region Kalender-Events

    /// <summary>
    /// Holt Kalender-Events für einen Zeitraum
    /// API: core_calendar_get_calendar_events
    /// </summary>
    /// <param name="timeStart">Start-Zeitpunkt (Unix-Timestamp)</param>
    /// <param name="timeEnd">End-Zeitpunkt (Unix-Timestamp)</param>
    /// <param name="courseIds">Optional: Nur Events dieser Kurse</param>
    public async Task<MoodleCalendarEventsResponse> GetCalendarEventsAsync(
        long? timeStart = null,
        long? timeEnd = null,
        int[]? courseIds = null)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "options[siteevents]", "1" },
                { "options[userevents]", "1" }
            };

            if (timeStart.HasValue)
                parameters["options[timestart]"] = timeStart.Value.ToString();
            if (timeEnd.HasValue)
                parameters["options[timeend]"] = timeEnd.Value.ToString();

            if (courseIds != null)
            {
                for (int i = 0; i < courseIds.Length; i++)
                {
                    parameters[$"events[courseids][{i}]"] = courseIds[i].ToString();
                }
            }

            var url = BuildApiUrl("core_calendar_get_calendar_events", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Calendar events response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleCalendarEventsResponse>(json, JsonOptions);
            return result ?? new MoodleCalendarEventsResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fehler beim Abrufen der Kalender-Events");
            return new MoodleCalendarEventsResponse();
        }
    }

    /// <summary>
    /// Holt anstehende Events für einen Benutzer
    /// API: core_calendar_get_action_events_by_timesort
    /// </summary>
    public async Task<List<MoodleCalendarEvent>> GetUpcomingEventsAsync(int limitNum = 50)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var parameters = new Dictionary<string, string>
            {
                { "timesortfrom", now.ToString() },
                { "limitnum", limitNum.ToString() }
            };

            var url = BuildApiUrl("core_calendar_get_action_events_by_timesort", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MoodleActionEventsResponse>(json, JsonOptions);
            return result?.Events ?? new List<MoodleCalendarEvent>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fehler beim Abrufen der anstehenden Events");
            return new List<MoodleCalendarEvent>();
        }
    }

    #endregion

    #region Datei-Download

    /// <summary>
    /// Lädt eine Datei von Moodle herunter
    /// Verwendet den Token für authentifizierten Zugriff
    /// </summary>
    /// <param name="fileUrl">Die Moodle-Datei-URL (kann relativ oder absolut sein)</param>
    /// <returns>Datei als Byte-Array oder null bei Fehler</returns>
    public async Task<MoodleFileDownloadResult> DownloadFileAsync(string fileUrl)
    {
        try
        {
            // URL mit Token versehen wenn nötig
            var downloadUrl = fileUrl;
            if (!fileUrl.Contains("token="))
            {
                var separator = fileUrl.Contains("?") ? "&" : "?";
                downloadUrl = $"{fileUrl}{separator}token={_token}";
            }

            // Wenn relative URL, BaseUrl hinzufügen
            if (!downloadUrl.StartsWith("http"))
            {
                downloadUrl = $"{_baseUrl.TrimEnd('/')}/{downloadUrl.TrimStart('/')}";
            }

            _logger?.LogDebug("Downloading file from: {Url}", downloadUrl.Replace(_token, "***TOKEN***"));

            var response = await _httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                          ?? Path.GetFileName(new Uri(downloadUrl).LocalPath);
            var content = await response.Content.ReadAsByteArrayAsync();

            return new MoodleFileDownloadResult
            {
                Success = true,
                Content = content,
                FileName = fileName,
                ContentType = contentType,
                FileSize = content.Length
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fehler beim Herunterladen der Datei von {Url}", fileUrl);
            return new MoodleFileDownloadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Konstruiert die Download-URL für eine Moodle-Datei
    /// </summary>
    public string GetFileDownloadUrl(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
            return string.Empty;

        // pluginfile.php URLs benötigen Token
        if (fileUrl.Contains("pluginfile.php") && !fileUrl.Contains("token="))
        {
            // Ersetze /pluginfile.php durch /webservice/pluginfile.php für Token-Auth
            var tokenUrl = fileUrl.Replace("/pluginfile.php", "/webservice/pluginfile.php");
            var separator = tokenUrl.Contains("?") ? "&" : "?";
            return $"{tokenUrl}{separator}token={_token}";
        }

        return fileUrl;
    }

    #endregion
}

// DTOs für Moodle-API-Responses

#region Basic DTOs

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
    public bool Visible { get; set; }
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

#endregion

#region Login DTOs

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

#endregion

#region Kursinhalte DTOs

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
    public string Modname { get; set; } = string.Empty;  // resource, assign, folder, url, page, etc.
    public string? Url { get; set; }
    public int? Instance { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Contents { get; set; }
}

public class MoodleModuleContent
{
    public string Type { get; set; } = string.Empty;  // file, url, etc.
    public string Filename { get; set; } = string.Empty;
    public string? Filepath { get; set; }
    public long Filesize { get; set; }
    public string? Fileurl { get; set; }
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
    public string? Mimetype { get; set; }
    public string? Author { get; set; }
}

#endregion

#region Assignment DTOs

public class MoodleAssignmentsResponse
{
    public List<MoodleAssignmentCourse>? Courses { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleAssignmentCourse
{
    public int Id { get; set; }
    public string Fullname { get; set; } = string.Empty;
    public string Shortname { get; set; } = string.Empty;
    public List<MoodleAssignmentData>? Assignments { get; set; }
}

public class MoodleAssignmentData
{
    public int Id { get; set; }
    public int Cmid { get; set; }  // Course module ID
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }  // Description HTML
    public long Duedate { get; set; }  // Unix timestamp
    public long Cutoffdate { get; set; }  // Unix timestamp
    public long Allowsubmissionsfromdate { get; set; }
    public int Grade { get; set; }  // Max grade
    public string? Submissiondrafts { get; set; }
    public bool Teamsubmission { get; set; }
}

public class MoodleSubmissionStatus
{
    public MoodleLastAttempt? Lastattempt { get; set; }
    public MoodleFeedback? Feedback { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleLastAttempt
{
    public MoodleSubmission? Submission { get; set; }
    public bool Submissionsenabled { get; set; }
    public bool Locked { get; set; }
    public bool Graded { get; set; }
    public bool Canedit { get; set; }
    public bool Cansubmit { get; set; }
}

public class MoodleSubmission
{
    public int Id { get; set; }
    public int Userid { get; set; }
    public string Status { get; set; } = string.Empty;  // new, draft, submitted
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
}

public class MoodleFeedback
{
    public MoodleGrade? Grade { get; set; }
    public string? Gradefordisplay { get; set; }
}

public class MoodleGrade
{
    public int Id { get; set; }
    public string? Grade { get; set; }
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
}

public class MoodleWarning
{
    public string? Item { get; set; }
    public int? Itemid { get; set; }
    public string? Warningcode { get; set; }
    public string? Message { get; set; }
}

#endregion

#region Calendar DTOs

public class MoodleCalendarEventsResponse
{
    public List<MoodleCalendarEvent>? Events { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleActionEventsResponse
{
    public List<MoodleCalendarEvent>? Events { get; set; }
    public bool Firstid { get; set; }
    public bool Lastid { get; set; }
}

public class MoodleCalendarEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Format { get; set; }
    public int Courseid { get; set; }
    public string? Categoryid { get; set; }
    public int? Groupid { get; set; }
    public int? Userid { get; set; }
    public int? Instance { get; set; }
    public string? Modulename { get; set; }  // assign, quiz, etc.
    public long Timestart { get; set; }  // Unix timestamp
    public int Timeduration { get; set; }  // Duration in seconds
    public bool Visible { get; set; }
    public string? Eventtype { get; set; }  // due, course, user, etc.
    public MoodleEventAction? Action { get; set; }
    public MoodleEventCourse? Course { get; set; }
}

public class MoodleEventAction
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Itemcount { get; set; }
    public bool Actionable { get; set; }
}

public class MoodleEventCourse
{
    public int Id { get; set; }
    public string Fullname { get; set; } = string.Empty;
    public string Shortname { get; set; } = string.Empty;
}

#endregion

#region File Download DTOs

public class MoodleFileDownloadResult
{
    public bool Success { get; set; }
    public byte[]? Content { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
