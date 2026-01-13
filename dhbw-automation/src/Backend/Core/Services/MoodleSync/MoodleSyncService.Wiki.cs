using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

// Type aliases
using MoodleCourseModel = DHBWAutomation.Backend.Core.Models.MoodleCourse;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Wikis und Quizzes Synchronisation
/// </summary>
public partial class MoodleSyncService
{
    private async Task SyncWikisAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var wikisResponse = await _moodleClient.GetWikisByCoursesAsync(courseIds);

            foreach (var wiki in wikisResponse.Wikis ?? Enumerable.Empty<MoodleWikiData>())
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == wiki.Course);
                var resourceKey = $"wiki_{wiki.Id}_";

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.Description = wiki.Intro;
                    existing.MoodleCourseModuleId = wiki.Coursemodule;
                    existing.SectionNumber = wiki.Section;
                    existing.IsVisible = wiki.Visible;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newWiki = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = wiki.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = wiki.Id,
                        MoodleCourseModuleId = wiki.Coursemodule,
                        ResourceType = "wiki",
                        Title = wiki.Name,
                        Description = wiki.Intro,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            wiki.Firstpagetitle,
                            wiki.Wikimode
                        }),
                        SectionNumber = wiki.Section,
                        IsVisible = wiki.Visible,
                        MoodleTimeModified = wiki.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(wiki.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newWiki);
                    existingDict[resourceKey] = newWiki;
                    result.Added++;
                }

                await SyncWikiPagesAsync(userId, wiki, course, existingDict, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync wikis - API may not be available");
        }
    }

    private async Task SyncWikiPagesAsync(
        int userId,
        MoodleWikiData wiki,
        MoodleCourseModel? course,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var pagesResponse = await _moodleClient.GetWikiPagesAsync(wiki.Id);

            foreach (var page in pagesResponse.Pages ?? Enumerable.Empty<MoodleWikiPage>())
            {
                var resourceKey = $"wiki_page_{page.Id}_";

                if (!existingDict.ContainsKey(resourceKey))
                {
                    var pageContent = await _moodleClient.GetWikiPageContentAsync(page.Id);

                    var newPage = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = wiki.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = page.Id,
                        ResourceType = "wiki_page",
                        Title = page.Title,
                        HtmlContent = pageContent?.Cachedcontent ?? page.Cachedcontent,
                        MoodleTimeModified = page.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(page.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };

                    if (existingDict.TryGetValue($"wiki_{wiki.Id}_", out var parentWiki))
                    {
                        newPage.ParentResourceId = parentWiki.Id;
                    }

                    _context.MoodleResources.Add(newPage);
                    existingDict[resourceKey] = newPage;
                    result.Added++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync wiki pages for wiki {WikiId}", wiki.Id);
        }
    }

    private async Task SyncQuizzesAsync(
        int userId,
        List<MoodleCourseModel> courses,
        int[] courseIds,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        try
        {
            var quizzesResponse = await _moodleClient.GetQuizzesByCoursesAsync(courseIds);

            foreach (var quiz in quizzesResponse.Quizzes ?? Enumerable.Empty<MoodleQuizData>())
            {
                var course = courses.FirstOrDefault(c => c.MoodleCourseId == quiz.Course);
                var resourceKey = $"quiz_{quiz.Id}_";

                var metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    quiz.Timelimit,
                    quiz.Attempts,
                    quiz.Grade,
                    TimeOpen = quiz.Timeopen > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(quiz.Timeopen).UtcDateTime
                        : (DateTime?)null,
                    TimeClose = quiz.Timeclose > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(quiz.Timeclose).UtcDateTime
                        : (DateTime?)null
                });

                if (existingDict.TryGetValue(resourceKey, out var existing))
                {
                    existing.Description = quiz.Intro;
                    existing.MoodleCourseModuleId = quiz.Coursemodule;
                    existing.SectionNumber = quiz.Section;
                    existing.IsVisible = quiz.Visible;
                    existing.Metadata = metadata;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newQuiz = new MoodleResource
                    {
                        UserId = userId,
                        CourseId = quiz.Course,
                        CourseName = course?.Fullname,
                        MoodleResourceId = quiz.Id,
                        MoodleCourseModuleId = quiz.Coursemodule,
                        ResourceType = "quiz",
                        Title = quiz.Name,
                        Description = quiz.Intro,
                        Metadata = metadata,
                        SectionNumber = quiz.Section,
                        IsVisible = quiz.Visible,
                        MoodleTimeModified = quiz.Timemodified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(quiz.Timemodified).UtcDateTime
                            : null,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleResources.Add(newQuiz);
                    existingDict[resourceKey] = newQuiz;
                    result.Added++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync quizzes - API may not be available");
        }
    }
}
