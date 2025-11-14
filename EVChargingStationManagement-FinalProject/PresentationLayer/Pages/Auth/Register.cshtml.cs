using BusinessLayer.Services;
using BusinessLayer.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationLayer.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<RegisterModel> _logger;

        [BindProperty]
        public RegisterRequest RegisterRequest { get; set; } = new();

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public RegisterModel(
            IAuthService authService,
            ILogger<RegisterModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public void OnGet(string? error = null, string? success = null)
        {
            ErrorMessage = error;
            SuccessMessage = success;
            
            // Set giá trị mặc định cho ngày sinh là năm 2000
            if (RegisterRequest.DateOfBirth == default(DateTime))
            {
                RegisterRequest.DateOfBirth = new DateTime(2000, 1, 1);
            }
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            // Kiểm tra xác nhận mật khẩu
            if (string.IsNullOrEmpty(ConfirmPassword) || RegisterRequest.Password != ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không khớp";
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Kiểm tra username đã tồn tại chưa
            var usernameExists = await _authService.CheckUsernameExistsAsync(RegisterRequest.Username);
            if (usernameExists)
            {
                ErrorMessage = "Tên đăng nhập đã được sử dụng. Vui lòng chọn tên khác.";
                return Page();
            }

            // Kiểm tra email đã tồn tại chưa
            var emailExists = await _authService.CheckEmailExistsAsync(RegisterRequest.Email);
            if (emailExists)
            {
                ErrorMessage = "Email đã được sử dụng. Vui lòng sử dụng email khác.";
                return Page();
            }

            try
            {
                // Đăng ký user mới với role mặc định là EVDriver
                var user = await _authService.RegisterAsync(
                    RegisterRequest.Username,
                    RegisterRequest.Password,
                    RegisterRequest.FullName,
                    RegisterRequest.Email,
                    RegisterRequest.Phone,
                    RegisterRequest.DateOfBirth,
                    RegisterRequest.Gender,
                    RegisterRequest.Role // Sử dụng Role từ RegisterRequest (mặc định là EVDriver)
                );

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

                // Redirect về trang chủ sau khi đăng ký thành công
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ErrorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.";
                return Page();
            }
        }

        public IActionResult OnPostGoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Auth/GoogleCallback")
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
    }
}

