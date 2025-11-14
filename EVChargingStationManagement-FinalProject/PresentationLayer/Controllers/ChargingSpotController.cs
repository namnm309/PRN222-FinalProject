using BusinessLayer.Services;
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
        private readonly IReservationService _reservationService;

        public ChargingSpotController(IChargingSpotService spotService, IRealtimeNotifier notifier, IReservationService reservationService)
        {
            _spotService = spotService;
            _notifier = notifier;
            _reservationService = reservationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSpots()
        {
            var spots = await _spotService.GetAllSpotsAsync();
            return Ok(spots);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSpotById(Guid id)
        {
            var spot = await _spotService.GetSpotByIdAsync(id);
            if (spot == null)
                return NotFound(new { message = "Charging spot not found" });

            return Ok(spot);
        }

        [HttpGet("station/{stationId}")]
        public async Task<IActionResult> GetSpotsByStationId(Guid stationId)
        {
            var spots = await _spotService.GetSpotsByStationIdAsync(stationId);
            return Ok(spots);
        }

        [HttpGet("station/{stationId}/available")]
        public async Task<IActionResult> GetAvailableSpotsByStationId(Guid stationId)
        {
            var spots = await _spotService.GetAvailableSpotsByStationIdAsync(stationId);
            return Ok(spots);
        }

        [HttpGet("station/{stationId}/all")]
        public async Task<IActionResult> GetAllSpotsByStationIdWithReservationInfo(Guid stationId)
        {
            var spotDTOs = await _reservationService.GetAvailableSpotsWithReservationInfoAsync(stationId);
            return Ok(spotDTOs);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetSpotsByStatus([FromRoute] string status)
        {
            // Parse string to enum using reflection from DTO property type
            var statusPropertyType = typeof(ChargingSpotDTO).GetProperty("Status")!.PropertyType;
            if (!Enum.TryParse(statusPropertyType, status, true, out var statusValue))
            {
                return BadRequest(new { message = "Invalid status value" });
            }
            
            // Call service method using reflection to avoid importing DataAccessLayer.Enums
            var method = typeof(IChargingSpotService).GetMethod("GetSpotsByStatusAsync");
            var task = (Task)method!.Invoke(_spotService, new[] { statusValue })!;
            await task;
            var resultProperty = task.GetType().GetProperty("Result");
            var spots = resultProperty!.GetValue(task);
            return Ok(spots);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> CreateSpot([FromBody] CreateChargingSpotRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdSpot = await _spotService.CreateSpotAsync(request);
                await _notifier.NotifySpotStatusChangedAsync(createdSpot);
                await _notifier.NotifySpotsListUpdatedAsync(createdSpot.ChargingStationId);
                return CreatedAtAction(nameof(GetSpotById), new { id = createdSpot.Id }, createdSpot);
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
                var updatedSpot = await _spotService.UpdateSpotAsync(id, request);
                if (updatedSpot == null)
                    return NotFound(new { message = "Charging spot not found" });

                await _notifier.NotifySpotStatusChangedAsync(updatedSpot);
                await _notifier.NotifySpotsListUpdatedAsync(updatedSpot.ChargingStationId);
                return Ok(updatedSpot);
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
    }
}

