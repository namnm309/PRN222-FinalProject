using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.DTOs;
using PresentationLayer.Helpers;
using System.Security.Claims;

namespace PresentationLayer.Pages.Auth
{
    [Authorize]
    public class EditProfileModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<EditProfileModel> _logger;

        [BindProperty]
        public EditProfileRequest EditRequest { get; set; } = new();

        public UserProfileDTO? CurrentUser { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public EditProfileModel(IAuthService authService, ILogger<EditProfileModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string? success = null)
        {
            // Sử dụng PageModel.User (ClaimsPrincipal) để lấy userId
            var userId = JwtHelper.GetUserId(User);
            
            if (userId == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            var userEntity = await _authService.GetUserByIdAsync(userId.Value);

            if (userEntity == null)
            {
                ErrorMessage = "Không tìm thấy thông tin người dùng";
                return Page();
            }

            // Chuyển đổi entity sang DTO
            CurrentUser = UserProfileMapper.ToDTO(userEntity);

            // Populate form với dữ liệu hiện tại
            EditRequest.FullName = CurrentUser.FullName;
            EditRequest.Email = CurrentUser.Email;
            EditRequest.Phone = CurrentUser.Phone;
            EditRequest.DateOfBirth = CurrentUser.DateOfBirth == DateTime.MinValue ? DateTime.Now.AddYears(-18) : CurrentUser.DateOfBirth;
            EditRequest.Gender = CurrentUser.Gender;

            SuccessMessage = success;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Sử dụng PageModel.User (ClaimsPrincipal) để lấy userId
            var userIdClaim = JwtHelper.GetUserId(User);
            
            if (userIdClaim == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            if (!ModelState.IsValid)
            {
                var userEntity = await _authService.GetUserByIdAsync(userIdClaim.Value);
                if (userEntity != null)
                {
                    CurrentUser = UserProfileMapper.ToDTO(userEntity);
                }
                return Page();
            }

            try
            {
                var updatedUserEntity = await _authService.UpdateUserProfileAsync(
                    userIdClaim.Value,
                    EditRequest.FullName,
                    EditRequest.Email,
                    EditRequest.Phone,
                    EditRequest.DateOfBirth,
                    EditRequest.Gender
                );

                if (updatedUserEntity == null)
                {
                    ErrorMessage = "Không thể cập nhật thông tin. Vui lòng thử lại.";
                    var userEntity = await _authService.GetUserByIdAsync(userIdClaim.Value);
                    if (userEntity != null)
                    {
                        CurrentUser = UserProfileMapper.ToDTO(userEntity);
                    }
                    return Page();
                }

                // Chuyển đổi entity sang DTO
                CurrentUser = UserProfileMapper.ToDTO(updatedUserEntity);
                SuccessMessage = "Cập nhật thông tin thành công!";

                _logger.LogInformation("User profile updated: UserId={UserId}", userIdClaim.Value);

                return RedirectToPage("/Auth/EditProfile", new { success = "Cập nhật thông tin thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                var userEntity = await _authService.GetUserByIdAsync(userIdClaim.Value);
                if (userEntity != null)
                {
                    CurrentUser = UserProfileMapper.ToDTO(userEntity);
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile: UserId={UserId}, Error={Error}", userIdClaim.Value, ex.Message);
                
                // Hiển thị thông báo lỗi chi tiết hơn trong development
                #if DEBUG
                ErrorMessage = $"Lỗi: {ex.Message}";
                if (ex.InnerException != null)
                {
                    ErrorMessage += $" - Chi tiết: {ex.InnerException.Message}";
                }
                #else
                ErrorMessage = "Đã xảy ra lỗi khi cập nhật thông tin. Vui lòng thử lại.";
                #endif
                
                var userEntity = await _authService.GetUserByIdAsync(userIdClaim.Value);
                if (userEntity != null)
                {
                    CurrentUser = UserProfileMapper.ToDTO(userEntity);
                }
                return Page();
            }
        }
    }
}

