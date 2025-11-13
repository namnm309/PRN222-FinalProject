using System.Linq;
using System.Security.Claims;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly EVDbContext _context;

        public ReservationController(
            IReservationService reservationService,
            IChargingSpotService spotService,
            IRealtimeNotifier notifier,
            IChargingStationService stationService,
            EVDbContext context)
        {
            _reservationService = reservationService;
            _spotService = spotService;
            _notifier = notifier;
            _stationService = stationService;
            _context = context;
        }

        [HttpGet("me")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> GetMyReservations([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var userId = GetUserId();
            var reservations = await _reservationService.GetReservationsForUserAsync(userId, from, to);
            return Ok(reservations.Select(MapToDto));
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
            if (reservation.UserId != userId && role != UserRole.Admin.ToString() && role != UserRole.CSStaff.ToString())
            {
                return Forbid();
            }

            return Ok(MapToDto(reservation));
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

                var reservation = new Reservation
                {
                    ChargingSpotId = request.ChargingSpotId,
                    VehicleId = request.VehicleId,
                    ScheduledStartTime = request.ScheduledStartTime.ToUniversalTime(),
                    ScheduledEndTime = request.ScheduledEndTime.HasValue ? request.ScheduledEndTime.Value.ToUniversalTime() : default(DateTime),
                    EstimatedEnergyKwh = request.EstimatedEnergyKwh,
                    EstimatedCost = request.EstimatedCost,
                    IsPrepaid = request.IsPrepaid,
                    Notes = request.Notes
                };

                var created = await _reservationService.CreateReservationAsync(GetUserId(), reservation);
                await _notifier.NotifyReservationChangedAsync(created);
                
                // Notify station availability change
                var stationId = created.ChargingSpot?.ChargingStationId ?? Guid.Empty;
                if (stationId != Guid.Empty)
                {
                    await NotifyStationAvailabilityAsync(stationId);
                    // Notify spots list updated to refresh dropdown
                    await _notifier.NotifySpotsListUpdatedAsync(stationId);
                }
                
                return CreatedAtAction(nameof(GetReservationById), new { id = created.Id }, MapToDto(created));
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
            if (!Enum.IsDefined(typeof(ReservationStatus), request.Status))
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
            var stationId = updated.ChargingSpot?.ChargingStationId ?? Guid.Empty;
            if (stationId != Guid.Empty)
            {
                if (updated.Status == ReservationStatus.Confirmed || 
                    updated.Status == ReservationStatus.Cancelled || 
                    updated.Status == ReservationStatus.Completed)
                {
                    await NotifyStationAvailabilityAsync(stationId);
                }
                // Always notify spots list updated when reservation status changes
                await _notifier.NotifySpotsListUpdatedAsync(stationId);
            }
            
            return Ok(MapToDto(updated));
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
                var stationId = reservation.ChargingSpot?.ChargingStationId ?? Guid.Empty;
                if (stationId != Guid.Empty)
                {
                    await NotifyStationAvailabilityAsync(stationId);
                    // Notify spots list updated to refresh dropdown
                    await _notifier.NotifySpotsListUpdatedAsync(stationId);
                }
            }

            return NoContent();
        }

        [HttpGet("station/{stationId:guid}")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> GetReservationsForStation(Guid stationId, [FromQuery] ReservationStatus? status)
        {
            var reservations = await _reservationService.GetReservationsForStationAsync(stationId, status);
            return Ok(reservations.Select(MapToDto));
        }

        [HttpGet("staff/all")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> GetAllReservationsForStaff([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            // Get all reservations for staff to manage
            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.ChargingSpot)
                    .ThenInclude(s => s!.ChargingStation)
                .Include(r => r.Vehicle)
                .OrderByDescending(r => r.ScheduledStartTime)
                .ToListAsync();

            // Apply date filters if provided
            if (from.HasValue)
            {
                reservations = reservations.Where(r => r.ScheduledStartTime >= from.Value.ToUniversalTime()).ToList();
            }

            if (to.HasValue)
            {
                reservations = reservations.Where(r => r.ScheduledStartTime <= to.Value.ToUniversalTime()).ToList();
            }

            return Ok(reservations.Select(MapToDto));
        }

        private ReservationDTO MapToDto(Reservation reservation)
        {
            return new ReservationDTO
            {
                Id = reservation.Id,
                ChargingSpotId = reservation.ChargingSpotId,
                ChargingSpotNumber = reservation.ChargingSpot?.SpotNumber,
                ChargingStationId = reservation.ChargingSpot?.ChargingStationId ?? Guid.Empty,
                ChargingStationName = reservation.ChargingSpot?.ChargingStation?.Name,
                UserId = reservation.UserId,
                VehicleId = reservation.VehicleId,
                VehicleName = reservation.Vehicle != null ? $"{reservation.Vehicle.Make} {reservation.Vehicle.Model}" : null,
                UserFullName = reservation.User?.FullName,
                Status = reservation.Status,
                ConfirmationCode = reservation.ConfirmationCode,
                ScheduledStartTime = reservation.ScheduledStartTime,
                ScheduledEndTime = reservation.ScheduledEndTime,
                EstimatedEnergyKwh = reservation.EstimatedEnergyKwh,
                EstimatedCost = reservation.EstimatedCost,
                IsPrepaid = reservation.IsPrepaid,
                Notes = reservation.Notes
            };
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
                var spots = station.ChargingSpots?.ToList() ?? new List<ChargingSpot>();
                var totalSpots = spots.Count;
                
                // Nếu station không Active, thì availableSpots = 0
                int availableSpots = 0;
                if (station.Status == StationStatus.Active)
                {
                    // Tính available spots: spot phải Available VÀ không có active reservations
                    var now = DateTime.UtcNow;
                    var spotIds = spots.Select(s => s.Id).ToList();
                    
                    // Load tất cả active reservations cho các spots trong station này một lần
                    var activeReservations = await _context.Reservations
                        .Where(r => spotIds.Contains(r.ChargingSpotId) &&
                                    (r.Status == ReservationStatus.Pending ||
                                     r.Status == ReservationStatus.Confirmed ||
                                     r.Status == ReservationStatus.CheckedIn) &&
                                    r.ScheduledEndTime > now)
                        .Select(r => r.ChargingSpotId)
                        .Distinct()
                        .ToListAsync();
                    
                    // Một spot available nếu: status = Available VÀ không có active reservation
                    availableSpots = spots.Count(s => 
                        s.Status == SpotStatus.Available && 
                        !activeReservations.Contains(s.Id));
                }
                
                await _notifier.NotifyStationAvailabilityChangedAsync(stationId, totalSpots, availableSpots);
            }
        }
    }
}

