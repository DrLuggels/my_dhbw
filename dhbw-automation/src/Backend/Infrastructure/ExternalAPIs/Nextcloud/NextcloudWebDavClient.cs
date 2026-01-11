using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Nextcloud;

/// <summary>
/// WebDAV client for Nextcloud file operations
/// </summary>
public class NextcloudWebDavClient : INextcloudWebDavClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NextcloudWebDavClient> _logger;

    public NextcloudWebDavClient(
        IHttpClientFactory httpClientFactory,
        ILogger<NextcloudWebDavClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Test connection to Nextcloud
    /// </summary>
    public async Task<bool> TestConnectionAsync(string baseUrl, string username, string password)
    {
        try
        {
            var client = CreateClient(baseUrl, username, password);
            var webdavUrl = GetWebDavUrl(baseUrl, username);

            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), webdavUrl);
            request.Headers.Add("Depth", "0");
            request.Content = new StringContent(GetPropfindBody(), Encoding.UTF8, "application/xml");

            var response = await client.SendAsync(request);

            return response.StatusCode == HttpStatusCode.MultiStatus ||
                   response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Nextcloud connection to {BaseUrl}", baseUrl);
            return false;
        }
    }

    /// <summary>
    /// List files and folders in a directory
    /// </summary>
    public async Task<List<NextcloudFileInfo>> ListDirectoryAsync(
        string baseUrl,
        string username,
        string password,
        string remotePath = "/")
    {
        var files = new List<NextcloudFileInfo>();

        try
        {
            var client = CreateClient(baseUrl, username, password);
            var webdavUrl = GetWebDavUrl(baseUrl, username) + NormalizePath(remotePath);

            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), webdavUrl);
            request.Headers.Add("Depth", "1");
            request.Content = new StringContent(GetPropfindBody(), Encoding.UTF8, "application/xml");

            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.MultiStatus)
            {
                _logger.LogWarning("Unexpected status code from Nextcloud: {StatusCode}", response.StatusCode);
                return files;
            }

            var content = await response.Content.ReadAsStringAsync();
            files = ParsePropfindResponse(content, remotePath);

            _logger.LogDebug("Listed {Count} files in {Path}", files.Count, remotePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directory {Path}", remotePath);
        }

        return files;
    }

    /// <summary>
    /// Recursively list all files in a directory and subdirectories
    /// </summary>
    public async Task<List<NextcloudFileInfo>> ListAllFilesRecursiveAsync(
        string baseUrl,
        string username,
        string password,
        string remotePath = "/",
        string[]? fileTypes = null)
    {
        var allFiles = new List<NextcloudFileInfo>();

        try
        {
            var items = await ListDirectoryAsync(baseUrl, username, password, remotePath);

            foreach (var item in items)
            {
                if (item.IsDirectory)
                {
                    // Recurse into subdirectory
                    var subFiles = await ListAllFilesRecursiveAsync(
                        baseUrl, username, password, item.Path, fileTypes);
                    allFiles.AddRange(subFiles);
                }
                else
                {
                    // Filter by file type if specified
                    if (fileTypes == null || fileTypes.Length == 0)
                    {
                        allFiles.Add(item);
                    }
                    else if (fileTypes.Any(ft => item.Name.EndsWith($".{ft}", StringComparison.OrdinalIgnoreCase)))
                    {
                        allFiles.Add(item);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files recursively from {Path}", remotePath);
        }

        return allFiles;
    }

    /// <summary>
    /// Download a file from Nextcloud
    /// </summary>
    public async Task<byte[]?> DownloadFileAsync(
        string baseUrl,
        string username,
        string password,
        string remotePath)
    {
        try
        {
            var client = CreateClient(baseUrl, username, password);
            var webdavUrl = GetWebDavUrl(baseUrl, username) + NormalizePath(remotePath);

            var response = await client.GetAsync(webdavUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download file {Path}: {StatusCode}",
                    remotePath, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            _logger.LogDebug("Downloaded file {Path} ({Size} bytes)", remotePath, content.Length);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {Path}", remotePath);
            return null;
        }
    }

    /// <summary>
    /// Download a file to a stream
    /// </summary>
    public async Task<Stream?> DownloadFileAsStreamAsync(
        string baseUrl,
        string username,
        string password,
        string remotePath)
    {
        try
        {
            var client = CreateClient(baseUrl, username, password);
            var webdavUrl = GetWebDavUrl(baseUrl, username) + NormalizePath(remotePath);

            var response = await client.GetAsync(webdavUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download file stream {Path}: {StatusCode}",
                    remotePath, response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file stream {Path}", remotePath);
            return null;
        }
    }

    /// <summary>
    /// Get file properties (including ETag for change detection)
    /// </summary>
    public async Task<NextcloudFileInfo?> GetFilePropertiesAsync(
        string baseUrl,
        string username,
        string password,
        string remotePath)
    {
        try
        {
            var client = CreateClient(baseUrl, username, password);
            var webdavUrl = GetWebDavUrl(baseUrl, username) + NormalizePath(remotePath);

            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), webdavUrl);
            request.Headers.Add("Depth", "0");
            request.Content = new StringContent(GetPropfindBody(), Encoding.UTF8, "application/xml");

            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.MultiStatus)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var files = ParsePropfindResponse(content, remotePath);

            return files.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file properties for {Path}", remotePath);
            return null;
        }
    }

    private HttpClient CreateClient(string baseUrl, string username, string password)
    {
        var client = _httpClientFactory.CreateClient("Nextcloud");
        client.BaseAddress = new Uri(baseUrl);

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        client.Timeout = TimeSpan.FromMinutes(5); // For large file downloads

        return client;
    }

    private static string GetWebDavUrl(string baseUrl, string username)
    {
        // Nextcloud WebDAV path: /remote.php/dav/files/{username}/
        return $"{baseUrl.TrimEnd('/')}/remote.php/dav/files/{username}";
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "/";

        path = path.Replace("\\", "/");
        if (!path.StartsWith("/"))
            path = "/" + path;

        return path;
    }

    private static string GetPropfindBody()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<d:propfind xmlns:d=""DAV:"" xmlns:oc=""http://owncloud.org/ns"" xmlns:nc=""http://nextcloud.org/ns"">
    <d:prop>
        <d:displayname/>
        <d:getcontenttype/>
        <d:getcontentlength/>
        <d:getlastmodified/>
        <d:getetag/>
        <d:resourcetype/>
        <oc:fileid/>
    </d:prop>
</d:propfind>";
    }

    private List<NextcloudFileInfo> ParsePropfindResponse(string xml, string basePath)
    {
        var files = new List<NextcloudFileInfo>();

        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace d = "DAV:";
            XNamespace oc = "http://owncloud.org/ns";

            var responses = doc.Descendants(d + "response");

            foreach (var response in responses)
            {
                var href = response.Element(d + "href")?.Value ?? "";
                var propstat = response.Element(d + "propstat");
                var prop = propstat?.Element(d + "prop");

                if (prop == null) continue;

                var displayName = prop.Element(d + "displayname")?.Value ?? "";
                var contentType = prop.Element(d + "getcontenttype")?.Value ?? "";
                var contentLength = long.TryParse(prop.Element(d + "getcontentlength")?.Value, out var len) ? len : 0;
                var lastModified = prop.Element(d + "getlastmodified")?.Value;
                var etag = prop.Element(d + "getetag")?.Value?.Trim('"');
                var resourceType = prop.Element(d + "resourcetype");
                var isDirectory = resourceType?.Element(d + "collection") != null;
                var fileId = prop.Element(oc + "fileid")?.Value;

                // Extract path from href
                var path = Uri.UnescapeDataString(href);
                var pathIndex = path.IndexOf("/remote.php/dav/files/");
                if (pathIndex >= 0)
                {
                    var afterPrefix = path.Substring(pathIndex + "/remote.php/dav/files/".Length);
                    var slashIndex = afterPrefix.IndexOf('/');
                    if (slashIndex >= 0)
                    {
                        path = afterPrefix.Substring(slashIndex);
                    }
                    else
                    {
                        path = "/";
                    }
                }

                // Skip the base path itself
                if (path.TrimEnd('/') == basePath.TrimEnd('/'))
                    continue;

                // Extract just the filename
                var name = string.IsNullOrEmpty(displayName)
                    ? Path.GetFileName(path.TrimEnd('/'))
                    : displayName;

                files.Add(new NextcloudFileInfo
                {
                    Name = name,
                    Path = path.TrimEnd('/'),
                    ContentType = contentType,
                    Size = contentLength,
                    LastModified = DateTime.TryParse(lastModified, out var dt) ? dt : DateTime.UtcNow,
                    ETag = etag,
                    IsDirectory = isDirectory,
                    FileId = fileId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing PROPFIND response");
        }

        return files;
    }
}

/// <summary>
/// File information from Nextcloud
/// </summary>
public class NextcloudFileInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string? ETag { get; set; }
    public bool IsDirectory { get; set; }
    public string? FileId { get; set; }

    public string Extension => System.IO.Path.GetExtension(Name).TrimStart('.').ToLower();
}

/// <summary>
/// Interface for Nextcloud WebDAV operations
/// </summary>
public interface INextcloudWebDavClient
{
    Task<bool> TestConnectionAsync(string baseUrl, string username, string password);
    Task<List<NextcloudFileInfo>> ListDirectoryAsync(string baseUrl, string username, string password, string remotePath = "/");
    Task<List<NextcloudFileInfo>> ListAllFilesRecursiveAsync(string baseUrl, string username, string password, string remotePath = "/", string[]? fileTypes = null);
    Task<byte[]?> DownloadFileAsync(string baseUrl, string username, string password, string remotePath);
    Task<Stream?> DownloadFileAsStreamAsync(string baseUrl, string username, string password, string remotePath);
    Task<NextcloudFileInfo?> GetFilePropertiesAsync(string baseUrl, string username, string password, string remotePath);
}
