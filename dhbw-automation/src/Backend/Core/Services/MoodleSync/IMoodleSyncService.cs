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

    /// <summary>
    /// Lädt eine einzelne Moodle-Ressource herunter und erstellt ein Dokument
    /// </summary>
    Task<MoodleDownloadResult> DownloadResourceAsync(int resourceId, int userId);

    /// <summary>
    /// Lädt alle nicht heruntergeladenen Datei-Ressourcen herunter
    /// </summary>
    Task<MoodleBatchDownloadResult> DownloadAllResourcesAsync(int userId, bool processAfterDownload = true);
}
