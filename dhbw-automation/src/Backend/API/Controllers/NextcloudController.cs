using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Services;
using System.Security.Claims;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for Nextcloud integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NextcloudController : ControllerBase
{
    private readonly INextcloudSyncService _syncService;
    private readonly ILogger<NextcloudController> _logger;

    public NextcloudController(
        INextcloudSyncService syncService,
        ILogger<NextcloudController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Save Nextcloud credentials
    /// </summary>
    [HttpPost("credentials")]
    public async Task<IActionResult> SaveCredentials([FromBody] NextcloudCredentialsDto dto)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var credential = await _syncService.SaveCredentialsAsync(
                userId,
                dto.NextcloudUrl,
                dto.Username,
                dto.Password
            );

            return Ok(new
            {
                success = true,
                message = "Credentials saved successfully",
                credentialId = credential.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Nextcloud credentials");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Test Nextcloud connection
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var success = await _syncService.TestConnectionAsync(userId);

            return Ok(new
            {
                success,
                message = success ? "Connection successful" : "Connection failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Nextcloud connection");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Trigger manual sync
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _syncService.SyncUserFilesAsync(userId);

            return Ok(new
            {
                success = result.Success,
                added = result.Added,
                updated = result.Updated,
                skipped = result.Skipped,
                errors = result.Errors,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Nextcloud files");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get sync status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var status = await _syncService.GetSyncStatusAsync(userId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get synced files
    /// </summary>
    [HttpGet("files")]
    public async Task<IActionResult> GetFiles()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var files = await _syncService.GetSyncedFilesAsync(userId);

            return Ok(files.Select(f => new
            {
                id = f.Id,
                fileName = f.FileName,
                remotePath = f.RemotePath,
                fileType = f.FileType,
                fileSize = f.FileSize,
                remoteModifiedAt = f.RemoteModifiedAt,
                isDownloaded = f.IsDownloaded,
                isProcessed = f.IsProcessed,
                localDocumentId = f.LocalDocumentId
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting synced files");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Download and process a specific file
    /// </summary>
    [HttpPost("files/{fileId}/download")]
    public async Task<IActionResult> DownloadFile(int fileId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var success = await _syncService.DownloadAndProcessFileAsync(fileId, userId);

            return Ok(new
            {
                success,
                message = success ? "File downloaded and processed" : "Failed to download file"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

/// <summary>
/// DTO for Nextcloud credentials
/// </summary>
public class NextcloudCredentialsDto
{
    public string NextcloudUrl { get; set; } = "https://nextcloud.dhbw-ravensburg.de";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
