using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;
using Microsoft.EntityFrameworkCore;

// Type aliases
using MoodleCourseModel = DHBWAutomation.Backend.Core.Models.MoodleCourse;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Resources Haupt-Synchronisation
/// </summary>
public partial class MoodleSyncService
{
    public async Task<MoodleSyncResult> SyncResourcesAsync(int userId)
    {
        var result = new MoodleSyncResult { EntityType = "Resources" };

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.MoodleSyncEnabled || string.IsNullOrEmpty(user.MoodleToken))
            {
                result.ErrorMessage = "Moodle-Sync nicht aktiviert";
                return result;
            }

            var token = _encryptionHelper.Decrypt(user.MoodleToken);
            _moodleClient.SetToken(token);

            var courses = await _context.MoodleCourses
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!courses.Any())
            {
                result.ErrorMessage = "Keine Kurse gefunden";
                return result;
            }

            var courseIds = courses.Select(c => c.MoodleCourseId).ToArray();

            var existingResources = await _context.MoodleResources
                .Where(r => r.UserId == userId)
                .ToListAsync();
            // Key format: {ResourceType}_{MoodleResourceId}_{FilePath}{Title}
            // This matches the unique index: UserId + ResourceType + MoodleResourceId + FilePath + Title
            var existingDict = existingResources.ToDictionary(
                r => $"{r.ResourceType}_{r.MoodleResourceId}_{r.FilePath ?? ""}{r.Title ?? ""}");

            _logger.LogInformation("Starting comprehensive resource sync for {CourseCount} courses", courses.Count);

            foreach (var course in courses)
            {
                _logger.LogDebug("Syncing course contents for {CourseName}", course.Fullname);
                var sections = await _moodleClient.GetCourseContentsAsync(course.MoodleCourseId);

                foreach (var section in sections)
                {
                    foreach (var module in section.Modules ?? Enumerable.Empty<MoodleModule>())
                    {
                        await ProcessModuleContents(userId, course, section, module, existingDict, result);
                        await ProcessSpecialModule(userId, course, section, module, existingDict, result);
                    }
                }
            }

            await SyncPagesAsync(userId, courses, courseIds, existingDict, result);
            await SyncFoldersAsync(userId, courses, courseIds, existingDict, result);
            await SyncUrlsAsync(userId, courses, courseIds, existingDict, result);
            await SyncLabelsAsync(userId, courses, courseIds, existingDict, result);
            await SyncBooksAsync(userId, courses, courseIds, existingDict, result);
            await SyncForumsAsync(userId, courses, courseIds, existingDict, result);
            await SyncGlossariesAsync(userId, courses, courseIds, existingDict, result);
            await SyncWikisAsync(userId, courses, courseIds, existingDict, result);
            await SyncQuizzesAsync(userId, courses, courseIds, existingDict, result);

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Resources sync completed: {Added} added, {Updated} updated", result.Added, result.Updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing resources for user {UserId}", userId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task ProcessModuleContents(
        int userId,
        MoodleCourseModel course,
        MoodleCourseSection section,
        MoodleModule module,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        foreach (var content in module.Contents ?? Enumerable.Empty<MoodleModuleContent>())
        {
            if (string.IsNullOrEmpty(content.Fileurl))
                continue;

            var filePath = content.Filepath?.Trim('/') ?? "";
            var resourceKey = $"file_{module.Id}_{filePath}{content.Filename}";
            var timeModified = content.Timemodified > 0
                ? DateTimeOffset.FromUnixTimeSeconds(content.Timemodified).UtcDateTime
                : (DateTime?)null;

            if (existingDict.TryGetValue(resourceKey, out var existing))
            {
                if (timeModified.HasValue && (existing.MoodleTimeModified == null || timeModified > existing.MoodleTimeModified))
                {
                    existing.Title = content.Filename;
                    existing.DownloadUrl = content.Fileurl;
                    existing.FileSize = content.Filesize;
                    existing.FileType = content.Mimetype ?? Path.GetExtension(content.Filename);
                    existing.FilePath = string.IsNullOrEmpty(filePath) ? null : filePath;
                    existing.MoodleTimeModified = timeModified;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.IsDownloaded = false;
                    result.Updated++;
                }
            }
            else
            {
                var newResource = new MoodleResource
                {
                    UserId = userId,
                    CourseId = course.MoodleCourseId,
                    CourseName = course.Fullname,
                    MoodleResourceId = module.Id,
                    MoodleCourseModuleId = module.Id,
                    ResourceType = "file",
                    Title = content.Filename,
                    Description = module.Description,
                    DownloadUrl = content.Fileurl,
                    FileType = content.Mimetype ?? Path.GetExtension(content.Filename),
                    FileSize = content.Filesize,
                    FilePath = string.IsNullOrEmpty(filePath) ? null : filePath,
                    SectionNumber = section.Section,
                    SectionName = section.Name,
                    IsVisible = module.Visible,
                    MoodleTimeModified = timeModified,
                    SyncedAt = DateTime.UtcNow
                };
                _context.MoodleResources.Add(newResource);
                existingDict[resourceKey] = newResource;
                result.Added++;
            }
        }

        await Task.CompletedTask;
    }

    private async Task ProcessSpecialModule(
        int userId,
        MoodleCourseModel course,
        MoodleCourseSection section,
        MoodleModule module,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        // Key format: {ResourceType}_{MoodleResourceId}_{FilePath}{Title}
        // For non-file resources: no FilePath, Title is module.Name
        var resourceKey = $"{module.Modname}_{module.Instance ?? module.Id}_{module.Name ?? ""}";

        if (existingDict.ContainsKey(resourceKey))
            return;

        MoodleResource? newResource = module.Modname switch
        {
            "url" when !string.IsNullOrEmpty(module.Url) => new MoodleResource
            {
                UserId = userId,
                CourseId = course.MoodleCourseId,
                CourseName = course.Fullname,
                MoodleResourceId = module.Instance ?? module.Id,
                MoodleCourseModuleId = module.Id,
                ResourceType = "url",
                Title = module.Name,
                Description = module.Description,
                ExternalUrl = module.Url,
                SectionNumber = section.Section,
                SectionName = section.Name,
                IsVisible = module.Visible,
                SyncedAt = DateTime.UtcNow
            },
            "page" or "label" or "folder" or "book" or "wiki" or "glossary" or "forum" or "quiz" or "assign" => new MoodleResource
            {
                UserId = userId,
                CourseId = course.MoodleCourseId,
                CourseName = course.Fullname,
                MoodleResourceId = module.Instance ?? module.Id,
                MoodleCourseModuleId = module.Id,
                ResourceType = module.Modname,
                Title = module.Name,
                Description = module.Description,
                HtmlContent = module.Modname == "page" || module.Modname == "label" ? module.Description : null,
                SectionNumber = section.Section,
                SectionName = section.Name,
                IsVisible = module.Visible,
                SyncedAt = DateTime.UtcNow
            },
            _ => null
        };

        if (newResource != null)
        {
            _context.MoodleResources.Add(newResource);
            existingDict[resourceKey] = newResource;
            result.Added++;
        }

        await Task.CompletedTask;
    }
}
