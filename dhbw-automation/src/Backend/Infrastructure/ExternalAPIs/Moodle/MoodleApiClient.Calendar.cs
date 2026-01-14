using System.Text.Json;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - Calendar event methods
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Get calendar events for a time period
    /// API: core_calendar_get_calendar_events
    /// </summary>
    /// <param name="timeStart">Start time (Unix timestamp)</param>
    /// <param name="timeEnd">End time (Unix timestamp)</param>
    /// <param name="courseIds">Optional: Only events from these courses</param>
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
            _logger?.LogError(ex, "Error fetching calendar events");
            return new MoodleCalendarEventsResponse();
        }
    }

    /// <summary>
    /// Get upcoming events for a user
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
            _logger?.LogError(ex, "Error fetching upcoming events");
            return new List<MoodleCalendarEvent>();
        }
    }
}
