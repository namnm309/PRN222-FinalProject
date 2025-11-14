using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PresentationLayer.Helpers;
using PresentationLayer.Hubs;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IHubContext<UserHub> _hubContext;

        public UserController(IUserService userService, IHubContext<UserHub> hubContext)
        {
            _userService = userService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null)
        {
            var (users, totalCount) = await _userService.GetAllUsersByRoleStringAsync(
                page,
                pageSize,
                role,
                isActive,
                search);

            return Ok(new
            {
                data = users,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = JwtHelper.GetUserId(User);
            if (!adminId.HasValue)
            {
                return Unauthorized(new { message = "Invalid admin credentials" });
            }

            try
            {
                var user = await _userService.UpdateUserRoleAsync(id, request.Role, adminId.Value);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userService.UpdateUserStatusAsync(id, request.IsActive);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Gửi SignalR notification cho admin group và user bị ảnh hưởng
            // Wrap trong try-catch để không ảnh hưởng đến response nếu SignalR lỗi
            try
            {
                await _hubContext.Clients.Group("admin-group").SendAsync("UserStatusUpdated", new
                {
                    userId = user.Id,
                    username = user.Username,
                    fullName = user.FullName,
                    email = user.Email,
                    isActive = user.IsActive,
                    updatedAt = user.UpdatedAt
                });

                // Gửi notification cho chính user đó nếu họ đang online
                await _hubContext.Clients.Group($"user-{user.Id}").SendAsync("AccountStatusChanged", new
                {
                    isActive = user.IsActive,
                    message = user.IsActive 
                        ? "Tài khoản của bạn đã được kích hoạt" 
                        : "Tài khoản của bạn đã bị khóa bởi quản trị viên"
                });
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng vẫn trả về response thành công
                // Vì việc cập nhật user đã thành công, chỉ là SignalR notification bị lỗi
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<UserController>>();
                logger.LogError(ex, "Error sending SignalR notification for user status update");
            }

            return Ok(user);
        }

        [HttpGet("role-distribution")]
        public async Task<IActionResult> GetUserRoleDistribution()
        {
            var distribution = await _userService.GetUserRoleDistributionAsync();
            return Ok(distribution);
        }
    }
}

