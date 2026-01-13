using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Helper-Methoden für Content-Files Verarbeitung
/// </summary>
public partial class MoodleSyncService
{
    private async Task ProcessContentFiles(
        int userId,
        int courseId,
        string? courseName,
        int parentId,
        string parentType,
        List<MoodleModuleContent>? files,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        if (files == null) return;

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Fileurl)) continue;

            var filePath = file.Filepath?.Trim('/') ?? "";
            var resourceKey = $"file_{parentType}_{parentId}_{filePath}{file.Filename}";

            if (!existingDict.ContainsKey(resourceKey))
            {
                var newFile = new MoodleResource
                {
                    UserId = userId,
                    CourseId = courseId,
                    CourseName = courseName,
                    MoodleResourceId = parentId * 10000 + Math.Abs(file.Filename?.GetHashCode() ?? 0) % 10000,
                    ResourceType = "file",
                    Title = file.Filename ?? "Unnamed File",
                    DownloadUrl = file.Fileurl,
                    FileType = file.Mimetype ?? Path.GetExtension(file.Filename),
                    FileSize = file.Filesize,
                    FilePath = string.IsNullOrEmpty(filePath) ? null : filePath,
                    MoodleTimeModified = file.Timemodified > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(file.Timemodified).UtcDateTime
                        : null,
                    SyncedAt = DateTime.UtcNow
                };

                _context.MoodleResources.Add(newFile);
                existingDict[resourceKey] = newFile;
                result.Added++;
            }
        }

        await Task.CompletedTask;
    }
}
