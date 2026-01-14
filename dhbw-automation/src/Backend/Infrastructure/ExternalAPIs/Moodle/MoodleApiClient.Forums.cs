using System.Text.Json;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - Forum methods
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Get all forums for the specified courses
    /// API: mod_forum_get_forums_by_courses
    /// </summary>
    public async Task<List<MoodleForumData>> GetForumsByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = new Dictionary<string, string>();
            for (int i = 0; i < courseIds.Length; i++)
            {
                parameters[$"courseids[{i}]"] = courseIds[i].ToString();
            }

            var url = BuildApiUrl("mod_forum_get_forums_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Forums response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<List<MoodleForumData>>(json, JsonOptions);
            return result ?? new List<MoodleForumData>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching forums for courses {CourseIds}", string.Join(", ", courseIds));
            return new List<MoodleForumData>();
        }
    }

    /// <summary>
    /// Get forum discussions
    /// API: mod_forum_get_forum_discussions
    /// </summary>
    public async Task<MoodleForumDiscussionsResponse> GetForumDiscussionsAsync(int forumId, int page = 0, int perPage = 50)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "forumid", forumId.ToString() },
                { "page", page.ToString() },
                { "perpage", perPage.ToString() }
            };

            var url = BuildApiUrl("mod_forum_get_forum_discussions", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MoodleForumDiscussionsResponse>(json, JsonOptions);
            return result ?? new MoodleForumDiscussionsResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching forum discussions for forum {ForumId}", forumId);
            return new MoodleForumDiscussionsResponse();
        }
    }
}
