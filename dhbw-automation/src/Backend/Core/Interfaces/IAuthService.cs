using DHBWAutomation.Backend.Core.DTOs.Requests;
using DHBWAutomation.Backend.Core.DTOs.Responses;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    string GenerateJwtToken(User user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
