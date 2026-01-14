using System.Text.Json;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - Wiki methods
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Get all wikis for the specified courses
    /// API: mod_wiki_get_wikis_by_courses
    /// </summary>
    public async Task<MoodleWikisResponse> GetWikisByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = new Dictionary<string, string>();
            for (int i = 0; i < courseIds.Length; i++)
            {
                parameters[$"courseids[{i}]"] = courseIds[i].ToString();
            }

            var url = BuildApiUrl("mod_wiki_get_wikis_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Wikis response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleWikisResponse>(json, JsonOptions);
            return result ?? new MoodleWikisResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching wikis for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleWikisResponse();
        }
    }

    /// <summary>
    /// Get wiki pages
    /// API: mod_wiki_get_subwiki_pages
    /// </summary>
    public async Task<MoodleWikiPagesResponse> GetWikiPagesAsync(int wikiId)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "wikiid", wikiId.ToString() }
            };

            var url = BuildApiUrl("mod_wiki_get_subwiki_pages", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MoodleWikiPagesResponse>(json, JsonOptions);
            return result ?? new MoodleWikiPagesResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching wiki pages for wiki {WikiId}", wikiId);
            return new MoodleWikiPagesResponse();
        }
    }

    /// <summary>
    /// Get wiki page content
    /// API: mod_wiki_get_page_contents
    /// </summary>
    public async Task<MoodleWikiPageContent?> GetWikiPageContentAsync(int pageId)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "pageid", pageId.ToString() }
            };

            var url = BuildApiUrl("mod_wiki_get_page_contents", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MoodleWikiPageContentResponse>(json, JsonOptions);
            return result?.Page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching wiki page content for page {PageId}", pageId);
            return null;
        }
    }
}
