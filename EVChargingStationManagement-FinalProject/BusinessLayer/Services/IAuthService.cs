using DataAccessLayer.Entities;
using System.Security.Claims;

namespace BusinessLayer.Services
{
    public interface IAuthService
    {
        // Authentication methods
        Task<Users?> LoginAsync(string username, string password);
        Task<Users> RegisterAsync(string username, string password, string fullName, string email, string phone, DateTime dateOfBirth, string gender, DataAccessLayer.Enums.UserRole role);
        Task<Users?> LoginWithGoogleAsync(string googleId, string email, string fullName, string? gender = null);
        Task<bool> VerifyPasswordAsync(string password, string hashedPassword);
        string HashPassword(string password);

        // JWT Token methods
        string GenerateToken(Guid userId, string username, string role);
        ClaimsPrincipal? ValidateToken(string token);

        // Refresh Token methods
        Task<string> GenerateRefreshTokenAsync(Guid userId);
        Task<bool> ValidateRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
        Task RevokeAllRefreshTokensAsync(Guid userId);
        Task CleanExpiredTokensAsync();

        // User Profile methods
        Task<Users?> GetUserByIdAsync(Guid userId);
        Task<Users?> UpdateUserProfileAsync(Guid userId, string fullName, string email, string phone, DateTime dateOfBirth, string gender);
        
        // User validation methods
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<Users?> GetUserByRefreshTokenAsync(string refreshToken);
    }
}

