using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

// Type aliases
using MoodleCourseModel = DHBWAutomation.Backend.Core.Models.MoodleCourse;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Books, Forums und Glossaries Synchronisation
/// </summary>
public partial class MoodleSyncService
{
    private async Task SyncBooksAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var booksResponse = await _moodleClient.GetBooksByCoursesAsync(courseIds);

            foreach (var book in booksResponse.Books ?? Enumerable.Empty<MoodleBookData>())
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == book.Course);
                var resourceKey = $"book_{book.Id}_";

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.Description = book.Intro;
                    existing.MoodleCourseModuleId = book.Coursemodule;
                    existing.SectionNumber = book.Section;
                    existing.IsVisible = book.Visible;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newBook = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = book.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = book.Id,
                        MoodleCourseModuleId = book.Coursemodule,
                        ResourceType = "book",
                        Title = book.Name,
                        Description = book.Intro,
                        SectionNumber = book.Section,
                        IsVisible = book.Visible,
                        MoodleTimeModified = book.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(book.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newBook);
                    existingDict[resourceKey] = newBook;
                    result.Added++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync books - API may not be available");
        }
    }

    private async Task SyncForumsAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var forums = await _moodleClient.GetForumsByCoursesAsync(courseIds);

            foreach (var forum in forums)
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == forum.Course);
                var resourceKey = $"forum_{forum.Id}_";

                var metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    forum.Type,
                    forum.Numdiscussions,
                    forum.Maxattachments
                });

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.Description = forum.Intro;
                    existing.MoodleCourseModuleId = forum.Cmid;
                    existing.SectionNumber = forum.Section;
                    existing.IsVisible = forum.Visible;
                    existing.Metadata = metadata;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newForum = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = forum.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = forum.Id,
                        MoodleCourseModuleId = forum.Cmid,
                        ResourceType = "forum",
                        Title = forum.Name,
                        Description = forum.Intro,
                        Metadata = metadata,
                        SectionNumber = forum.Section,
                        IsVisible = forum.Visible,
                        MoodleTimeModified = forum.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(forum.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newForum);
                    existingDict[resourceKey] = newForum;
                    result.Added++;
                }

                await SyncForumDiscussionsAsync(userId, forum, course?.Fullname, existingDict, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync forums - API may not be available");
        }
    }

    private async Task SyncForumDiscussionsAsync(
        int userId,
        MoodleForumData forum,
        string? courseName,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var discussionsResponse = await _moodleClient.GetForumDiscussionsAsync(forum.Id);

            foreach (var discussion in discussionsResponse.Discussions ?? Enumerable.Empty<MoodleForumDiscussion>())
            {
                var resourceKey = $"forum_discussion_{discussion.Id}_";

                if (!existingDict.ContainsKey(resourceKey))
                {
                    var newDiscussion = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = forum.Course,
                        CourseName = courseName,
                        MoodleResourceId = discussion.Id,
                        ResourceType = "forum_discussion",
                        Title = discussion.Subject,
                        HtmlContent = discussion.Message,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            discussion.Userfullname,
                            discussion.Numreplies,
                            Created = discussion.Created
                        }),
                        SyncedAt = DateTime.UtcNow
                    };

                    if (existingDict.TryGetValue($"forum_{forum.Id}_", out var parentForum))
                    {
                        newDiscussion.ParentResourceId = parentForum.Id;
                    }

                    _context.MoodleResources.Add(newDiscussion);
                    existingDict[resourceKey] = newDiscussion;
                    result.Added++;

                    await ProcessContentFiles(userId, forum.Course, courseName, discussion.Id, "forum_discussion",
                        discussion.Attachments, existingDict, result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync forum discussions for forum {ForumId}", forum.Id);
        }
    }

    private async Task SyncGlossariesAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var glossariesResponse = await _moodleClient.GetGlossariesByCoursesAsync(courseIds);

            foreach (var glossary in glossariesResponse.Glossaries ?? Enumerable.Empty<MoodleGlossaryData>())
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == glossary.Course);
                var resourceKey = $"glossary_{glossary.Id}_";

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.Description = glossary.Intro;
                    existing.MoodleCourseModuleId = glossary.Coursemodule;
                    existing.SectionNumber = glossary.Section;
                    existing.IsVisible = glossary.Visible;
                    existing.Metadata = System.Text.Json.JsonSerializer.Serialize(new { glossary.Entrycount });
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newGlossary = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = glossary.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = glossary.Id,
                        MoodleCourseModuleId = glossary.Coursemodule,
                        ResourceType = "glossary",
                        Title = glossary.Name,
                        Description = glossary.Intro,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new { glossary.Entrycount }),
                        SectionNumber = glossary.Section,
                        IsVisible = glossary.Visible,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newGlossary);
                    existingDict[resourceKey] = newGlossary;
                    result.Added++;
                }

                await SyncGlossaryEntriesAsync(userId, glossary, course?.Fullname, existingDict, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync glossaries - API may not be available");
        }
    }

    private async Task SyncGlossaryEntriesAsync(
        int userId,
        MoodleGlossaryData glossary,
        string? courseName,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var entriesResponse = await _moodleClient.GetGlossaryEntriesAsync(glossary.Id);

            foreach (var entry in entriesResponse.Entries ?? Enumerable.Empty<MoodleGlossaryEntry>())
            {
                var resourceKey = $"glossary_entry_{entry.Id}_";

                if (!existingDict.ContainsKey(resourceKey))
                {
                    var newEntry = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = glossary.Course,
                        CourseName = courseName,
                        MoodleResourceId = entry.Id,
                        ResourceType = "glossary_entry",
                        Title = entry.Concept,
                        HtmlContent = entry.Definition,
                        MoodleTimeModified = entry.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(entry.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };

                    if (existingDict.TryGetValue($"glossary_{glossary.Id}_", out var parentGlossary))
                    {
                        newEntry.ParentResourceId = parentGlossary.Id;
                    }

                    _context.MoodleResources.Add(newEntry);
                    existingDict[resourceKey] = newEntry;
                    result.Added++;

                    await ProcessContentFiles(userId, glossary.Course, courseName, entry.Id, "glossary_entry",
                        entry.Attachments, existingDict, result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync glossary entries for glossary {GlossaryId}", glossary.Id);
        }
    }
}
