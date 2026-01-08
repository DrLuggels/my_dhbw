using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Core.Interfaces;
using DHBWAutomation.Core.DTOs.Requests;
using DHBWAutomation.Core.DTOs.Responses;
using System.Security.Claims;

namespace DHBWAutomation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Nur authentifizierte Benutzer
public class MailController : ControllerBase
{
    private readonly IMailService _mailService;
    private readonly ILogger<MailController> _logger;

    public MailController(IMailService mailService, ILogger<MailController> logger)
    {
        _mailService = mailService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/mail/summary - Dashboard-Zusammenfassung
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<EmailSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var summary = await _mailService.GetSummaryAsync(userId, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// GET /api/mail/inbox - Alle E-Mails mit Filterung
    /// </summary>
    [HttpGet("inbox")]
    public async Task<ActionResult<List<EmailResponse>>> GetInbox(
        [FromQuery] string? folder = null,
        [FromQuery] bool? isRead = null,
        [FromQuery] bool? requiresAction = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var emails = await _mailService.GetEmailsAsync(
            userId, folder, isRead, requiresAction, skip, take, cancellationToken);
        
        return Ok(emails);
    }

    /// <summary>
    /// GET /api/mail/{id} - Einzelne E-Mail abrufen
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmailResponse>> GetEmailById(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = await _mailService.GetEmailByIdAsync(id, userId, cancellationToken);

        if (email == null)
            return NotFound(new { message = "E-Mail nicht gefunden" });

        return Ok(email);
    }

    /// <summary>
    /// POST /api/mail/sync - Manueller E-Mail-Sync
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<object>> SyncEmails(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        
        try
        {
            var newEmailsCount = await _mailService.SyncEmailsAsync(userId, cancellationToken);
            return Ok(new 
            { 
                success = true, 
                message = $"{newEmailsCount} neue E-Mails synchronisiert",
                count = newEmailsCount 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim manuellen E-Mail-Sync für User {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Synchronisation fehlgeschlagen" });
        }
    }

    /// <summary>
    /// POST /api/mail/{id}/action - Aktion auf E-Mail ausführen (accept, decline, snooze, etc.)
    /// </summary>
    [HttpPost("{id:int}/action")]
    public async Task<ActionResult<EmailResponse>> ExecuteAction(
        int id,
        [FromBody] EmailActionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        request.EmailId = id; // Setze ID aus Route

        try
        {
            var result = await _mailService.ExecuteActionAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Ausführen der Aktion für E-Mail {EmailId}", id);
            return StatusCode(500, new { message = "Aktion konnte nicht ausgeführt werden" });
        }
    }

    /// <summary>
    /// PUT /api/mail/{id}/read - E-Mail als gelesen/ungelesen markieren
    /// </summary>
    [HttpPut("{id:int}/read")]
    public async Task<ActionResult> MarkAsRead(
        int id,
        [FromBody] MarkAsReadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mailService.MarkAsReadAsync(id, request.IsRead, cancellationToken);
            return Ok(new { success = true, message = "Status aktualisiert" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Markieren von E-Mail {EmailId}", id);
            return StatusCode(500, new { success = false, message = "Status konnte nicht aktualisiert werden" });
        }
    }

    /// <summary>
    /// DELETE /api/mail/{id} - E-Mail löschen
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteEmail(
        int id,
        [FromQuery] bool deleteFromServer = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mailService.DeleteEmailAsync(id, deleteFromServer, cancellationToken);
            return Ok(new { success = true, message = "E-Mail gelöscht" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen von E-Mail {EmailId}", id);
            return StatusCode(500, new { success = false, message = "E-Mail konnte nicht gelöscht werden" });
        }
    }

    /// <summary>
    /// POST /api/mail/{id}/process - Erzwinge KI-Verarbeitung einer E-Mail
    /// </summary>
    [HttpPost("{id:int}/process")]
    public async Task<ActionResult> ProcessEmail(int id, CancellationToken cancellationToken)
    {
        try
        {
            var email = await _mailService.ProcessEmailAsync(id, cancellationToken);
            return Ok(new { success = true, message = "E-Mail verarbeitet", emailId = email.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Verarbeiten von E-Mail {EmailId}", id);
            return StatusCode(500, new { success = false, message = "Verarbeitung fehlgeschlagen" });
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
}

// DTO für MarkAsRead Request
public class MarkAsReadRequest
{
    public bool IsRead { get; set; }
}
