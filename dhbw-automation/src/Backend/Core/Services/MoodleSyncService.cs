using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service für die Synchronisation von Moodle-Daten
/// </summary>
public interface IMoodleSyncService
{
    /// <summary>
    /// Führt einen Login mit Username/Passwort durch und speichert den Token
    /// </summary>
    Task<MoodleLoginSyncResult> LoginAsync(int userId, string username, string password);

    /// <summary>
    /// Testet die Moodle-Verbindung für einen User
    /// </summary>
    Task<MoodleConnectionTestResult> TestConnectionAsync(int userId);

    /// <summary>
    /// Synchronisiert alle Kurse eines Users
    /// </summary>
    Task<MoodleSyncResult> SyncCoursesAsync(int userId);

    /// <summary>
    /// Synchronisiert Assignments für alle Kurse eines Users
    /// </summary>
    Task<MoodleSyncResult> SyncAssignmentsAsync(int userId);

    /// <summary>
    /// Synchronisiert Ressourcen/Materialien für alle Kurse eines Users
    /// </summary>
    Task<MoodleSyncResult> SyncResourcesAsync(int userId);

    /// <summary>
    /// Synchronisiert Kalender-Events eines Users
    /// </summary>
    Task<MoodleSyncResult> SyncCalendarEventsAsync(int userId);

    /// <summary>
    /// Führt eine vollständige Synchronisation durch
    /// </summary>
    Task<MoodleFullSyncResult> FullSyncAsync(int userId);

    /// <summary>
    /// Holt den Sync-Status für einen User
    /// </summary>
    Task<MoodleSyncStatus> GetSyncStatusAsync(int userId);
}

public class MoodleSyncService : IMoodleSyncService
{
    private readonly AppDbContext _context;
    private readonly MoodleApiClient _moodleClient;
    private readonly ILogger<MoodleSyncService> _logger;
    private readonly EncryptionHelper _encryptionHelper;

    public MoodleSyncService(
        AppDbContext context,
        MoodleApiClient moodleClient,
        ILogger<MoodleSyncService> logger,
        EncryptionHelper encryptionHelper)
    {
        _context = context;
        _moodleClient = moodleClient;
        _logger = logger;
        _encryptionHelper = encryptionHelper;
    }

    public async Task<MoodleLoginSyncResult> LoginAsync(int userId, string username, string password)
    {
        try
        {
            _logger.LogInformation("Moodle login attempt for user {UserId} with username {Username}", userId, username);

            var loginResult = await _moodleClient.LoginAndGetTokenAsync(username, password);

            if (!loginResult.Success || string.IsNullOrEmpty(loginResult.Token))
            {
                return new MoodleLoginSyncResult
                {
                    Success = false,
                    ErrorMessage = loginResult.ErrorMessage ?? "Login fehlgeschlagen"
                };
            }

            // Token speichern
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new MoodleLoginSyncResult
                {
                    Success = false,
                    ErrorMessage = "Benutzer nicht gefunden"
                };
            }

            // Moodle-User-Info abrufen
            _moodleClient.SetToken(loginResult.Token);
            var siteInfo = await _moodleClient.GetSiteInfoAsync();

            user.MoodleToken = _encryptionHelper.Encrypt(loginResult.Token);
            user.MoodleUsername = username;
            user.MoodlePassword = _encryptionHelper.Encrypt(password);
            user.MoodleUserId = siteInfo?.Userid;
            user.MoodleSyncEnabled = true;
            user.MoodleLastSyncError = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Moodle login successful for user {UserId}, MoodleUserId: {MoodleUserId}",
                userId, user.MoodleUserId);

