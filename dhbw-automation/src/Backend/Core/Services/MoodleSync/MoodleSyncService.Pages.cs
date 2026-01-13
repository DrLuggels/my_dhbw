using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

// Type aliases
using MoodleCourseModel = DHBWAutomation.Backend.Core.Models.MoodleCourse;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Pages, Folders, URLs und Labels Synchronisation
/// </summary>
public partial class MoodleSyncService
{
    private async Task SyncPagesAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var pagesResponse = await _moodleClient.GetPagesByCoursesAsync(courseIds);

            foreach (var page in pagesResponse.Pages ?? Enumerable.Empty<MoodlePageData>())
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == page.Course);
                var resourceKey = $"page_{page.Id}_";

                var timeModified = page.Timemodified > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(page.Timemodified).UtcDateTime
                    : (DateTime?)null;

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.HtmlContent = page.Content;
                    existing.Description = page.Intro;
                    existing.MoodleCourseModuleId = page.Coursemodule;
                    existing.MoodleTimeModified = timeModified;
                    existing.SectionNumber = page.Section;
                    existing.IsVisible = page.Visible;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;

                    await ProcessContentFiles(userId, page.Course, course?.Fullname, page.Id, "page",
                        page.Contentfiles, existingDict, result);
                }
                else
                {
                    var newPage = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = page.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = page.Id,
                        MoodleCourseModuleId = page.Coursemodule,
                        ResourceType = "page",
                        Title = page.Name,
                        Description = page.Intro,
                        HtmlContent = page.Content,
                        SectionNumber = page.Section,
                        IsVisible = page.Visible,
                        MoodleTimeModified = timeModified,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newPage);
                    existingDict[resourceKey] = newPage;
                    result.Added++;

                    await ProcessContentFiles(userId, page.Course, course?.Fullname, page.Id, "page",
                        page.Contentfiles, existingDict, result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync pages - API may not be available");
        }
    }

    private async Task SyncFoldersAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var foldersResponse = await _moodleClient.GetFoldersByCoursesAsync(courseIds);

            foreach (var folder in foldersResponse.Folders ?? Enumerable.Empty<MoodleFolderData>())
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == folder.Course);
                var resourceKey = $"folder_{folder.Id}_";

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.Description = folder.Intro;
                    existing.MoodleCourseModuleId = folder.Coursemodule;
                    existing.SectionNumber = folder.Section;
                    existing.IsVisible = folder.Visible;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newFolder = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = folder.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = folder.Id,
                        MoodleCourseModuleId = folder.Coursemodule,
                        ResourceType = "folder",
                        Title = folder.Name,
                        Description = folder.Intro,
                        SectionNumber = folder.Section,
                        IsVisible = folder.Visible,
                        MoodleTimeModified = folder.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(folder.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newFolder);
                    existingDict[resourceKey] = newFolder;
                    result.Added++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync folders - API may not be available");
        }
    }

    private async Task SyncUrlsAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var urlsResponse = await _moodleClient.GetUrlsByCoursesAsync(courseIds);

            foreach (var url in urlsResponse.Urls ?? Enumerable.Empty<MoodleUrlData>())
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == url.Course);
                var resourceKey = $"url_{url.Id}_";

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.ExternalUrl = url.Externalurl;
                    existing.Description = url.Intro;
                    existing.MoodleCourseModuleId = url.Coursemodule;
                    existing.SectionNumber = url.Section;
                    existing.IsVisible = url.Visible;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newUrl = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = url.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = url.Id,
                        MoodleCourseModuleId = url.Coursemodule,
                        ResourceType = "url",
                        Title = url.Name,
                        Description = url.Intro,
                        ExternalUrl = url.Externalurl,
                        SectionNumber = url.Section,
                        IsVisible = url.Visible,
                        MoodleTimeModified = url.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(url.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newUrl);
                    existingDict[resourceKey] = newUrl;
                    result.Added++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync URLs - API may not be available");
        }
    }

    private async Task SyncLabelsAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var labelsResponse = await _moodleClient.GetLabelsByCoursesAsync(courseIds);

            foreach (var label in labelsResponse.Labels ?? Enumerable.Empty<MoodleLabelData>())
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == label.Course);
                var resourceKey = $"label_{label.Id}_";

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.HtmlContent = label.Intro;
                    existing.MoodleCourseModuleId = label.Coursemodule;
                    existing.SectionNumber = label.Section;
                    existing.IsVisible = label.Visible;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newLabel = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = label.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = label.Id,
                        MoodleCourseModuleId = label.Coursemodule,
                        ResourceType = "label",
                        Title = label.Name,
                        HtmlContent = label.Intro,
                        SectionNumber = label.Section,
                        IsVisible = label.Visible,
                        MoodleTimeModified = label.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(label.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newLabel);
                    existingDict[resourceKey] = newLabel;
                    result.Added++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync labels - API may not be available");
        }
    }
}
