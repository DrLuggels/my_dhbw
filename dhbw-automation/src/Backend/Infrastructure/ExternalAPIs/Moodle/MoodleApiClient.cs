using System.Text.Json;
using System.Text.Json.Serialization;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// Client for the Moodle API (Web Services)
/// Documentation: https://docs.moodle.org/dev/Web_services
///
/// This is a partial class split across multiple files:
/// - MoodleApiClient.cs (this file) - Core functionality
/// - MoodleApiClient.Auth.cs - Authentication
/// - MoodleApiClient.Courses.cs - Course contents
/// - MoodleApiClient.Assignments.cs - Assignments
/// - MoodleApiClient.Calendar.cs - Calendar events
/// - MoodleApiClient.Modules.cs - Module-specific APIs
/// - MoodleApiClient.Forums.cs - Forum APIs
/// - MoodleApiClient.Glossary.cs - Glossary APIs
/// - MoodleApiClient.Wiki.cs - Wiki APIs
/// - MoodleApiClient.Files.cs - File downloads
/// </summary>
public partial class MoodleApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MoodleApiClient>? _logger;
    private readonly string _baseUrl;
    private string _token;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new MoodleBoolConverter() }
    };

    public MoodleApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<MoodleApiClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = Environment.GetEnvironmentVariable("MOODLE_BASE_URL")
                   ?? configuration["Moodle:BaseUrl"]
                   ?? "https://elearning.dhbw-ravensburg.de";
        _token = Environment.GetEnvironmentVariable("MOODLE_TOKEN")
                 ?? configuration["Moodle:Token"]
                 ?? "";

        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    /// <summary>
    /// Sets the token for API requests (e.g., after login)
    /// </summary>
    public void SetToken(string token)
    {
        _token = token;
    }

    /// <summary>
    /// Checks if a valid token is present
    /// </summary>
    public bool HasToken => !string.IsNullOrEmpty(_token);

    /// <summary>
    /// Builds the Moodle API URL with token
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
    /// Gets user information based on email address
    /// Uses DHBWAuthHelper to generate the correct username
    /// </summary>
    /// <param name="email">The user's email address</param>
    public async Task<MoodleUser?> GetUserByEmailAsync(string email)
    {
        try
        {
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
            Console.WriteLine($"Error fetching Moodle user: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all courses of a user
    /// </summary>
    /// <param name="userId">The Moodle user ID</param>
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
            _logger?.LogDebug("Moodle courses response: {Json}", json.Length > 500 ? json.Substring(0, 500) : json);
            var courses = JsonSerializer.Deserialize<List<MoodleCourse>>(json, JsonOptions);

            return courses ?? new List<MoodleCourse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Moodle courses: {ex.Message}");
            return new List<MoodleCourse>();
        }
    }

    /// <summary>
    /// Tests the connection to the Moodle API
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
    /// Gets site information (for testing the connection)
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
            _logger?.LogError(ex, "Error fetching Moodle site info");
            return null;
        }
    }
}
