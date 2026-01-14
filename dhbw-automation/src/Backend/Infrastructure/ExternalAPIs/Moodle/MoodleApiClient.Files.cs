namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// MoodleApiClient - File download methods
/// </summary>
public partial class MoodleApiClient
{
    /// <summary>
    /// Download a file from Moodle
    /// Uses the token for authenticated access
    /// </summary>
    /// <param name="fileUrl">The Moodle file URL (can be relative or absolute)</param>
    /// <returns>File as byte array or null on error</returns>
    public async Task<MoodleFileDownloadResult> DownloadFileAsync(string fileUrl)
    {
        try
        {
            var downloadUrl = fileUrl;
            if (!fileUrl.Contains("token="))
            {
                var separator = fileUrl.Contains("?") ? "&" : "?";
                downloadUrl = $"{fileUrl}{separator}token={_token}";
            }

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
            _logger?.LogError(ex, "Error downloading file from {Url}", fileUrl);
            return new MoodleFileDownloadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Construct the download URL for a Moodle file
    /// </summary>
    public string GetFileDownloadUrl(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
            return string.Empty;

        if (fileUrl.Contains("pluginfile.php") && !fileUrl.Contains("token="))
        {
            var tokenUrl = fileUrl.Replace("/pluginfile.php", "/webservice/pluginfile.php");
            var separator = tokenUrl.Contains("?") ? "&" : "?";
            return $"{tokenUrl}{separator}token={_token}";
        }

        return fileUrl;
    }
}
