using System.Text.Json;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - Glossary methods
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Get all glossaries for the specified courses
    /// API: mod_glossary_get_glossaries_by_courses
    /// </summary>
    public async Task<MoodleGlossariesResponse> GetGlossariesByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = new Dictionary<string, string>();
            for (int i = 0; i < courseIds.Length; i++)
            {
                parameters[$"courseids[{i}]"] = courseIds[i].ToString();
            }

            var url = BuildApiUrl("mod_glossary_get_glossaries_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Glossaries response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleGlossariesResponse>(json, JsonOptions);
            return result ?? new MoodleGlossariesResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching glossaries for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleGlossariesResponse();
        }
    }

    /// <summary>
    /// Get glossary entries
    /// API: mod_glossary_get_entries_by_letter
    /// </summary>
    public async Task<MoodleGlossaryEntriesResponse> GetGlossaryEntriesAsync(int glossaryId, string letter = "ALL", int from = 0, int limit = 100)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "id", glossaryId.ToString() },
                { "letter", letter },
                { "from", from.ToString() },
                { "limit", limit.ToString() }
            };

            var url = BuildApiUrl("mod_glossary_get_entries_by_letter", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MoodleGlossaryEntriesResponse>(json, JsonOptions);
            return result ?? new MoodleGlossaryEntriesResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching glossary entries for glossary {GlossaryId}", glossaryId);
            return new MoodleGlossaryEntriesResponse();
        }
    }
}
