using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.DTOs;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StationMaintenanceController : ControllerBase
    {
        private readonly IStationMaintenanceService _maintenanceService;

        public StationMaintenanceController(IStationMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMaintenances()
        {
            var maintenances = await _maintenanceService.GetAllMaintenancesAsync();
            var maintenanceDTOs = maintenances.Select(m => MapToDTO(m)).ToList();
            return Ok(maintenanceDTOs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMaintenanceById(Guid id)
        {
            var maintenance = await _maintenanceService.GetMaintenanceByIdAsync(id);
            if (maintenance == null)
                return NotFound(new { message = "Maintenance record not found" });

            return Ok(MapToDTO(maintenance));
        }

        [HttpGet("station/{stationId}")]
        public async Task<IActionResult> GetMaintenancesByStationId(Guid stationId)
        {
            var maintenances = await _maintenanceService.GetMaintenancesByStationIdAsync(stationId);
            var maintenanceDTOs = maintenances.Select(m => MapToDTO(m)).ToList();
            return Ok(maintenanceDTOs);
        }

        [HttpGet("spot/{spotId}")]
        public async Task<IActionResult> GetMaintenancesBySpotId(Guid spotId)
        {
            var maintenances = await _maintenanceService.GetMaintenancesBySpotIdAsync(spotId);
            var maintenanceDTOs = maintenances.Select(m => MapToDTO(m)).ToList();
            return Ok(maintenanceDTOs);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetMaintenancesByStatus(MaintenanceStatus status)
        {
            var maintenances = await _maintenanceService.GetMaintenancesByStatusAsync(status);
            var maintenanceDTOs = maintenances.Select(m => MapToDTO(m)).ToList();
            return Ok(maintenanceDTOs);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetMaintenancesByUserId(Guid userId)
        {
            var maintenances = await _maintenanceService.GetMaintenancesByUserIdAsync(userId);
            var maintenanceDTOs = maintenances.Select(m => MapToDTO(m)).ToList();
            return Ok(maintenanceDTOs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> CreateMaintenance([FromBody] CreateStationMaintenanceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var maintenance = new StationMaintenance
                {
                    ChargingStationId = request.ChargingStationId,
                    ChargingSpotId = request.ChargingSpotId,
                    ReportedByUserId = request.ReportedByUserId,
                    AssignedToUserId = request.AssignedToUserId,
                    ScheduledDate = request.ScheduledDate,
                    Status = request.Status,
                    Title = request.Title,
                    Description = request.Description,
                    Notes = request.Notes
                };

                var createdMaintenance = await _maintenanceService.CreateMaintenanceAsync(maintenance);
                return CreatedAtAction(nameof(GetMaintenanceById), new { id = createdMaintenance.Id }, MapToDTO(createdMaintenance));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateMaintenance(Guid id, [FromBody] UpdateStationMaintenanceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingMaintenance = await _maintenanceService.GetMaintenanceByIdAsync(id);
            if (existingMaintenance == null)
                return NotFound(new { message = "Maintenance record not found" });

            var maintenance = new StationMaintenance
            {
                ChargingSpotId = request.ChargingSpotId ?? existingMaintenance.ChargingSpotId,
                AssignedToUserId = request.AssignedToUserId ?? existingMaintenance.AssignedToUserId,
                ScheduledDate = request.ScheduledDate ?? existingMaintenance.ScheduledDate,
                StartDate = request.StartDate ?? existingMaintenance.StartDate,
                EndDate = request.EndDate ?? existingMaintenance.EndDate,
                Status = request.Status,
                Title = request.Title,
                Description = request.Description,
                Notes = request.Notes
            };

            var updatedMaintenance = await _maintenanceService.UpdateMaintenanceAsync(id, maintenance);
            if (updatedMaintenance == null)
                return NotFound(new { message = "Maintenance record not found" });

            return Ok(MapToDTO(updatedMaintenance));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMaintenance(Guid id)
        {
            var result = await _maintenanceService.DeleteMaintenanceAsync(id);
            if (!result)
                return NotFound(new { message = "Maintenance record not found" });

            return Ok(new { message = "Maintenance record deleted successfully" });
        }

        private StationMaintenanceDTO MapToDTO(StationMaintenance maintenance)
        {
            return new StationMaintenanceDTO
            {
                Id = maintenance.Id,
                ChargingStationId = maintenance.ChargingStationId,
                ChargingStationName = maintenance.ChargingStation?.Name,
                ChargingSpotId = maintenance.ChargingSpotId,
                ChargingSpotNumber = maintenance.ChargingSpot?.SpotNumber,
                ReportedByUserId = maintenance.ReportedByUserId,
                ReportedByUserName = maintenance.ReportedByUser?.FullName,
                AssignedToUserId = maintenance.AssignedToUserId,
                AssignedToUserName = maintenance.AssignedToUser?.FullName,
                ScheduledDate = maintenance.ScheduledDate,
                StartDate = maintenance.StartDate,
                EndDate = maintenance.EndDate,
                Status = maintenance.Status,
                Title = maintenance.Title,
                Description = maintenance.Description,
                Notes = maintenance.Notes,
                CreatedAt = maintenance.CreatedAt,
                UpdatedAt = maintenance.UpdatedAt
            };
        }
    }
}

