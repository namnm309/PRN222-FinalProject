using System.Linq;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.DTOs;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChargingSpotController : ControllerBase
    {
        private readonly IChargingSpotService _spotService;
        private readonly IRealtimeNotifier _notifier;

        public ChargingSpotController(IChargingSpotService spotService, IRealtimeNotifier notifier)
        {
            _spotService = spotService;
            _notifier = notifier;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSpots()
        {
            var spots = await _spotService.GetAllSpotsAsync();
            var spotDTOs = spots.Select(s => MapToDTO(s)).ToList();
            return Ok(spotDTOs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSpotById(Guid id)
        {
            var spot = await _spotService.GetSpotByIdAsync(id);
            if (spot == null)
                return NotFound(new { message = "Charging spot not found" });

            return Ok(MapToDTO(spot));
        }

        [HttpGet("station/{stationId}")]
        public async Task<IActionResult> GetSpotsByStationId(Guid stationId)
        {
            var spots = await _spotService.GetSpotsByStationIdAsync(stationId);
            var spotDTOs = spots.Select(s => MapToDTO(s)).ToList();
            return Ok(spotDTOs);
        }

        [HttpGet("station/{stationId}/available")]
        public async Task<IActionResult> GetAvailableSpotsByStationId(Guid stationId)
        {
            var spots = await _spotService.GetAvailableSpotsByStationIdAsync(stationId);
            var spotDTOs = spots.Select(s => MapToDTO(s)).ToList();
            return Ok(spotDTOs);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetSpotsByStatus(SpotStatus status)
        {
            var spots = await _spotService.GetSpotsByStatusAsync(status);
            var spotDTOs = spots.Select(s => MapToDTO(s)).ToList();
            return Ok(spotDTOs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> CreateSpot([FromBody] CreateChargingSpotRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var spot = new ChargingSpot
                {
                    SpotNumber = request.SpotNumber,
                    ChargingStationId = request.ChargingStationId,
                    Status = request.Status,
                    ConnectorType = request.ConnectorType,
                    PowerOutput = request.PowerOutput,
                    PricePerKwh = request.PricePerKwh,
                    Description = request.Description
                };

                var createdSpot = await _spotService.CreateSpotAsync(spot);
                await _notifier.NotifySpotStatusChangedAsync(createdSpot);
                await _notifier.NotifySpotsListUpdatedAsync(createdSpot.ChargingStationId);
                return CreatedAtAction(nameof(GetSpotById), new { id = createdSpot.Id }, MapToDTO(createdSpot));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateSpot(Guid id, [FromBody] UpdateChargingSpotRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingSpot = await _spotService.GetSpotByIdAsync(id);
                if (existingSpot == null)
                    return NotFound(new { message = "Charging spot not found" });

                var spot = new ChargingSpot
                {
                    SpotNumber = request.SpotNumber,
                    ChargingStationId = existingSpot.ChargingStationId, // Giữ nguyên station
                    Status = request.Status,
                    ConnectorType = request.ConnectorType,
                    PowerOutput = request.PowerOutput,
                    PricePerKwh = request.PricePerKwh,
                    Description = request.Description
                };

                var updatedSpot = await _spotService.UpdateSpotAsync(id, spot);
                if (updatedSpot == null)
                    return NotFound(new { message = "Charging spot not found" });

                await _notifier.NotifySpotStatusChangedAsync(updatedSpot);
                await _notifier.NotifySpotsListUpdatedAsync(updatedSpot.ChargingStationId);
                return Ok(MapToDTO(updatedSpot));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSpot(Guid id)
        {
            var existingSpot = await _spotService.GetSpotByIdAsync(id);
            if (existingSpot == null)
                return NotFound(new { message = "Charging spot not found" });

            var stationId = existingSpot.ChargingStationId;
            var result = await _spotService.DeleteSpotAsync(id);
            if (!result)
                return NotFound(new { message = "Charging spot not found" });

            await _notifier.NotifySpotsListUpdatedAsync(stationId);
            return Ok(new { message = "Charging spot deleted successfully" });
        }

        private ChargingSpotDTO MapToDTO(ChargingSpot spot)
        {
            return new ChargingSpotDTO
            {
                Id = spot.Id,
                SpotNumber = spot.SpotNumber,
                ChargingStationId = spot.ChargingStationId,
                ChargingStationName = spot.ChargingStation?.Name,
                Status = spot.Status,
                ConnectorType = spot.ConnectorType,
                PowerOutput = spot.PowerOutput,
                PricePerKwh = spot.PricePerKwh,
                Description = spot.Description,
                CreatedAt = spot.CreatedAt,
                UpdatedAt = spot.UpdatedAt
            };
        }
    }
}

