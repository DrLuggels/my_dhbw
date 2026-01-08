using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DHBWAutomation.Backend.Core.DTOs.Requests;
using DHBWAutomation.Backend.Core.DTOs.Responses;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;

namespace DHBWAutomation.Backend.Core.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await GetUserByEmailAsync(request.Email);
            
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning($"Failed login attempt for email: {request.Email}");
                return null;
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                User = MapToUserResponse(user),
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning($"Registration failed - user already exists: {request.Email}");
                return null;
            }

            var user = new User
            {
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                MatriculationNumber = request.MatriculationNumber,
                Course = request.Course,
                IsActive = true,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                User = MapToUserResponse(user),
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            throw;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return false;

            if (!VerifyPassword(request.OldPassword, user.PasswordHash))
                return false;

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            throw;
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
            ?? _configuration["JWT_SECRET"] 
            ?? "your_super_secret_jwt_key_at_least_32_characters_long";
        
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "DHBWAutomation";
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "DHBWAutomationUsers";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password)
    {
        // Simple hash for demo - in production use BCrypt.Net-Next
        return Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(password + "DHBW_SALT")
            )
        );
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var hashedInput = HashPassword(password);
        return hashedInput == passwordHash;
    }

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private UserResponse MapToUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MatriculationNumber = user.MatriculationNumber,
            Course = user.Course,
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt
        };
    }
}
