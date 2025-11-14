using BusinessLayer.Services;
using BusinessLayer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,CSStaff")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] Guid? stationId = null)
        {
            var overview = await _dashboardService.GetOverviewAsync(stationId);
            return Ok(overview);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessionTimeline([FromQuery] Guid? stationId = null)
        {
            var timeline = await _dashboardService.GetSessionTimelineAsync(stationId);
            return Ok(timeline);
        }

        [HttpGet("sessions/all")]
        public async Task<IActionResult> GetAllSessions(
            [FromQuery] Guid? stationId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // Parse status string to enum using reflection
            object? statusEnum = null;
            if (!string.IsNullOrEmpty(status))
            {
                var statusType = typeof(ChargingSessionDTO).GetProperty("Status")!.PropertyType;
                if (Enum.TryParse(statusType, status, true, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }
            }
            
            var result = await _dashboardService.GetAllSessionsAsync(stationId, (dynamic?)statusEnum, startDate, endDate, page, pageSize);
            return Ok(result);
        }
    }
}

