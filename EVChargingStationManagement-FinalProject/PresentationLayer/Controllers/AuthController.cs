using BusinessLayer.Services;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresentationLayer.DTOs;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly EVDbContext _context;

        public AuthController(
            IAuthService authService,
            EVDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _authService.LoginAsync(request.Username, request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            var accessToken = _authService.GenerateToken(user.Id, user.Username, user.Role.ToString());
            var refreshToken = await _authService.GenerateRefreshTokenAsync(user.Id);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString()
                }
            };

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra username đã tồn tại chưa
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Username already exists" });
            }

            // Kiểm tra email đã tồn tại chưa
            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingEmail != null)
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var user = await _authService.RegisterAsync(
                request.Username,
                request.Password,
                request.FullName,
                request.Email,
                request.Phone,
                request.DateOfBirth,
                request.Gender,
                request.Role
            );

            var accessToken = _authService.GenerateToken(user.Id, user.Username, user.Role.ToString());
            var refreshToken = await _authService.GenerateRefreshTokenAsync(user.Id);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString()
                }
            };

            return CreatedAtAction(nameof(Login), response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _authService.ValidateRefreshTokenAsync(request.RefreshToken);
            if (!isValid)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            // Lấy refresh token từ database để lấy UserId
            var refreshTokenEntity = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshTokenEntity == null || refreshTokenEntity.User == null)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            var user = refreshTokenEntity.User;

            // Revoke old refresh token
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken);

            // Generate new tokens
            var newAccessToken = _authService.GenerateToken(user.Id, user.Username, user.Role.ToString());
            var newRefreshToken = await _authService.GenerateRefreshTokenAsync(user.Id);

            var response = new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString()
                }
            };

            return Ok(response);
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _authService.RevokeRefreshTokenAsync(request.RefreshToken);

            return Ok(new { message = "Refresh token revoked successfully" });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            await _authService.RevokeAllRefreshTokensAsync(userId);

            return Ok(new { message = "Logged out successfully" });
        }
    }
}

