using System.Text.Json;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - Module-specific API methods (folders, pages, books, resources, etc.)
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Get all folders for the specified courses
    /// API: mod_folder_get_folders_by_courses
    /// </summary>
    public async Task<MoodleFoldersResponse> GetFoldersByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = BuildCourseIdsParameters(courseIds);
            var url = BuildApiUrl("mod_folder_get_folders_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Folders response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleFoldersResponse>(json, JsonOptions);
            return result ?? new MoodleFoldersResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching folders for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleFoldersResponse();
        }
    }

    /// <summary>
    /// Get all pages for the specified courses
    /// API: mod_page_get_pages_by_courses
    /// </summary>
    public async Task<MoodlePagesResponse> GetPagesByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = BuildCourseIdsParameters(courseIds);
            var url = BuildApiUrl("mod_page_get_pages_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Pages response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodlePagesResponse>(json, JsonOptions);
            return result ?? new MoodlePagesResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching pages for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodlePagesResponse();
        }
    }

    /// <summary>
    /// Get all books for the specified courses
    /// API: mod_book_get_books_by_courses
    /// </summary>
    public async Task<MoodleBooksResponse> GetBooksByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = BuildCourseIdsParameters(courseIds);
            var url = BuildApiUrl("mod_book_get_books_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Books response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleBooksResponse>(json, JsonOptions);
            return result ?? new MoodleBooksResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching books for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleBooksResponse();
        }
    }

    /// <summary>
    /// Get all resources for the specified courses
    /// API: mod_resource_get_resources_by_courses
    /// </summary>
    public async Task<MoodleResourcesResponse> GetResourcesByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = BuildCourseIdsParameters(courseIds);
            var url = BuildApiUrl("mod_resource_get_resources_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Resources response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleResourcesResponse>(json, JsonOptions);
            return result ?? new MoodleResourcesResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching resources for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleResourcesResponse();
        }
    }

    /// <summary>
    /// Get all URLs for the specified courses
    /// API: mod_url_get_urls_by_courses
    /// </summary>
    public async Task<MoodleUrlsResponse> GetUrlsByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = BuildCourseIdsParameters(courseIds);
            var url = BuildApiUrl("mod_url_get_urls_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("URLs response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleUrlsResponse>(json, JsonOptions);
            return result ?? new MoodleUrlsResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching URLs for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleUrlsResponse();
        }
    }

    /// <summary>
    /// Get all labels for the specified courses
    /// API: mod_label_get_labels_by_courses
    /// </summary>
    public async Task<MoodleLabelsResponse> GetLabelsByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = BuildCourseIdsParameters(courseIds);
            var url = BuildApiUrl("mod_label_get_labels_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Labels response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleLabelsResponse>(json, JsonOptions);
            return result ?? new MoodleLabelsResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching labels for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleLabelsResponse();
        }
    }

    /// <summary>
    /// Get all quizzes for the specified courses
    /// API: mod_quiz_get_quizzes_by_courses
    /// </summary>
    public async Task<MoodleQuizzesResponse> GetQuizzesByCoursesAsync(params int[] courseIds)
    {
        try
        {
            var parameters = BuildCourseIdsParameters(courseIds);
            var url = BuildApiUrl("mod_quiz_get_quizzes_by_courses", parameters);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger?.LogDebug("Quizzes response: {Length} chars", json.Length);

            var result = JsonSerializer.Deserialize<MoodleQuizzesResponse>(json, JsonOptions);
            return result ?? new MoodleQuizzesResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching quizzes for courses {CourseIds}", string.Join(", ", courseIds));
            return new MoodleQuizzesResponse();
        }
    }

    /// <summary>
    /// Helper method to build course IDs parameters
    /// </summary>
    private static Dictionary<string, string> BuildCourseIdsParameters(int[] courseIds)
    {
        var parameters = new Dictionary<string, string>();
        for (int i = 0; i < courseIds.Length; i++)
        {
            parameters[$"courseids[{i}]"] = courseIds[i].ToString();
        }
        return parameters;
    }
}
