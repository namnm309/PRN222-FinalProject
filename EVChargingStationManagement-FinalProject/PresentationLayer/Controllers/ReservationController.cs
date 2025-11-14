using System.Security.Claims;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly IChargingSpotService _spotService;
        private readonly IRealtimeNotifier _notifier;
        private readonly IChargingStationService _stationService;

        public ReservationController(
            IReservationService reservationService,
            IChargingSpotService spotService,
            IRealtimeNotifier notifier,
            IChargingStationService stationService)
        {
            _reservationService = reservationService;
            _spotService = spotService;
            _notifier = notifier;
            _stationService = stationService;
        }

        [HttpGet("me")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> GetMyReservations([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var userId = GetUserId();
            var reservations = await _reservationService.GetReservationsForUserAsync(userId, from, to);
            return Ok(reservations);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetReservationById(Guid id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (reservation.UserId != userId && role != "Admin" && role != "CSStaff")
            {
                return Forbid();
            }

            return Ok(reservation);
        }

        [HttpPost]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = ModelState });
            }

            try
            {
                var spot = await _spotService.GetSpotByIdAsync(request.ChargingSpotId);
                if (spot == null)
                {
                    return NotFound(new { message = "Không tìm thấy cổng sạc" });
                }

                var created = await _reservationService.CreateReservationAsync(GetUserId(), request);
                await _notifier.NotifyReservationChangedAsync(created);
                
                // Notify station availability change
                if (created.ChargingStationId != Guid.Empty)
                {
                    await NotifyStationAvailabilityAsync(created.ChargingStationId);
                    // Notify spots list updated to refresh dropdown
                    await _notifier.NotifySpotsListUpdatedAsync(created.ChargingStationId);
                }
                
                return CreatedAtAction(nameof(GetReservationById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo đặt trước: " + ex.Message });
            }
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateReservationStatus(Guid id, [FromBody] UpdateReservationStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Validate enum using reflection from DTO
            var statusType = typeof(ReservationDTO).GetProperty("Status")!.PropertyType;
            if (!Enum.IsDefined(statusType, request.Status))
            {
                return BadRequest(new { message = "Trạng thái không hợp lệ" });
            }

            var updated = await _reservationService.UpdateReservationStatusAsync(id, request.Status, request.Notes);
            if (updated == null)
            {
                return NotFound();
            }

            await _notifier.NotifyReservationChangedAsync(updated);
            
            // Notify station availability change when status changes (Confirmed, Cancelled, Completed)
            if (updated.ChargingStationId != Guid.Empty)
            {
                var statusStr = updated.Status.ToString();
                if (statusStr == "Confirmed" || 
                    statusStr == "Cancelled" || 
                    statusStr == "Completed")
                {
                    await NotifyStationAvailabilityAsync(updated.ChargingStationId);
                }
                // Always notify spots list updated when reservation status changes
                await _notifier.NotifySpotsListUpdatedAsync(updated.ChargingStationId);
            }
            
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> CancelReservation(Guid id, [FromBody] UpdateReservationStatusRequest request)
        {
            var userId = GetUserId();
            var success = await _reservationService.CancelReservationAsync(id, userId, request.Notes);
            if (!success)
            {
                return NotFound();
            }

            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation != null)
            {
                // Notify user about reservation change
                await _notifier.NotifyReservationChangedAsync(reservation);
                
                // Notify station availability change - this will trigger SignalR update
                // to refresh homepage immediately when reservation is cancelled
                if (reservation.ChargingStationId != Guid.Empty)
                {
                    await NotifyStationAvailabilityAsync(reservation.ChargingStationId);
                    // Notify spots list updated to refresh dropdown
                    await _notifier.NotifySpotsListUpdatedAsync(reservation.ChargingStationId);
                }
            }

            return NoContent();
        }

        [HttpGet("station/{stationId:guid}")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> GetReservationsForStation(Guid stationId, [FromQuery] string? status)
        {
            // Parse status string to enum using reflection
            object? statusEnum = null;
            if (!string.IsNullOrEmpty(status))
            {
                var statusType = typeof(ReservationDTO).GetProperty("Status")!.PropertyType;
                if (Enum.TryParse(statusType, status, true, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }
            }
            
            // Cast to the expected enum type for the service
            var statusTypeForService = typeof(ReservationDTO).GetProperty("Status")!.PropertyType;
            var typedStatus = statusEnum != null ? Convert.ChangeType(statusEnum, statusTypeForService) : null;
            var reservations = await _reservationService.GetReservationsForStationAsync(stationId, (dynamic?)typedStatus);
            return Ok(reservations);
        }

        [HttpGet("staff/all")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> GetAllReservationsForStaff([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var reservations = await _reservationService.GetAllReservationsForStaffAsync(from, to);
            return Ok(reservations);
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId!);
        }

        private async Task NotifyStationAvailabilityAsync(Guid stationId)
        {
            var station = await _stationService.GetStationByIdAsync(stationId);
            if (station != null)
            {
                var spots = await _spotService.GetSpotsByStationIdAsync(stationId);
                var spotsList = spots.ToList();
                var totalSpots = spotsList.Count;
                
                // Nếu station không Active, thì availableSpots = 0
                int availableSpots = 0;
                if (station.Status.ToString() == "Active")
                {
                    // Tính available spots: spot phải Available VÀ không có active reservations
                    var now = DateTime.UtcNow;
                    var spotIds = spotsList.Select(s => s.Id).ToList();
                    
                    // Load tất cả active reservations cho các spots trong station này một lần
                    var allReservations = await _reservationService.GetReservationsForStationAsync(stationId, null);
                    var reservationsList = allReservations.ToList();
                    var activeReservations = reservationsList
                        .Where(r => spotIds.Contains(r.ChargingSpotId) &&
                                    (r.Status.ToString() == "Pending" ||
                                     r.Status.ToString() == "Confirmed" ||
                                     r.Status.ToString() == "CheckedIn") &&
                                    r.ScheduledEndTime > now)
                        .Select(r => r.ChargingSpotId)
                        .Distinct()
                        .ToList();
                    
                    // Một spot available nếu: status = Available VÀ không có active reservation
                    availableSpots = spotsList.Count(s => 
                        s.Status.ToString() == "Available" && 
                        !activeReservations.Contains(s.Id));
                }
                
                await _notifier.NotifyStationAvailabilityChangedAsync(stationId, totalSpots, availableSpots);
            }
        }
    }
}

