using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Helpers;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu authentication
    public class ExampleProtectedController : ControllerBase
    {
        [HttpGet("public")]
        [AllowAnonymous] // Không cần authentication
        public IActionResult PublicEndpoint()
        {
            return Ok(new { message = "This is a public endpoint, no authentication required" });
        }

        [HttpGet("protected")]
        public IActionResult ProtectedEndpoint()
        {
            var userId = JwtHelper.GetUserId(User);
            var username = JwtHelper.GetUsername(User);
            var role = JwtHelper.GetUserRole(User);

            return Ok(new 
            { 
                message = "This is a protected endpoint",
                userId = userId,
                username = username,
                role = role
            });
        }

        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")] // Chỉ Admin mới truy cập được
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok(new { message = "This endpoint is only accessible by Admin users" });
        }

        [HttpGet("staff-only")]
        [Authorize(Roles = "CSStaff,Admin")] // CSStaff hoặc Admin
        public IActionResult StaffOnlyEndpoint()
        {
            return Ok(new { message = "This endpoint is accessible by CSStaff and Admin users" });
        }

        [HttpGet("driver-only")]
        [Authorize(Roles = "EVDriver")]
        public IActionResult DriverOnlyEndpoint()
        {
            return Ok(new { message = "This endpoint is only accessible by EVDriver users" });
        }
    }
}





