using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Login, Test und Status Funktionen
/// </summary>
public partial class MoodleSyncService
{
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
