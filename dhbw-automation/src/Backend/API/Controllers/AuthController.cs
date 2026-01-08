using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.DTOs.Requests;
using DHBWAutomation.Backend.Core.DTOs.Responses;

namespace DHBWAutomation.Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            
            if (result == null)
            {
                return Unauthorized(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Ungültige Anmeldedaten",
                    Errors = new[] { "Email oder Passwort ist falsch" }
                });
            }

            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Data = result,
                Message = "Login erfolgreich"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Login");
            return StatusCode(500, new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Ein Fehler ist aufgetreten",
                Errors = new[] { ex.Message }
            });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            
            if (result == null)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Registrierung fehlgeschlagen",
                    Errors = new[] { "Benutzer existiert bereits oder ungültige Daten" }
                });
            }

            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Data = result,
                Message = "Registrierung erfolgreich"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei Registrierung");
            return StatusCode(500, new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Ein Fehler ist aufgetreten",
                Errors = new[] { ex.Message }
            });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetCurrentUser()
    {
        // TODO: Implement JWT authentication and get user from token
        return Ok(new ApiResponse<UserResponse>
        {
            Success = true,
            Message = "JWT Authentication noch nicht implementiert"
        });
    }
}
