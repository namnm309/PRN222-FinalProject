using System.Linq;
using System.Security.Claims;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChargingSessionController : ControllerBase
    {
        private readonly IChargingSessionService _sessionService;
        private readonly IRealtimeNotifier _notifier;
        private readonly IQrCodeService _qrCodeService;
        private readonly IChargingProgressService _progressService;
        private readonly IChargingSpotService _spotService;
        private readonly IChargingStationService _stationService;

        public ChargingSessionController(
            IChargingSessionService sessionService,
            IRealtimeNotifier notifier,
            IQrCodeService qrCodeService,
            IChargingProgressService progressService,
            IChargingSpotService spotService,
            IChargingStationService stationService)
        {
            _sessionService = sessionService;
            _notifier = notifier;
            _qrCodeService = qrCodeService;
            _progressService = progressService;
            _spotService = spotService;
            _stationService = stationService;
        }

        [HttpGet("me")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> GetMySessions([FromQuery] int limit = 20)
        {
            var userId = GetUserId();
            var sessions = await _sessionService.GetSessionsForUserAsync(userId, limit);
            return Ok(sessions.Select(MapToDto));
        }

        [HttpGet("active")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> GetActiveSessions([FromQuery] Guid? stationId)
        {
            var sessions = await _sessionService.GetActiveSessionsAsync(stationId);
            return Ok(sessions.Select(MapToDto));
        }

        [HttpGet("me/active")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> GetMyActiveSession()
        {
            var userId = GetUserId();
            var session = await _sessionService.GetActiveSessionForUserAsync(userId);
            if (session == null)
            {
                return NotFound(new { message = "No active session found" });
            }
            return Ok(MapToDto(session));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (session.UserId != userId && role != UserRole.Admin.ToString() && role != UserRole.CSStaff.ToString())
            {
                return Forbid();
            }

            return Ok(MapToDto(session));
        }

        [HttpPost]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> StartSession([FromBody] StartChargingSessionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var session = new ChargingSession
            {
                ChargingSpotId = request.ChargingSpotId,
                ReservationId = request.ReservationId,
                VehicleId = request.VehicleId,
                EnergyRequestedKwh = request.EnergyRequestedKwh,
                PricePerKwh = request.PricePerKwh,
                Notes = request.Notes
            };

            var created = await _sessionService.StartSessionAsync(GetUserId(), session);
            await _notifier.NotifySessionChangedAsync(created);
            
            // Notify station availability change
            var stationId = created.ChargingSpot?.ChargingStationId ?? Guid.Empty;
            if (stationId != Guid.Empty)
            {
                await NotifyStationAvailabilityAsync(stationId);
            }
            
            return CreatedAtAction(nameof(GetSessionById), new { id = created.Id }, MapToDto(created));
        }

        [HttpPost("{id:guid}/complete")]
        [Authorize(Roles = "EVDriver,Admin,CSStaff")]
        public async Task<IActionResult> CompleteSession(Guid id, [FromBody] CompleteChargingSessionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var session = await _sessionService.CompleteSessionAsync(id, request.EnergyDeliveredKwh, request.Cost, request.PricePerKwh, request.Notes);
            if (session == null)
            {
                return NotFound();
            }

            await _notifier.NotifySessionChangedAsync(session);
            
            // Notify station availability change
            var stationId = session.ChargingSpot?.ChargingStationId ?? Guid.Empty;
            if (stationId != Guid.Empty)
            {
                await NotifyStationAvailabilityAsync(stationId);
            }
            
            return Ok(MapToDto(session));
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateSessionStatus(Guid id, [FromBody] UpdateChargingSessionStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!Enum.IsDefined(typeof(ChargingSessionStatus), request.Status))
            {
                return BadRequest(new { message = "Trạng thái không hợp lệ" });
            }

            var session = await _sessionService.UpdateSessionStatusAsync(id, request.Status, request.Notes);
            if (session == null)
            {
                return NotFound();
            }

            await _notifier.NotifySessionChangedAsync(session);
            
            // Notify station availability change when status changes
            var stationId = session.ChargingSpot?.ChargingStationId ?? Guid.Empty;
            if (stationId != Guid.Empty)
            {
                await NotifyStationAvailabilityAsync(stationId);
            }
            
            return Ok(MapToDto(session));
        }

        [HttpPost("scan-qr")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> ScanQrCode([FromBody] QrCodeScanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.QrCode))
                return BadRequest(new { message = "QR code is required" });

            var spotId = _qrCodeService.ParseQrCode(request.QrCode);
            if (!spotId.HasValue)
                return BadRequest(new { message = "Invalid QR code" });

            var spot = await _spotService.GetSpotByIdAsync(spotId.Value);
            if (spot == null)
                return NotFound(new { message = "Charging spot not found" });

            // Kiểm tra station status - chỉ cho phép bắt đầu sạc khi station Active
            if (spot.ChargingStation == null || spot.ChargingStation.Status != StationStatus.Active)
                return BadRequest(new { message = "Trạm sạc hiện không khả dụng để bắt đầu sạc." });

            if (spot.Status != SpotStatus.Available)
                return BadRequest(new { message = "Charging spot is not available" });

            var session = new ChargingSession
            {
                ChargingSpotId = spotId.Value,
                ReservationId = request.ReservationId,
                VehicleId = request.VehicleId,
                TargetSocPercentage = request.TargetSocPercentage,
                EnergyRequestedKwh = request.EnergyRequestedKwh,
                QrCodeScanned = request.QrCode
            };

            var created = await _sessionService.StartSessionAsync(GetUserId(), session);
            await _notifier.NotifySessionChangedAsync(created);
            
            // Notify station availability change
            var stationId = created.ChargingSpot?.ChargingStationId ?? Guid.Empty;
            if (stationId != Guid.Empty)
            {
                await NotifyStationAvailabilityAsync(stationId);
            }
            
            return CreatedAtAction(nameof(GetSessionById), new { id = created.Id }, MapToDto(created));
        }

        [HttpGet("{id:guid}/progress")]
        [Authorize(Roles = "EVDriver,Admin,CSStaff")]
        public async Task<IActionResult> GetProgress(Guid id)
        {
            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);
            
            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null)
                return NotFound();

            if (session.UserId != userId && role != UserRole.Admin.ToString() && role != UserRole.CSStaff.ToString())
                return Forbid();

            var progress = await _progressService.GetProgressAsync(id);
            if (progress == null)
                return NotFound();

            return Ok(progress);
        }

        [HttpPut("{id:guid}/progress")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateChargingProgressRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _progressService.UpdateProgressAsync(id, request);
                var progress = await _progressService.GetProgressAsync(id);
                return Ok(progress);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}/progress/history")]
        [Authorize(Roles = "EVDriver,Admin,CSStaff")]
        public async Task<IActionResult> GetProgressHistory(Guid id)
        {
            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);
            
            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null)
                return NotFound();

            if (session.UserId != userId && role != UserRole.Admin.ToString() && role != UserRole.CSStaff.ToString())
                return Forbid();

            var history = await _progressService.GetProgressHistoryAsync(id);
            return Ok(history);
        }

        private ChargingSessionDTO MapToDto(ChargingSession session)
        {
            return new ChargingSessionDTO
            {
                Id = session.Id,
                ChargingSpotId = session.ChargingSpotId,
                ChargingSpotNumber = session.ChargingSpot?.SpotNumber,
                ChargingStationId = session.ChargingSpot?.ChargingStationId ?? Guid.Empty,
                ChargingStationName = session.ChargingSpot?.ChargingStation?.Name,
                UserId = session.UserId,
                UserName = session.User != null ? (session.User.FullName ?? session.User.Email) : null,
                VehicleId = session.VehicleId,
                VehicleName = session.Vehicle != null ? $"{session.Vehicle.Make} {session.Vehicle.Model}" : null,
                ReservationId = session.ReservationId,
                ScheduledStartTime = session.Reservation?.ScheduledStartTime,
                ScheduledEndTime = session.Reservation?.ScheduledEndTime,
                Status = session.Status,
                SessionStartTime = session.SessionStartTime,
                SessionEndTime = session.SessionEndTime,
                EnergyDeliveredKwh = session.EnergyDeliveredKwh,
                EnergyRequestedKwh = session.EnergyRequestedKwh,
                Cost = session.Cost,
                PricePerKwh = session.PricePerKwh,
                ChargingSpotPower = session.ChargingSpot?.PowerOutput,
                ExternalSessionId = session.ExternalSessionId,
                Notes = session.Notes
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
                var availableSpots = station.Status == StationStatus.Active 
                    ? spots.Count(s => s.Status == SpotStatus.Available)
                    : 0;
                
                await _notifier.NotifyStationAvailabilityChangedAsync(stationId, totalSpots, availableSpots);
            }
        }
    }
}

