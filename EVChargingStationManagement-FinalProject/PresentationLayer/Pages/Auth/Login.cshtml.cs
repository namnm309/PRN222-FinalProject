using BusinessLayer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.DTOs;
using System.Security.Claims;

namespace PresentationLayer.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        public LoginRequest LoginRequest { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public LoginModel(IAuthService authService, ILogger<LoginModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public void OnGet(string? error = null, string? success = null)
        {
            ErrorMessage = error;
            SuccessMessage = success;
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _authService.LoginAsync(LoginRequest.Username, LoginRequest.Password);

            if (user == null)
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng";
                return Page();
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

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

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

        public async Task<IActionResult> OnPostGoogleLoginAsync()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Auth/GoogleCallback")
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
    }
}

