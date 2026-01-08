using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DHBWAutomation.Core.DTOs.Requests;
using DHBWAutomation.Core.DTOs.Responses;
using DHBWAutomation.Infrastructure.Database;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DHBWAutomation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserController> _logger;
    private readonly IConfiguration _configuration;

    public UserController(
        AppDbContext context,
        ILogger<UserController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// GET /api/user/profile - Benutzerprofil abrufen
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "Benutzer nicht gefunden" });

        var profile = new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MatriculationNumber = user.MatriculationNumber,
            Course = user.Course,
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            EmailSyncEnabled = user.EmailSyncEnabled,
            EmailSyncAddress = user.EmailSyncAddress,
            EmailImapHost = user.EmailImapHost ?? "outlook.office365.com",
            EmailImapPort = user.EmailImapPort,
            EmailSmtpHost = user.EmailSmtpHost ?? "smtp.office365.com",
            EmailSmtpPort = user.EmailSmtpPort,
            EmailSyncIntervalMinutes = user.EmailSyncIntervalMinutes,
            LastEmailSync = user.LastEmailSync
        };

        return Ok(profile);
    }

    /// <summary>
    /// PUT /api/user/profile - Profil aktualisieren
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "Benutzer nicht gefunden" });

        // Update basic fields
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;

        if (request.MatriculationNumber != null)
            user.MatriculationNumber = request.MatriculationNumber;

        if (request.Course != null)
            user.Course = request.Course;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetProfile();
    }

    /// <summary>
    /// PUT /api/user/email-settings - E-Mail-Einstellungen aktualisieren
    /// </summary>
    [HttpPut("email-settings")]
    public async Task<ActionResult<UserProfileResponse>> UpdateEmailSettings(
        [FromBody] UpdateEmailSettingsRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "Benutzer nicht gefunden" });

        try
        {
            user.EmailSyncEnabled = request.EmailSyncEnabled;
            user.EmailSyncAddress = request.EmailSyncAddress;

            // Verschlüssele Passwort wenn vorhanden
            if (!string.IsNullOrEmpty(request.EmailSyncPassword))
            {
                user.EmailSyncPassword = EncryptPassword(request.EmailSyncPassword);
            }

            // Update IMAP/SMTP Settings mit Defaults
            user.EmailImapHost = request.EmailImapHost ?? "outlook.office365.com";
            user.EmailImapPort = request.EmailImapPort ?? 993;
            user.EmailSmtpHost = request.EmailSmtpHost ?? "smtp.office365.com";
            user.EmailSmtpPort = request.EmailSmtpPort ?? 587;
            user.EmailSyncIntervalMinutes = request.EmailSyncIntervalMinutes ?? 1;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("E-Mail-Einstellungen aktualisiert für User {UserId}", userId);

            return await GetProfile();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren der E-Mail-Einstellungen");
            return StatusCode(500, new { message = "Fehler beim Speichern der Einstellungen" });
        }
    }

    /// <summary>
    /// POST /api/user/test-email-connection - E-Mail-Verbindung testen
    /// </summary>
    [HttpPost("test-email-connection")]
    public async Task<ActionResult> TestEmailConnection()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null || !user.EmailSyncEnabled || string.IsNullOrEmpty(user.EmailSyncAddress))
            return BadRequest(new { message = "E-Mail-Sync nicht konfiguriert" });

        try
        {
            var password = DecryptPassword(user.EmailSyncPassword ?? "");
            var username = GetEmailUsername(user.EmailSyncAddress);

            using var client = new MailKit.Net.Imap.ImapClient();
            await client.ConnectAsync(
                user.EmailImapHost ?? "outlook.office365.com",
                user.EmailImapPort,
                MailKit.Security.SecureSocketOptions.SslOnConnect);

            await client.AuthenticateAsync(username, password);
            await client.DisconnectAsync(true);

            return Ok(new { success = true, message = "Verbindung erfolgreich!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-Mail-Verbindungstest fehlgeschlagen für User {UserId}", userId);
            return BadRequest(new { 
                success = false, 
                message = "Verbindung fehlgeschlagen: " + ex.Message 
            });
        }
    }

    // === Helper Methods ===

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID nicht gefunden");
        }

        return userId;
    }

    private string EncryptPassword(string plainPassword)
    {
        // Einfache XOR-Verschlüsselung mit Key aus Configuration
        // ACHTUNG: Für Production sollte AES oder eine stärkere Methode verwendet werden
        var key = _configuration["Encryption:Key"] ?? "DefaultEncryptionKey123!";
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var passwordBytes = Encoding.UTF8.GetBytes(plainPassword);

        var encrypted = new byte[passwordBytes.Length];
        for (int i = 0; i < passwordBytes.Length; i++)
        {
            encrypted[i] = (byte)(passwordBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return Convert.ToBase64String(encrypted);
    }

    private string DecryptPassword(string encryptedPassword)
    {
        try
        {
            var key = _configuration["Encryption:Key"] ?? "DefaultEncryptionKey123!";
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var encryptedBytes = Convert.FromBase64String(encryptedPassword);

            var decrypted = new byte[encryptedBytes.Length];
            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decrypted[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return "";
        }
    }

    private string GetEmailUsername(string email)
    {
        // Cvitanovic.Luka-25@stud.dhbw-ravensburg.de → Cvitanovic.Luka-25
        if (email.Contains("@"))
        {
            return email.Split('@')[0];
        }
        return email;
    }
}

// DTO für Profil-Update
public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MatriculationNumber { get; set; }
    public string? Course { get; set; }
}