            return new MoodleLoginSyncResult
            {
                Success = true,
                MoodleUserId = user.MoodleUserId,
                MoodleUsername = siteInfo?.Username,
                MoodleFullname = siteInfo?.Fullname
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Moodle login for user {UserId}", userId);
            return new MoodleLoginSyncResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<MoodleConnectionTestResult> TestConnectionAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.MoodleToken))
            {
                return new MoodleConnectionTestResult
                {
                    Success = false,
                    ErrorMessage = "Moodle nicht konfiguriert"
                };
            }

            var token = _encryptionHelper.Decrypt(user.MoodleToken);
            _moodleClient.SetToken(token);

            var siteInfo = await _moodleClient.GetSiteInfoAsync();
            if (siteInfo == null)
            {
                return new MoodleConnectionTestResult
                {
                    Success = false,
                    ErrorMessage = "Verbindung fehlgeschlagen - ungültiger Token?"
                };
            }

            return new MoodleConnectionTestResult
            {
                Success = true,
                SiteName = siteInfo.Sitename,
                Username = siteInfo.Username,
                Fullname = siteInfo.Fullname,
                UserId = siteInfo.Userid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Moodle connection for user {UserId}", userId);
            return new MoodleConnectionTestResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

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
                // Versuche User-ID zu holen
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
                if (existingCourses.TryGetValue(course.Id, out var existing))
                {
                    // Update
                    existing.Shortname = course.Shortname;
                    existing.Fullname = course.Fullname;
                    existing.Summary = course.Summary;
                    existing.Format = course.Format;
                    existing.StartDate = course.Startdate > 0 ? DateTimeOffset.FromUnixTimeSeconds(course.Startdate).UtcDateTime : null;
                    existing.EndDate = course.Enddate > 0 ? DateTimeOffset.FromUnixTimeSeconds(course.Enddate).UtcDateTime : null;
                    existing.Visible = course.Visible;
                    existing.Progress = course.Progress;
                    existing.LastSynced = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    // Insert
                    var newCourse = new MoodleCourse
                    {
                        UserId = userId,
                        MoodleCourseId = course.Id,
                        Shortname = course.Shortname,
                        Fullname = course.Fullname,
                        Summary = course.Summary,
                        Format = course.Format,
                        StartDate = course.Startdate > 0 ? DateTimeOffset.FromUnixTimeSeconds(course.Startdate).UtcDateTime : null,
                        EndDate = course.Enddate > 0 ? DateTimeOffset.FromUnixTimeSeconds(course.Enddate).UtcDateTime : null,
                        Visible = course.Visible,
                        Progress = course.Progress,
                        LastSynced = DateTime.UtcNow
                    };
                    _context.MoodleCourses.Add(newCourse);
                    result.Added++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Course sync completed: {Added} added, {Updated} updated",
                result.Added, result.Updated);
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

            // Hole alle Kurse des Users
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
                    if (existingAssignments.TryGetValue(assignment.Id, out var existing))
                    {
                        // Update
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
                        // Insert
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

            _logger.LogInformation("Assignments sync completed: {Added} added, {Updated} updated",
                result.Added, result.Updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing assignments for user {UserId}", userId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

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

            var existingResources = await _context.MoodleResources
                .Where(r => r.UserId == userId)
                .ToDictionaryAsync(r => r.MoodleResourceId);

            foreach (var course in courses)
            {
                _logger.LogDebug("Syncing resources for course {CourseName}", course.Fullname);

                var sections = await _moodleClient.GetCourseContentsAsync(course.MoodleCourseId);

                foreach (var section in sections)
                {
                    foreach (var module in section.Modules ?? Enumerable.Empty<MoodleModule>())
                    {
                        // Verarbeite Dateien im Modul
                        foreach (var content in module.Contents ?? Enumerable.Empty<MoodleModuleContent>())
                        {
                            if (content.Type != "file" || string.IsNullOrEmpty(content.Fileurl))
                                continue;

                            var resourceId = module.Id * 10000 + (content.Filename?.GetHashCode() ?? 0) % 10000;

                            if (existingResources.TryGetValue(resourceId, out var existing))
                            {
                                // Update nur wenn geändert
                                if (existing.UpdatedAt == null ||
                                    (content.Timemodified > 0 &&
                                     DateTimeOffset.FromUnixTimeSeconds(content.Timemodified).UtcDateTime > existing.UpdatedAt))
                                {
                                    existing.Title = content.Filename;
                                    existing.DownloadUrl = content.Fileurl;
                                    existing.FileSize = content.Filesize;
                                    existing.FileType = content.Mimetype ?? Path.GetExtension(content.Filename);
                                    existing.SyncedAt = DateTime.UtcNow;
                                    existing.UpdatedAt = DateTime.UtcNow;
                                    existing.IsDownloaded = false; // Neu herunterladen nötig
                                    result.Updated++;
                                }
                            }
                            else
                            {
                                // Insert
                                var newResource = new MoodleResource
                                {
                                    UserId = userId,
                                    CourseId = course.MoodleCourseId,
                                    CourseName = course.Fullname,
                                    MoodleResourceId = resourceId,
                                    ResourceType = module.Modname,
                                    Title = content.Filename,
                                    Description = module.Description,
                                    DownloadUrl = content.Fileurl,
                                    FileType = content.Mimetype ?? Path.GetExtension(content.Filename),
                                    FileSize = content.Filesize,
                                    SectionNumber = section.Section,
                                    SectionName = section.Name,
                                    SyncedAt = DateTime.UtcNow
                                };
                                _context.MoodleResources.Add(newResource);
                                result.Added++;
                            }
                        }

                        // Verarbeite URL-Ressourcen
                        if (module.Modname == "url" && !string.IsNullOrEmpty(module.Url))
                        {
                            var urlResourceId = module.Id;

                            if (!existingResources.ContainsKey(urlResourceId))
                            {
                                var urlResource = new MoodleResource
                                {
                                    UserId = userId,
                                    CourseId = course.MoodleCourseId,
                                    CourseName = course.Fullname,
                                    MoodleResourceId = urlResourceId,
                                    ResourceType = "url",
                                    Title = module.Name,
                                    Description = module.Description,
                                    ExternalUrl = module.Url,
                                    SectionNumber = section.Section,
                                    SectionName = section.Name,
                                    SyncedAt = DateTime.UtcNow
                                };
                                _context.MoodleResources.Add(urlResource);
                                result.Added++;
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Resources sync completed: {Added} added, {Updated} updated",
                result.Added, result.Updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing resources for user {UserId}", userId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<MoodleSyncResult> SyncCalendarEventsAsync(int userId)
    {
        var result = new MoodleSyncResult { EntityType = "CalendarEvents" };

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

            // Nächste 90 Tage
            var now = DateTimeOffset.UtcNow;
            var timeStart = now.ToUnixTimeSeconds();
            var timeEnd = now.AddDays(90).ToUnixTimeSeconds();

            var eventsResponse = await _moodleClient.GetCalendarEventsAsync(timeStart, timeEnd);
            var events = eventsResponse.Events ?? new List<MoodleCalendarEvent>();

            // Auch anstehende Events holen
            var upcomingEvents = await _moodleClient.GetUpcomingEventsAsync(100);
            events.AddRange(upcomingEvents);

            // Duplikate entfernen
            events = events.DistinctBy(e => e.Id).ToList();

            var existingEvents = await _context.MoodleCalendarEvents
                .Where(e => e.UserId == userId)
                .ToDictionaryAsync(e => e.MoodleEventId);

            foreach (var moodleEvent in events)
            {
                if (existingEvents.TryGetValue(moodleEvent.Id, out var existing))
                {
                    // Update
                    existing.Name = moodleEvent.Name;
                    existing.Description = moodleEvent.Description;
                    existing.EventType = moodleEvent.Eventtype;
                    existing.ModuleName = moodleEvent.Modulename;
                    existing.TimeStart = DateTimeOffset.FromUnixTimeSeconds(moodleEvent.Timestart).UtcDateTime;
                    existing.TimeDuration = moodleEvent.Timeduration;
                    existing.CourseId = moodleEvent.Courseid > 0 ? moodleEvent.Courseid : null;
                    existing.CourseName = moodleEvent.Course?.Fullname;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    // Insert
                    var newEvent = new Models.MoodleCalendarEvent
                    {
                        UserId = userId,
                        MoodleEventId = moodleEvent.Id,
                        Name = moodleEvent.Name,
                        Description = moodleEvent.Description,
                        EventType = moodleEvent.Eventtype,
                        ModuleName = moodleEvent.Modulename,
                        TimeStart = DateTimeOffset.FromUnixTimeSeconds(moodleEvent.Timestart).UtcDateTime,
                        TimeDuration = moodleEvent.Timeduration,
                        CourseId = moodleEvent.Courseid > 0 ? moodleEvent.Courseid : null,
                        CourseName = moodleEvent.Course?.Fullname,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleCalendarEvents.Add(newEvent);
                    result.Added++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Calendar events sync completed: {Added} added, {Updated} updated",
                result.Added, result.Updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing calendar events for user {UserId}", userId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<MoodleFullSyncResult> FullSyncAsync(int userId)
    {
        var fullResult = new MoodleFullSyncResult();

        try
        {
            _logger.LogInformation("Starting full Moodle sync for user {UserId}", userId);

            // 1. Kurse synchronisieren
            fullResult.CoursesResult = await SyncCoursesAsync(userId);

            // 2. Aufgaben synchronisieren
            fullResult.AssignmentsResult = await SyncAssignmentsAsync(userId);

            // 3. Ressourcen synchronisieren
            fullResult.ResourcesResult = await SyncResourcesAsync(userId);

            // 4. Kalender-Events synchronisieren
            fullResult.CalendarEventsResult = await SyncCalendarEventsAsync(userId);

            // User-Status aktualisieren
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.MoodleLastSync = DateTime.UtcNow;
                user.MoodleLastSyncError = fullResult.HasErrors ? fullResult.GetErrorSummary() : null;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            fullResult.Success = !fullResult.HasErrors;

            _logger.LogInformation("Full Moodle sync completed for user {UserId}. Success: {Success}",
                userId, fullResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full Moodle sync for user {UserId}", userId);
            fullResult.Success = false;
            fullResult.ErrorMessage = ex.Message;
        }

        return fullResult;
    }

    public async Task<MoodleSyncStatus> GetSyncStatusAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return new MoodleSyncStatus { IsConfigured = false };
        }

        var coursesCount = await _context.MoodleCourses.CountAsync(c => c.UserId == userId);
        var assignmentsCount = await _context.MoodleAssignments.CountAsync(a => a.UserId == userId);
        var resourcesCount = await _context.MoodleResources.CountAsync(r => r.UserId == userId);
        var eventsCount = await _context.MoodleCalendarEvents.CountAsync(e => e.UserId == userId);

        var pendingAssignments = await _context.MoodleAssignments
            .Where(a => a.UserId == userId && !a.IsSubmitted && a.DueDate > DateTime.UtcNow)
            .CountAsync();

        return new MoodleSyncStatus
        {
            IsConfigured = !string.IsNullOrEmpty(user.MoodleToken),
            IsSyncEnabled = user.MoodleSyncEnabled,
            MoodleUserId = user.MoodleUserId,
            MoodleUsername = user.MoodleUsername,
            LastSync = user.MoodleLastSync,
            LastSyncError = user.MoodleLastSyncError,
            CoursesCount = coursesCount,
            AssignmentsCount = assignmentsCount,
            ResourcesCount = resourcesCount,
            CalendarEventsCount = eventsCount,
            PendingAssignmentsCount = pendingAssignments
        };
    }
}

#region Result DTOs

public class MoodleLoginSyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? MoodleUserId { get; set; }
    public string? MoodleUsername { get; set; }
    public string? MoodleFullname { get; set; }
}

public class MoodleConnectionTestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SiteName { get; set; }
    public string? Username { get; set; }
    public string? Fullname { get; set; }
    public int UserId { get; set; }
}

public class MoodleSyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Deleted { get; set; }
}

public class MoodleFullSyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public MoodleSyncResult? CoursesResult { get; set; }
    public MoodleSyncResult? AssignmentsResult { get; set; }
    public MoodleSyncResult? ResourcesResult { get; set; }
    public MoodleSyncResult? CalendarEventsResult { get; set; }

    public bool HasErrors =>
        (CoursesResult != null && !CoursesResult.Success) ||
        (AssignmentsResult != null && !AssignmentsResult.Success) ||
        (ResourcesResult != null && !ResourcesResult.Success) ||
        (CalendarEventsResult != null && !CalendarEventsResult.Success);

    public string GetErrorSummary()
    {
        var errors = new List<string>();
        if (CoursesResult?.ErrorMessage != null) errors.Add($"Kurse: {CoursesResult.ErrorMessage}");
        if (AssignmentsResult?.ErrorMessage != null) errors.Add($"Aufgaben: {AssignmentsResult.ErrorMessage}");
        if (ResourcesResult?.ErrorMessage != null) errors.Add($"Ressourcen: {ResourcesResult.ErrorMessage}");
        if (CalendarEventsResult?.ErrorMessage != null) errors.Add($"Kalender: {CalendarEventsResult.ErrorMessage}");
        return string.Join("; ", errors);
    }
}

public class MoodleSyncStatus
{
    public bool IsConfigured { get; set; }
    public bool IsSyncEnabled { get; set; }
    public int? MoodleUserId { get; set; }
    public string? MoodleUsername { get; set; }
    public DateTime? LastSync { get; set; }
    public string? LastSyncError { get; set; }
    public int CoursesCount { get; set; }
    public int AssignmentsCount { get; set; }
    public int ResourcesCount { get; set; }
    public int CalendarEventsCount { get; set; }
    public int PendingAssignmentsCount { get; set; }
}

#endregion
