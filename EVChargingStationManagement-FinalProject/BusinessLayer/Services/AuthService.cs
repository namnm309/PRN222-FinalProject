using BCrypt.Net;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly EVDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(EVDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        #region Authentication Methods

        public async Task<Users?> LoginAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return null;

            if (!user.IsActive)
                return null;

            if (!VerifyPasswordAsync(password, user.Password).Result)
                return null;

            return user;
        }

        public async Task<Users> RegisterAsync(string username, string password, string fullName, string email, string phone, DateTime dateOfBirth, string gender, UserRole role)
        {
            var hashedPassword = HashPassword(password);

            // Đảm bảo DateOfBirth có Kind = UTC cho PostgreSQL
            var dateOfBirthUtc = dateOfBirth.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Utc) 
                : dateOfBirth.ToUniversalTime();

            var user = new Users
            {
                Id = Guid.NewGuid(),
                Username = username,
                Password = hashedPassword,
                FullName = fullName,
                Email = email,
                Phone = phone,
                DateOfBirth = dateOfBirthUtc,
                Gender = gender,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<Users?> LoginWithGoogleAsync(string googleId, string email, string fullName, string? gender = null)
        {
            // Tìm user theo GoogleId hoặc Email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.GoogleId == googleId || u.Email == email);

            if (user != null)
            {
                // Nếu user đã tồn tại nhưng chưa có GoogleId, cập nhật GoogleId
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = googleId;
                    user.UpdatedAt = DateTime.UtcNow;
                }

                // Cập nhật Gender nếu có và user chưa có thông tin này
                if (!string.IsNullOrEmpty(gender) && (string.IsNullOrEmpty(user.Gender) || user.Gender == "Unknown"))
                {
                    user.Gender = gender;
                    user.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Kiểm tra user có active không
                if (!user.IsActive)
                    return null;

                return user;
            }

            // Tạo user mới nếu chưa tồn tại
            // Tạo username từ email (lấy phần trước @)
            var usernameFromEmail = email.Split('@')[0];
            var baseUsername = usernameFromEmail;
            var username = baseUsername;
            var counter = 1;

            // Đảm bảo username là unique
            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            // Tạo password ngẫu nhiên cho user Google (sẽ không dùng để login)
            var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // Đảm bảo DateTime.MinValue có Kind = UTC cho PostgreSQL
            var minDateUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

            user = new Users
            {
                Id = Guid.NewGuid(),
                Username = username,
                Password = HashPassword(randomPassword), // Password ngẫu nhiên, không dùng để login
                FullName = fullName,
                Email = email,
                Phone = string.Empty, // Google không cung cấp số điện thoại trong profile cơ bản
                DateOfBirth = minDateUtc, // Google KHÔNG cung cấp ngày sinh (thông tin nhạy cảm)
                Gender = gender ?? "Unknown", // Google có thể cung cấp nếu user đã cấu hình
                GoogleId = googleId,
                Role = UserRole.EVDriver, // Mặc định là EVDriver
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public Task<bool> VerifyPasswordAsync(string password, string hashedPassword)
        {
            return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, hashedPassword));
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt());
        }

        #endregion

        #region JWT Token Methods

        public string GenerateToken(Guid userId, string username, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey!);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Refresh Token Methods

        public async Task<string> GenerateRefreshTokenAsync(Guid userId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return token;
        }

        public async Task<bool> ValidateRefreshTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null)
                return false;

            if (refreshToken.IsRevoked)
                return false;

            if (refreshToken.ExpiresAt < DateTime.UtcNow)
                return false;

            return true;
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllRefreshTokensAsync(Guid userId)
        {
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CleanExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow || rt.IsRevoked)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region User Profile Methods

        public async Task<Users?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<Users?> UpdateUserProfileAsync(Guid userId, string fullName, string email, string phone, DateTime dateOfBirth, string gender)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            // Kiểm tra email có bị trùng với user khác không
            var existingUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && u.Id != userId);

            if (existingUser != null)
                throw new InvalidOperationException("Email đã được sử dụng bởi người dùng khác");

            // Attach entity và đánh dấu là modified
            var trackedUser = await _context.Users.FindAsync(userId);
            if (trackedUser == null)
                return null;

            // Cập nhật thông tin
            trackedUser.FullName = fullName;
            trackedUser.Email = email;
            trackedUser.Phone = phone;
            // Đảm bảo DateOfBirth có Kind = UTC cho PostgreSQL
            trackedUser.DateOfBirth = dateOfBirth.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Utc) 
                : dateOfBirth.ToUniversalTime();
            trackedUser.Gender = gender;
            trackedUser.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                throw new InvalidOperationException($"Lỗi khi lưu thông tin: {ex.InnerException?.Message ?? ex.Message}", ex);
            }

            return trackedUser;
        }

        public async Task<bool> CheckUsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<RefreshToken?> GetRefreshTokenWithUserAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        #endregion
    }
}

