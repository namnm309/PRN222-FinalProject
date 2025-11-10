using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.DTOs;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
            if (await _authService.UsernameExistsAsync(request.Username))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            // Kiểm tra email đã tồn tại chưa
            if (await _authService.EmailExistsAsync(request.Email))
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

            // Lấy user từ refresh token
            var user = await _authService.GetUserByRefreshTokenAsync(request.RefreshToken);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

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

