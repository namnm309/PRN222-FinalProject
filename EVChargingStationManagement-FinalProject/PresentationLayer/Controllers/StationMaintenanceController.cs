using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.DTOs;

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
        public async Task<IActionResult> GetMaintenancesByStatus(DataAccessLayer.Enums.MaintenanceStatus status)
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
                var createdMaintenance = await _maintenanceService.CreateMaintenanceAsync(request);
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

            // Set default values from existing maintenance if not provided in request
            if (!request.ChargingSpotId.HasValue && existingMaintenance.ChargingSpotId.HasValue)
            {
                request.ChargingSpotId = existingMaintenance.ChargingSpotId;
            }
            if (!request.AssignedToUserId.HasValue && existingMaintenance.AssignedToUserId.HasValue)
            {
                request.AssignedToUserId = existingMaintenance.AssignedToUserId;
            }
            if (!request.ScheduledDate.HasValue)
            {
                request.ScheduledDate = existingMaintenance.ScheduledDate;
            }
            if (!request.StartDate.HasValue && existingMaintenance.StartDate.HasValue)
            {
                request.StartDate = existingMaintenance.StartDate;
            }
            if (!request.EndDate.HasValue && existingMaintenance.EndDate.HasValue)
            {
                request.EndDate = existingMaintenance.EndDate;
            }
            if (request.Notes == null && existingMaintenance.Notes != null)
            {
                request.Notes = existingMaintenance.Notes;
            }

            var updatedMaintenance = await _maintenanceService.UpdateMaintenanceAsync(id, request);
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

        private StationMaintenanceDTO MapToDTO(DataAccessLayer.Entities.StationMaintenance maintenance)
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

