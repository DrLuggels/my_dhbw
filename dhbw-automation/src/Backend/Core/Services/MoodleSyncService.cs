// ===================================================================
// DIESES FILE WURDE REFACTORIERT IN KLEINERE MODULE
// Die Implementierung befindet sich nun in:
// - MoodleSync/IMoodleSyncService.cs
// - MoodleSync/MoodleSyncService.Base.cs
// - MoodleSync/MoodleSyncService.Core.cs
// - MoodleSync/MoodleSyncService.Courses.cs
// - MoodleSync/MoodleSyncService.Resources.cs
// - MoodleSync/MoodleSyncService.Pages.cs (wird noch erstellt)
// - MoodleSync/MoodleSyncService.Interactive.cs (wird noch erstellt)
// - MoodleSync/MoodleSyncService.Wiki.cs (wird noch erstellt)
// - MoodleSync/MoodleSyncService.Calendar.cs (wird noch erstellt)
// - MoodleSync/MoodleSyncService.Helpers.cs (wird noch erstellt)
// - MoodleSync/MoodleSyncModels.cs
// ===================================================================
// Diese Datei bleibt temporär zur Rückwärtskompatibilität bestehen
// und sollte nach vollständiger Migration gelöscht werden.
// ===================================================================

using DHBWAutomation.Backend.Core.Services.MoodleSync;

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
                // Skip invalid courses (Id must be > 0)
                if (course.Id <= 0)
                {
                    _logger.LogWarning("Skipping course with invalid ID: {CourseId}, Name: {CourseName}", course.Id, course.Fullname);
                    continue;
                }

                if (existingCourses.TryGetValue(course.Id, out var existing))
                {
                    // Update
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
                    // Insert
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
                    // Skip invalid assignments
                    if (assignment.Id <= 0) continue;

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

            var courseIds = courses.Select(c => c.MoodleCourseId).ToArray();

            // Lade existierende Ressourcen mit zusammengesetztem Key (Type + MoodleResourceId)
            var existingResources = await _context.MoodleResources
                .Where(r => r.UserId == userId)
                .ToListAsync();
            var existingDict = existingResources.ToDictionary(r => $"{r.ResourceType}_{r.MoodleResourceId}_{r.FilePath ?? ""}");

            _logger.LogInformation("Starting comprehensive resource sync for {CourseCount} courses", courses.Count);

            // 1. Sync über core_course_get_contents (alle Module mit Dateien)
            foreach (var course in courses)
            {
                _logger.LogDebug("Syncing course contents for {CourseName}", course.Fullname);
                var sections = await _moodleClient.GetCourseContentsAsync(course.MoodleCourseId);

                foreach (var section in sections)
                {
                    foreach (var module in section.Modules ?? Enumerable.Empty<MoodleModule>())
                    {
                        // Verarbeite alle Module mit Contents (Dateien)
                        await ProcessModuleContents(userId, course, section, module, existingDict, result);

                        // Verarbeite spezielle Modultypen
                        await ProcessSpecialModule(userId, course, section, module, existingDict, result);
                    }
                }
            }

            // 2. Sync über modulspezifische APIs für detaillierte Daten
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

    /// <summary>
    /// Verarbeitet alle Dateien innerhalb eines Moduls (Contents)
    /// </summary>
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
            // Akzeptiere alles mit FileUrl (nicht nur Type="file")
            if (string.IsNullOrEmpty(content.Fileurl))
                continue;

            var filePath = content.Filepath?.Trim('/') ?? "";
            var resourceKey = $"file_{module.Id}_{filePath}{content.Filename}";
            var timeModified = content.Timemodified > 0
                ? DateTimeOffset.FromUnixTimeSeconds(content.Timemodified).UtcDateTime
                : (DateTime?)null;

            if (existingDict.TryGetValue(resourceKey, out var existing))
            {
                // Update wenn geändert
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

    /// <summary>
    /// Verarbeitet spezielle Modultypen (URL, Page, etc.) aus course_contents
    /// </summary>
    private async Task ProcessSpecialModule(
        int userId,
        MoodleCourseModel course,
        MoodleCourseSection section,
        MoodleModule module,
        Dictionary<string, MoodleResource> existingDict,
        MoodleSyncResult result)
    {
        var resourceKey = $"{module.Modname}_{module.Instance ?? module.Id}_";

        // Prüfe ob bereits verarbeitet
        if (existingDict.ContainsKey(resourceKey))
            return;

        MoodleResource? newResource = null;

        switch (module.Modname)
        {
            case "url":
                if (!string.IsNullOrEmpty(module.Url))
                {
                    newResource = new MoodleResource
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
                    };
                }
                break;

            case "page":
            case "label":
                newResource = new MoodleResource
                {
                    UserId = userId,
                    CourseId = course.MoodleCourseId,
                    CourseName = course.Fullname,
                    MoodleResourceId = module.Instance ?? module.Id,
                    MoodleCourseModuleId = module.Id,
                    ResourceType = module.Modname,
                    Title = module.Name,
                    Description = module.Description,
                    HtmlContent = module.Description, // Grundlegende Info aus course_contents
                    SectionNumber = section.Section,
                    SectionName = section.Name,
                    IsVisible = module.Visible,
                    SyncedAt = DateTime.UtcNow
                };
                break;

            case "folder":
            case "book":
            case "wiki":
            case "glossary":
            case "forum":
            case "quiz":
            case "assign":
                // Diese werden durch spezielle API-Calls verarbeitet
                // Hier nur als Platzhalter registrieren
                newResource = new MoodleResource
                {
                    UserId = userId,
                    CourseId = course.MoodleCourseId,
                    CourseName = course.Fullname,
                    MoodleResourceId = module.Instance ?? module.Id,
                    MoodleCourseModuleId = module.Id,
                    ResourceType = module.Modname,
                    Title = module.Name,
                    Description = module.Description,
                    SectionNumber = section.Section,
                    SectionName = section.Name,
                    IsVisible = module.Visible,
                    SyncedAt = DateTime.UtcNow
                };
                break;
        }

        if (newResource != null)
        {
            _context.MoodleResources.Add(newResource);
            existingDict[resourceKey] = newResource;
            result.Added++;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Sync Seiten (Pages) mit detailliertem Content
    /// </summary>
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
                    // Update mit detailliertem Content
                    existing.HtmlContent = page.Content;
                    existing.Description = page.Intro;
                    existing.MoodleCourseModuleId = page.Coursemodule;
                    existing.MoodleTimeModified = timeModified;
                    existing.SectionNumber = page.Section;
                    existing.IsVisible = page.Visible;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;

                    // Verarbeite Content-Files
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

    /// <summary>
    /// Sync Ordner (Folders) - Dateien werden bereits durch course_contents erfasst
    /// </summary>
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

    /// <summary>
    /// Sync URLs mit detaillierten Daten
    /// </summary>
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

    /// <summary>
    /// Sync Labels (Textblöcke)
    /// </summary>
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

    /// <summary>
    /// Sync Bücher (Books)
    /// </summary>
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

    /// <summary>
    /// Sync Foren (Forums) mit Diskussionen
    /// </summary>
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

                // Sync Forum-Diskussionen
                await SyncForumDiscussionsAsync(userId, forum, course?.Fullname, existingDict, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync forums - API may not be available");
        }
    }

    /// <summary>
    /// Sync Forum-Diskussionen
    /// </summary>
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

                    // Finde Parent-Forum
                    if (existingDict.TryGetValue($"forum_{forum.Id}_", out var parentForum))
                    {
                        newDiscussion.ParentResourceId = parentForum.Id;
                    }

                    _context.MoodleResources.Add(newDiscussion);
                    existingDict[resourceKey] = newDiscussion;
                    result.Added++;

                    // Verarbeite Anhänge
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

    /// <summary>
    /// Sync Glossare (Glossaries)
    /// </summary>
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

                // Sync Glossar-Einträge
                await SyncGlossaryEntriesAsync(userId, glossary, course?.Fullname, existingDict, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync glossaries - API may not be available");
        }
    }

    /// <summary>
    /// Sync Glossar-Einträge
    /// </summary>
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

                    // Finde Parent-Glossar
                    if (existingDict.TryGetValue($"glossary_{glossary.Id}_", out var parentGlossary))
                    {
                        newEntry.ParentResourceId = parentGlossary.Id;
                    }

                    _context.MoodleResources.Add(newEntry);
                    existingDict[resourceKey] = newEntry;
                    result.Added++;

                    // Verarbeite Anhänge
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

    /// <summary>
    /// Sync Wikis
    /// </summary>
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

                // Sync Wiki-Seiten
                await SyncWikiPagesAsync(userId, wiki, course, existingDict, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync wikis - API may not be available");
        }
    }

    /// <summary>
    /// Sync Wiki-Seiten
    /// </summary>
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
                    // Hole detaillierten Seiteninhalt
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

                    // Finde Parent-Wiki
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

    /// <summary>
    /// Sync Quizze (Quizzes)
    /// </summary>
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

    /// <summary>
    /// Verarbeitet Content-Files (Anhänge) für verschiedene Modultypen
    /// </summary>
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
                    Title = file.Filename,
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
            var events = eventsResponse.Events ?? new List<MoodleApiCalendarEvent>();

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
