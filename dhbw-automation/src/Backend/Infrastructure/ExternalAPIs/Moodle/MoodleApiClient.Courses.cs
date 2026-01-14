using System.Text.Json;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - Course content methods
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Get all contents of a course (sections, modules, files)
    /// API: core_course_get_contents
    /// </summary>
    /// <param name="courseId">The Moodle course ID</param>
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
            _logger?.LogError(ex, "Error fetching course contents for course {CourseId}", courseId);
            return new List<MoodleCourseSection>();
        }
    }
}
