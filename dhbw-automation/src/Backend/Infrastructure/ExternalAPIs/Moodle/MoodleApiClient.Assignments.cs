using System.Text.Json;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - Assignment methods
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Get all assignments for the specified courses
    /// API: mod_assign_get_assignments
    /// </summary>
    /// <param name="courseIds">List of course IDs</param>
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
            _logger?.LogError(ex, "Error fetching assignments for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleAssignmentsResponse();
        }
    }

    /// <summary>
    /// Get the submission status for an assignment
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
            _logger?.LogError(ex, "Error fetching submission status for assignment {AssignmentId}", assignmentId);
            return null;
        }
    }
}
