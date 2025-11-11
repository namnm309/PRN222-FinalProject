using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,CSStaff")]
    public class ReportingController : ControllerBase
    {
        private readonly IReportingService _reportingService;

        public ReportingController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        [HttpGet("stations/{stationId}")]
        public async Task<IActionResult> GetStationReport(
            Guid stationId,
            [FromQuery] DateOnly? reportDate = null,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null)
        {
            if (reportDate.HasValue)
            {
                var report = await _reportingService.GetStationReportAsync(stationId, reportDate.Value);
                if (report == null)
                    return NotFound(new { message = "Report not found" });
                return Ok(report);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                var reports = await _reportingService.GetStationReportsAsync(stationId, startDate.Value, endDate.Value);
                return Ok(reports);
            }

            return BadRequest(new { message = "Either reportDate or both startDate and endDate must be provided" });
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] Guid? stationId = null)
        {
            var report = await _reportingService.GetRevenueReportAsync(startDate, endDate, stationId);
            return Ok(report);
        }

        [HttpGet("usage-statistics")]
        public async Task<IActionResult> GetUsageStatistics(
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] Guid? stationId = null)
        {
            var statistics = await _reportingService.GetUsageStatisticsAsync(startDate, endDate, stationId);
            return Ok(statistics);
        }

        [HttpPost("stations/{stationId}/generate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateDailyReport(Guid stationId, [FromQuery] DateOnly reportDate)
        {
            var report = await _reportingService.GenerateDailyReportAsync(stationId, reportDate);
            var dto = new StationReportDTO
            {
                Id = report.Id,
                ChargingStationId = report.ChargingStationId,
                ReportDate = report.ReportDate,
                TotalSessions = report.TotalSessions,
                TotalEnergyDeliveredKwh = report.TotalEnergyDeliveredKwh,
                TotalRevenue = report.TotalRevenue,
                PeakHour = report.PeakHour,
                AverageSessionDurationMinutes = report.AverageSessionDurationMinutes,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };

            return Ok(dto);
        }
    }
}

