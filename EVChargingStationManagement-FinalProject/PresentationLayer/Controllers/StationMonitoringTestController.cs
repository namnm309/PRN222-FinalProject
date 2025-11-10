using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    /// <summary>
    /// Test controller for triggering SignalR broadcasts manually
    /// Use this to test real-time monitoring without actual database changes
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,CSStaff")]
    public class StationMonitoringTestController : ControllerBase
    {
        private readonly IStationMonitoringService _monitoringService;

        public StationMonitoringTestController(IStationMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        /// <summary>
        /// Test spot status change broadcast
        /// </summary>
        [HttpPost("test/spot-status")]
        public async Task<IActionResult> TestSpotStatusChange([FromBody] TestSpotStatusRequest request)
        {
            await _monitoringService.BroadcastSpotStatusChange(
                request.SpotId,
                request.StationId,
                request.Status,
                request.SpotNumber
            );

            return Ok(new { message = "Spot status change broadcasted", data = request });
        }

        /// <summary>
        /// Test station status change broadcast
        /// </summary>
        [HttpPost("test/station-status")]
        public async Task<IActionResult> TestStationStatusChange([FromBody] TestStationStatusRequest request)
        {
            await _monitoringService.BroadcastStationStatusChange(
                request.StationId,
                request.Status,
                request.StationName
            );

            return Ok(new { message = "Station status change broadcasted", data = request });
        }

        /// <summary>
        /// Test session started broadcast
        /// </summary>
        [HttpPost("test/session-started")]
        public async Task<IActionResult> TestSessionStarted([FromBody] TestSessionRequest request)
        {
            await _monitoringService.BroadcastSessionStarted(
                request.SessionId,
                request.SpotId,
                request.StationId,
                request.UserName
            );

            return Ok(new { message = "Session started broadcasted", data = request });
        }

        /// <summary>
        /// Test session ended broadcast
        /// </summary>
        [HttpPost("test/session-ended")]
        public async Task<IActionResult> TestSessionEnded([FromBody] TestSessionEndRequest request)
        {
            await _monitoringService.BroadcastSessionEnded(
                request.SessionId,
                request.SpotId,
                request.StationId,
                request.EnergyConsumed
            );

            return Ok(new { message = "Session ended broadcasted", data = request });
        }

        /// <summary>
        /// Test stats update broadcast
        /// </summary>
        [HttpPost("test/stats-update")]
        public async Task<IActionResult> TestStatsUpdate([FromBody] TestStatsRequest request)
        {
            await _monitoringService.BroadcastStatsUpdate(
                request.Available,
                request.Occupied,
                request.Maintenance,
                request.Offline
            );

            return Ok(new { message = "Stats update broadcasted", data = request });
        }

        /// <summary>
        /// Test error reported broadcast
        /// </summary>
        [HttpPost("test/error-reported")]
        public async Task<IActionResult> TestErrorReported([FromBody] TestErrorRequest request)
        {
            await _monitoringService.BroadcastErrorReported(
                request.ErrorId,
                request.StationId,
                request.Title,
                request.Severity
            );

            return Ok(new { message = "Error reported broadcasted", data = request });
        }

        /// <summary>
        /// Test maintenance scheduled broadcast
        /// </summary>
        [HttpPost("test/maintenance-scheduled")]
        public async Task<IActionResult> TestMaintenanceScheduled([FromBody] TestMaintenanceRequest request)
        {
            await _monitoringService.BroadcastMaintenanceScheduled(
                request.MaintenanceId,
                request.StationId,
                request.ScheduledDate,
                request.Title
            );

            return Ok(new { message = "Maintenance scheduled broadcasted", data = request });
        }

        /// <summary>
        /// Test staff alert broadcast
        /// </summary>
        [HttpPost("test/staff-alert")]
        public async Task<IActionResult> TestStaffAlert([FromBody] TestAlertRequest request)
        {
            await _monitoringService.SendAlertToStaff(
                request.Message,
                request.Severity,
                request.Data
            );

            return Ok(new { message = "Staff alert broadcasted", data = request });
        }
    }

    #region Request Models

    public class TestSpotStatusRequest
    {
        public Guid SpotId { get; set; }
        public Guid StationId { get; set; }
        public int Status { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
    }

    public class TestStationStatusRequest
    {
        public Guid StationId { get; set; }
        public int Status { get; set; }
        public string StationName { get; set; } = string.Empty;
    }

    public class TestSessionRequest
    {
        public Guid SessionId { get; set; }
        public Guid SpotId { get; set; }
        public Guid StationId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class TestSessionEndRequest
    {
        public Guid SessionId { get; set; }
        public Guid SpotId { get; set; }
        public Guid StationId { get; set; }
        public decimal EnergyConsumed { get; set; }
    }

    public class TestStatsRequest
    {
        public int Available { get; set; }
        public int Occupied { get; set; }
        public int Maintenance { get; set; }
        public int Offline { get; set; }
    }

    public class TestErrorRequest
    {
        public Guid ErrorId { get; set; }
        public Guid StationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
    }

    public class TestMaintenanceRequest
    {
        public Guid MaintenanceId { get; set; }
        public Guid StationId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class TestAlertRequest
    {
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "info";
        public Dictionary<string, object>? Data { get; set; }
    }

    #endregion
}

