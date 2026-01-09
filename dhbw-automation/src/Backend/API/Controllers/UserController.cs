using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.DTOs.Requests;
using DHBWAutomation.Backend.Core.DTOs.Responses;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using DHBWAutomation.Backend.Shared.Helpers;
using System.Security.Claims;

namespace DHBWAutomation.Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserController> _logger;
    private readonly EncryptionHelper _encryptionHelper;

    public UserController(AppDbContext context, ILogger<UserController> logger, EncryptionHelper encryptionHelper)
    {
        _context = context;
        _logger = logger;
        _encryptionHelper = encryptionHelper;
    }

    /// <summary>
    /// Get API Keys Status (zeigt nur ob Keys gesetzt sind, nicht die Keys selbst)
    /// </summary>
    [HttpGet("api-keys")]
    public async Task<ActionResult<ApiResponse<ApiKeysResponse>>> GetApiKeys()
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<ApiKeysResponse>
                {
                    Success = false,
                    Message = "Nicht authentifiziert"
                });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new ApiResponse<ApiKeysResponse>
                {
                    Success = false,
                    Message = "Benutzer nicht gefunden"
                });
            }

            var response = new ApiKeysResponse
            {
                HasOpenAiKey = !string.IsNullOrEmpty(user.OpenAiApiKey),
                HasAnthropicKey = !string.IsNullOrEmpty(user.AnthropicApiKey),
                HasGeminiKey = !string.IsNullOrEmpty(user.GeminiApiKey)
            };

            // Optional: Zeige Preview (erste 7 und letzte 4 Zeichen)
            if (!string.IsNullOrEmpty(user.OpenAiApiKey))
            {
                var decrypted = _encryptionHelper.Decrypt(user.OpenAiApiKey);
                response.OpenAiKeyPreview = GetKeyPreview(decrypted);
            }
            if (!string.IsNullOrEmpty(user.AnthropicApiKey))
            {
                var decrypted = _encryptionHelper.Decrypt(user.AnthropicApiKey);
                response.AnthropicKeyPreview = GetKeyPreview(decrypted);
            }
            if (!string.IsNullOrEmpty(user.GeminiApiKey))
            {
                var decrypted = _encryptionHelper.Decrypt(user.GeminiApiKey);
                response.GeminiKeyPreview = GetKeyPreview(decrypted);
            }

            return Ok(new ApiResponse<ApiKeysResponse>
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der API-Keys");
            return StatusCode(500, new ApiResponse<ApiKeysResponse>
            {
                Success = false,
                Message = "Fehler beim Abrufen der API-Keys",
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update API Keys
    /// </summary>
    [HttpPut("api-keys")]
    public async Task<ActionResult<ApiResponse<ApiKeysResponse>>> UpdateApiKeys([FromBody] UpdateApiKeysRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<ApiKeysResponse>
                {
                    Success = false,
                    Message = "Nicht authentifiziert"
                });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new ApiResponse<ApiKeysResponse>
                {
                    Success = false,
                    Message = "Benutzer nicht gefunden"
                });
            }

            // Verschlüssele und speichere API Keys (nur wenn nicht null oder leer)
            if (!string.IsNullOrWhiteSpace(request.OpenAiApiKey))
            {
                user.OpenAiApiKey = _encryptionHelper.Encrypt(request.OpenAiApiKey.Trim());
            }
            else if (request.OpenAiApiKey == "") // Explizit leer = löschen
            {
                user.OpenAiApiKey = null;
            }

            if (!string.IsNullOrWhiteSpace(request.AnthropicApiKey))
            {
                user.AnthropicApiKey = _encryptionHelper.Encrypt(request.AnthropicApiKey.Trim());
            }
            else if (request.AnthropicApiKey == "")
            {
                user.AnthropicApiKey = null;
            }

            if (!string.IsNullOrWhiteSpace(request.GeminiApiKey))
            {
                user.GeminiApiKey = _encryptionHelper.Encrypt(request.GeminiApiKey.Trim());
            }
            else if (request.GeminiApiKey == "")
            {
                user.GeminiApiKey = null;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated API keys", userId);

            // Return updated status
            var response = new ApiKeysResponse
            {
                HasOpenAiKey = !string.IsNullOrEmpty(user.OpenAiApiKey),
                HasAnthropicKey = !string.IsNullOrEmpty(user.AnthropicApiKey),
                HasGeminiKey = !string.IsNullOrEmpty(user.GeminiApiKey)
            };

            if (!string.IsNullOrEmpty(user.OpenAiApiKey))
            {
                response.OpenAiKeyPreview = GetKeyPreview(_encryptionHelper.Decrypt(user.OpenAiApiKey));
            }
            if (!string.IsNullOrEmpty(user.AnthropicApiKey))
            {
                response.AnthropicKeyPreview = GetKeyPreview(_encryptionHelper.Decrypt(user.AnthropicApiKey));
            }
            if (!string.IsNullOrEmpty(user.GeminiApiKey))
            {
                response.GeminiKeyPreview = GetKeyPreview(_encryptionHelper.Decrypt(user.GeminiApiKey));
            }

            return Ok(new ApiResponse<ApiKeysResponse>
            {
                Success = true,
                Data = response,
                Message = "API Keys erfolgreich aktualisiert"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren der API-Keys");
            return StatusCode(500, new ApiResponse<ApiKeysResponse>
            {
                Success = false,
                Message = "Fehler beim Aktualisieren der API-Keys",
                Errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete all API Keys
    /// </summary>
    [HttpDelete("api-keys")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteApiKeys()
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Nicht authentifiziert"
                });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Benutzer nicht gefunden"
                });
            }

            user.OpenAiApiKey = null;
            user.AnthropicApiKey = null;
            user.GeminiApiKey = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted all API keys", userId);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Alle API Keys gelöscht"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen der API-Keys");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Fehler beim Löschen der API-Keys",
                Errors = new[] { ex.Message }
            });
        }
    }

    private int? GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }

    private string GetKeyPreview(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 12)
            return "***";
        
        return $"{key.Substring(0, 7)}...{key.Substring(key.Length - 4)}";
    }
}
