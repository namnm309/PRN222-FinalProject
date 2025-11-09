using BusinessLayer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationLayer.Pages.Auth
{
    public class GoogleCallbackModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<GoogleCallbackModel> _logger;

        public GoogleCallbackModel(IAuthService authService, ILogger<GoogleCallbackModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Kiểm tra xem có lỗi từ Google OAuth không
            if (Request.Query.ContainsKey("error"))
            {
                var error = Request.Query["error"].ToString();
                _logger.LogWarning("Google OAuth error: {Error}", error);
                return RedirectToPage("/Auth/Login", new { error = "Đăng nhập Google thất bại. Vui lòng thử lại." });
            }

            // Lấy thông tin từ Google claims
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                _logger.LogWarning("Google authentication failed");
                return RedirectToPage("/Auth/Login", new { error = "Không thể xác thực với Google. Vui lòng thử lại." });
            }

            // Lấy thông tin từ Google
            var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            
            // Thử lấy Gender từ Google claims (nếu có)
            // Google có thể cung cấp trong claim "gender" hoặc "urn:google:gender"
            var gender = result.Principal.FindFirst("gender")?.Value 
                      ?? result.Principal.FindFirst("urn:google:gender")?.Value
                      ?? result.Principal.FindFirst(ClaimTypes.Gender)?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Missing Google claims: GoogleId={GoogleId}, Email={Email}", googleId, email);
                return RedirectToPage("/Auth/Login", new { error = "Không thể lấy thông tin từ Google. Vui lòng thử lại." });
            }

            // Log các claims có sẵn để debug (có thể xóa sau)
            _logger.LogInformation("Google claims received - Email: {Email}, Gender: {Gender}", email, gender ?? "Not provided");

            // Tìm hoặc tạo user
            // Lưu ý: DateOfBirth KHÔNG thể lấy từ Google vì đây là thông tin nhạy cảm
            // User sẽ cần cập nhật thông tin này sau khi đăng nhập
            var user = await _authService.LoginWithGoogleAsync(googleId, email, fullName ?? email, gender);

            if (user == null)
            {
                _logger.LogWarning("Failed to login/create user with Google: Email={Email}", email);
                return RedirectToPage("/Auth/Login", new { error = "Không thể đăng nhập. Vui lòng thử lại." });
            }

            // Tạo claims cho cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Đăng nhập với cookie scheme
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User logged in with Google: UserId={UserId}, Email={Email}", user.Id, email);

            // Redirect theo vai trò
            var role = user.Role.ToString();
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Admin/Index");
            }
            if (string.Equals(role, "CSStaff", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Staff/Index");
            }
            return RedirectToPage("/Index");
        }
    }
}

