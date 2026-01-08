using System.Text.Json;
using DHBWAutomation.Backend.Shared.Helpers;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// Client für die Moodle-API (Web Services)
/// Dokumentation: https://docs.moodle.org/dev/Web_services
/// </summary>
public class MoodleApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _token;

    public MoodleApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = Environment.GetEnvironmentVariable("MOODLE_BASE_URL")
                   ?? configuration["Moodle:BaseUrl"]
                   ?? "https://moodle.dhbw-ravensburg.de";
        _token = Environment.GetEnvironmentVariable("MOODLE_TOKEN")
                 ?? configuration["Moodle:Token"]
                 ?? "";

        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

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
            return JsonSerializer.Deserialize<MoodleSiteInfo>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Abrufen der Moodle-Site-Info: {ex.Message}");
            return null;
        }
    }
}

// DTOs für Moodle-API-Responses

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
    public long StartDate { get; set; }
    public long EndDate { get; set; }
    public bool Visible { get; set; }
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
