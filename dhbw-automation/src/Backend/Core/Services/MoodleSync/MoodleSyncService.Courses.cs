using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;
using Microsoft.EntityFrameworkCore;

// Type aliases to resolve ambiguous references
using MoodleCourseModel = DHBWAutomation.Backend.Core.Models.MoodleCourse;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Courses und Assignments Synchronisation
/// </summary>
public partial class MoodleSyncService
{
    public async Task<MoodleSyncResult> SyncCoursesAsync(int userId)
    {
        var result = new MoodleSyncResult { EntityType = "Courses" };

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

            var moodleUserId = user.MoodleUserId ?? 0;
            if (moodleUserId == 0)
            {
                var siteInfo = await _moodleClient.GetSiteInfoAsync();
                if (siteInfo != null)
                {
                    user.MoodleUserId = siteInfo.Userid;
                    moodleUserId = siteInfo.Userid;
                    await _context.SaveChangesAsync();
                }
            }

            _logger.LogInformation("Syncing courses for Moodle user {MoodleUserId}", moodleUserId);

            var courses = await _moodleClient.GetUserCoursesAsync(moodleUserId);
            var existingCourses = await _context.MoodleCourses
                .Where(c => c.UserId == userId)
                .ToDictionaryAsync(c => c.MoodleCourseId);

            foreach (var course in courses)
            {
                if (course.Id <= 0)
                {
                    _logger.LogWarning("Skipping course with invalid ID: {CourseId}, Name: {CourseName}", course.Id, course.Fullname);
                    continue;
                }

                if (existingCourses.TryGetValue(course.Id, out var existing))
                {
                    existing.Shortname = course.Shortname;
                    existing.Fullname = course.Fullname;
                    existing.Summary = course.Summary;
                    existing.Format = course.Format;
                    existing.StartDate = course.Startdate > 0 ? DateTimeOffset.FromUnixTimeSeconds(course.Startdate).UtcDateTime : null;
                    existing.EndDate = course.Enddate > 0 ? DateTimeOffset.FromUnixTimeSeconds(course.Enddate).UtcDateTime : null;
                    existing.Visible = course.Visible != 0;
                    existing.Progress = course.Progress;
                    existing.LastSynced = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newCourse = new MoodleCourseModel
                    {
                        UserId = userId,
                        MoodleCourseId = course.Id,
                        Shortname = course.Shortname,
                        Fullname = course.Fullname,
                        Summary = course.Summary,
                        Format = course.Format,
                        StartDate = course.Startdate > 0 ? DateTimeOffset.FromUnixTimeSeconds(course.Startdate).UtcDateTime : null,
                        EndDate = course.Enddate > 0 ? DateTimeOffset.FromUnixTimeSeconds(course.Enddate).UtcDateTime : null,
                        Visible = course.Visible != 0,
                        Progress = course.Progress,
                        LastSynced = DateTime.UtcNow
                    };
                    _context.MoodleCourses.Add(newCourse);
                    result.Added++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Course sync completed: {Added} added, {Updated} updated", result.Added, result.Updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing courses for user {UserId}", userId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<MoodleSyncResult> SyncAssignmentsAsync(int userId)
    {
        var result = new MoodleSyncResult { EntityType = "Assignments" };

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
                result.ErrorMessage = "Keine Kurse gefunden. Bitte erst Kurse synchronisieren.";
                return result;
            }

            var courseIds = courses.Select(c => c.MoodleCourseId).ToArray();
            var assignmentsResponse = await _moodleClient.GetAssignmentsAsync(courseIds);

            var existingAssignments = await _context.MoodleAssignments
                .Where(a => a.UserId == userId)
                .ToDictionaryAsync(a => a.MoodleAssignmentId);

            foreach (var courseDta in assignmentsResponse.Courses ?? Enumerable.Empty<MoodleAssignmentCourse>())
            {
                var localCourse = courses.FirstOrDefault(c => c.MoodleCourseId == courseDta.Id);

                foreach (var assignment in courseDta.Assignments ?? Enumerable.Empty<MoodleAssignmentData>())
                {
                    if (assignment.Id <= 0) continue;

                    if (existingAssignments.TryGetValue(assignment.Id, out var existing))
                    {
                        existing.Title = assignment.Name;
                        existing.Description = assignment.Intro;
                        existing.DueDate = assignment.Duedate > 0 ? DateTimeOffset.FromUnixTimeSeconds(assignment.Duedate).UtcDateTime : null;
                        existing.CutoffDate = assignment.Cutoffdate > 0 ? DateTimeOffset.FromUnixTimeSeconds(assignment.Cutoffdate).UtcDateTime : null;
                        existing.AllowSubmissionsFrom = assignment.Allowsubmissionsfromdate > 0 ? DateTimeOffset.FromUnixTimeSeconds(assignment.Allowsubmissionsfromdate).UtcDateTime : null;
                        existing.MaxGrade = assignment.Grade;
                        existing.CourseName = localCourse?.Fullname ?? courseDta.Fullname;
                        existing.SyncedAt = DateTime.UtcNow;
                        existing.UpdatedAt = DateTime.UtcNow;
                        result.Updated++;
                    }
                    else
                    {
                        var newAssignment = new MoodleAssignment
                        {
                            UserId = userId,
                            CourseId = assignment.Course,
                            CourseName = localCourse?.Fullname ?? courseDta.Fullname,
                            MoodleAssignmentId = assignment.Id,
                            Title = assignment.Name,
                            Description = assignment.Intro,
                            DueDate = assignment.Duedate > 0 ? DateTimeOffset.FromUnixTimeSeconds(assignment.Duedate).UtcDateTime : null,
                            CutoffDate = assignment.Cutoffdate > 0 ? DateTimeOffset.FromUnixTimeSeconds(assignment.Cutoffdate).UtcDateTime : null,
                            AllowSubmissionsFrom = assignment.Allowsubmissionsfromdate > 0 ? DateTimeOffset.FromUnixTimeSeconds(assignment.Allowsubmissionsfromdate).UtcDateTime : null,
                            MaxGrade = assignment.Grade,
                            SyncedAt = DateTime.UtcNow
                        };
                        _context.MoodleAssignments.Add(newAssignment);
                        result.Added++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Assignments sync completed: {Added} added, {Updated} updated", result.Added, result.Updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing assignments for user {UserId}", userId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}
