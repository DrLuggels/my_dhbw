using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Nextcloud;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for synchronizing files from Nextcloud
/// </summary>
public class NextcloudSyncService : INextcloudSyncService
{
    private readonly INextcloudWebDavClient _webDavClient;
    private readonly IFileService _fileService;
    private readonly IStorageService _storageService;
    private readonly AppDbContext _context;
    private readonly EncryptionHelper _encryptionHelper;
    private readonly ILogger<NextcloudSyncService> _logger;

    // File types to sync
    private static readonly string[] SupportedFileTypes = { "pdf", "docx", "doc", "pptx", "ppt", "xlsx", "xls", "txt", "md" };

    public NextcloudSyncService(
        INextcloudWebDavClient webDavClient,
        IFileService fileService,
        IStorageService storageService,
        AppDbContext context,
        EncryptionHelper encryptionHelper,
        ILogger<NextcloudSyncService> logger)
    {
        _webDavClient = webDavClient;
        _fileService = fileService;
        _storageService = storageService;
        _context = context;
        _encryptionHelper = encryptionHelper;
        _logger = logger;
    }

    /// <summary>
    /// Save or update Nextcloud credentials
    /// </summary>
    public async Task<NextcloudCredential> SaveCredentialsAsync(
        int userId,
        string nextcloudUrl,
        string username,
        string password)
    {
        var existing = await _context.NextcloudCredentials
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (existing != null)
        {
            existing.NextcloudUrl = nextcloudUrl;
            existing.Username = username;
            existing.EncryptedPassword = _encryptionHelper.Encrypt(password);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new NextcloudCredential
            {
                UserId = userId,
                NextcloudUrl = nextcloudUrl,
                Username = username,
                EncryptedPassword = _encryptionHelper.Encrypt(password)
            };
            _context.NextcloudCredentials.Add(existing);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved Nextcloud credentials for user {UserId}", userId);

        return existing;
    }

    /// <summary>
    /// Test Nextcloud connection with credentials
    /// </summary>
    public async Task<bool> TestConnectionAsync(int userId)
    {
        var credential = await GetCredentialAsync(userId);
        if (credential == null)
        {
            return false;
        }

        var password = _encryptionHelper.Decrypt(credential.EncryptedPassword);
        return await _webDavClient.TestConnectionAsync(credential.NextcloudUrl, credential.Username, password);
    }

    /// <summary>
    /// Sync all files for a user
    /// </summary>
    public async Task<SyncResult> SyncUserFilesAsync(int userId)
    {
        var result = new SyncResult();

        try
        {
            var credential = await GetCredentialAsync(userId);
            if (credential == null)
            {
                result.Error = "No Nextcloud credentials found";
                return result;
            }

            var password = _encryptionHelper.Decrypt(credential.EncryptedPassword);

            // Get all files from Nextcloud
            var remoteFiles = await _webDavClient.ListAllFilesRecursiveAsync(
                credential.NextcloudUrl,
                credential.Username,
                password,
                "/",
                SupportedFileTypes
            );

            _logger.LogInformation("Found {Count} files in Nextcloud for user {UserId}",
                remoteFiles.Count, userId);

            // Get existing tracked files
            var existingFiles = await _context.NextcloudFiles
                .Where(f => f.CredentialId == credential.Id)
                .ToListAsync();

            var existingPaths = existingFiles.ToDictionary(f => f.RemotePath);

            foreach (var remoteFile in remoteFiles)
            {
                try
                {
                    if (existingPaths.TryGetValue(remoteFile.Path, out var existingFile))
                    {
                        // Check if file has changed (ETag comparison)
                        if (existingFile.ETag != remoteFile.ETag)
                        {
                            await UpdateFileAsync(credential, existingFile, remoteFile, password);
                            result.Updated++;
                        }
                        else
                        {
                            result.Skipped++;
                        }
                    }
                    else
                    {
                        // New file
                        await AddNewFileAsync(credential, remoteFile, password);
                        result.Added++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {Path}", remoteFile.Path);
                    result.Errors++;
                }
            }

            // Update sync timestamp
            credential.LastSyncAt = DateTime.UtcNow;
            credential.LastSyncError = null;
            await _context.SaveChangesAsync();

            result.Success = true;
            _logger.LogInformation("Sync completed for user {UserId}: Added={Added}, Updated={Updated}, Skipped={Skipped}",
                userId, result.Added, result.Updated, result.Skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing files for user {UserId}", userId);
            result.Error = ex.Message;

            // Update credential with error
            var credential = await GetCredentialAsync(userId);
            if (credential != null)
            {
                credential.LastSyncError = ex.Message;
                await _context.SaveChangesAsync();
            }
        }

        return result;
    }

    /// <summary>
    /// Get sync status for a user
    /// </summary>
    public async Task<SyncStatus> GetSyncStatusAsync(int userId)
    {
        var credential = await GetCredentialAsync(userId);
        if (credential == null)
        {
            return new SyncStatus { IsConfigured = false };
        }

        var fileCount = await _context.NextcloudFiles
            .CountAsync(f => f.CredentialId == credential.Id);

        var downloadedCount = await _context.NextcloudFiles
            .CountAsync(f => f.CredentialId == credential.Id && f.IsDownloaded);

        return new SyncStatus
        {
            IsConfigured = true,
            IsActive = credential.IsActive,
            LastSyncAt = credential.LastSyncAt,
            LastError = credential.LastSyncError,
            TotalFiles = fileCount,
            DownloadedFiles = downloadedCount,
            SyncIntervalMinutes = credential.SyncIntervalMinutes
        };
    }

    /// <summary>
    /// Get all synced files for a user
    /// </summary>
    public async Task<List<NextcloudFile>> GetSyncedFilesAsync(int userId)
    {
        var credential = await GetCredentialAsync(userId);
        if (credential == null)
        {
            return new List<NextcloudFile>();
        }

        return await _context.NextcloudFiles
            .Where(f => f.CredentialId == credential.Id)
            .OrderBy(f => f.RemotePath)
            .ToListAsync();
    }

    /// <summary>
    /// Download a specific file and process it
    /// </summary>
    public async Task<bool> DownloadAndProcessFileAsync(int fileId, int userId)
    {
        try
        {
            var file = await _context.NextcloudFiles
                .Include(f => f.Credential)
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found for user {UserId}", fileId, userId);
                return false;
            }

            var password = _encryptionHelper.Decrypt(file.Credential.EncryptedPassword);

            // Download file content
            var content = await _webDavClient.DownloadFileAsync(
                file.Credential.NextcloudUrl,
                file.Credential.Username,
                password,
                file.RemotePath
            );

            if (content == null)
            {
                _logger.LogWarning("Failed to download file {Path}", file.RemotePath);
                return false;
            }

            // Create a memory stream for processing
            using var stream = new MemoryStream(content);

            // Upload to our storage and process via FileService
            var document = await _fileService.UploadAndProcessDocumentAsync(
                userId,
                stream,
                file.FileName,
                file.FileType,
                "nextcloud_sync"
            );

            if (document != null)
            {
                file.LocalDocumentId = document.Id;
                file.IsDownloaded = true;
                file.IsProcessed = document.IsProcessed;
                file.LocalSyncedAt = DateTime.UtcNow;
                file.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Downloaded and processed file {Path} -> Document {DocumentId}",
                    file.RemotePath, document.Id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            return false;
        }
    }

    private async Task<NextcloudCredential?> GetCredentialAsync(int userId)
    {
        return await _context.NextcloudCredentials
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
    }

    private async Task AddNewFileAsync(NextcloudCredential credential, NextcloudFileInfo remoteFile, string password)
    {
        var newFile = new NextcloudFile
        {
            UserId = credential.UserId,
            CredentialId = credential.Id,
            RemotePath = remoteFile.Path,
            FileName = remoteFile.Name,
            FileType = remoteFile.Extension,
            FileSize = remoteFile.Size,
            ETag = remoteFile.ETag,
            RemoteModifiedAt = remoteFile.LastModified
        };

        _context.NextcloudFiles.Add(newFile);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Added new file tracking: {Path}", remoteFile.Path);
    }

    private async Task UpdateFileAsync(NextcloudCredential credential, NextcloudFile existingFile, NextcloudFileInfo remoteFile, string password)
    {
        existingFile.FileSize = remoteFile.Size;
        existingFile.ETag = remoteFile.ETag;
        existingFile.RemoteModifiedAt = remoteFile.LastModified;
        existingFile.IsDownloaded = false; // Mark for re-download
        existingFile.IsProcessed = false;
        existingFile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Updated file tracking: {Path}", remoteFile.Path);
    }
}

/// <summary>
/// Result of a sync operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Current sync status
/// </summary>
public class SyncStatus
{
    public bool IsConfigured { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastError { get; set; }
    public int TotalFiles { get; set; }
    public int DownloadedFiles { get; set; }
    public int SyncIntervalMinutes { get; set; }
}

/// <summary>
/// Interface for Nextcloud sync service
/// </summary>
public interface INextcloudSyncService
{
    Task<NextcloudCredential> SaveCredentialsAsync(int userId, string nextcloudUrl, string username, string password);
    Task<bool> TestConnectionAsync(int userId);
    Task<SyncResult> SyncUserFilesAsync(int userId);
    Task<SyncStatus> GetSyncStatusAsync(int userId);
    Task<List<NextcloudFile>> GetSyncedFilesAsync(int userId);
    Task<bool> DownloadAndProcessFileAsync(int fileId, int userId);
}
