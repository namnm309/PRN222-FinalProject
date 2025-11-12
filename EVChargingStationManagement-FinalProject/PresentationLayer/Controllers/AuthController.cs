using BusinessLayer.Services;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.DTOs;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly EVDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            EVDbContext context,
            IConfiguration configuration)
        {
            _authService = authService;
            _context = context;
            _configuration = configuration;
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

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var clientId = _configuration.GetSection("GoogleOAuth")["ClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Google OAuth is not configured properly." });
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Google token is invalid or expired." });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Không thể xác thực token Google. Vui lòng thử lại." });
            }

            if (string.IsNullOrEmpty(payload.Email) || string.IsNullOrEmpty(payload.Subject))
            {
                return BadRequest(new { message = "Thiếu thông tin cần thiết từ Google." });
            }

            // Google payload không cung cấp gender theo mặc định. Nếu claim tồn tại thì lấy.
            var user = await _authService.LoginWithGoogleAsync(payload.Subject, payload.Email, payload.Name ?? payload.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Tài khoản đã bị vô hiệu hóa hoặc không thể đăng nhập bằng Google." });
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

