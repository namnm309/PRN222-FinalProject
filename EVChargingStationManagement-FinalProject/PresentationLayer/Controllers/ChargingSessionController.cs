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
            return Ok(sessions);
        }

        [HttpGet("active")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> GetActiveSessions([FromQuery] Guid? stationId)
        {
            var sessions = await _sessionService.GetActiveSessionsAsync(stationId);
            return Ok(sessions);
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
            return Ok(session);
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
            // Check role using string comparison instead of enum
            if (session.UserId != userId && role != "Admin" && role != "CSStaff")
            {
                return Forbid();
            }

            return Ok(session);
        }

        [HttpPost]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> StartSession([FromBody] StartChargingSessionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _sessionService.StartSessionAsync(GetUserId(), request);
            await _notifier.NotifySessionChangedAsync(created);
            
            // Notify spot status change (spot becomes occupied)
            var spot = await _spotService.GetSpotByIdAsync(created.ChargingSpotId);
            if (spot != null)
            {
                await _notifier.NotifySpotStatusChangedAsync(spot);
                await _notifier.NotifySpotsListUpdatedAsync(spot.ChargingStationId);
            }
            
            // Notify station availability change
            if (created.ChargingStationId != Guid.Empty)
            {
                await NotifyStationAvailabilityAsync(created.ChargingStationId);
            }
            
            return CreatedAtAction(nameof(GetSessionById), new { id = created.Id }, created);
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
            if (session.ChargingStationId != Guid.Empty)
            {
                await NotifyStationAvailabilityAsync(session.ChargingStationId);
            }
            
            return Ok(session);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateSessionStatus(Guid id, [FromBody] UpdateChargingSessionStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Validate enum using reflection from DTO
            var statusType = typeof(ChargingSessionDTO).GetProperty("Status")!.PropertyType;
            if (!Enum.IsDefined(statusType, request.Status))
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
            if (session.ChargingStationId != Guid.Empty)
            {
                await NotifyStationAvailabilityAsync(session.ChargingStationId);
            }
            
            return Ok(session);
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
            var station = await _stationService.GetStationByIdAsync(spot.ChargingStationId);
            if (station == null || station.Status.ToString() != "Active")
                return BadRequest(new { message = "Trạm sạc hiện không khả dụng để bắt đầu sạc." });
            
            if (spot.Status.ToString() != "Available")
                return BadRequest(new { message = "Charging spot is not available" });

            var startRequest = new StartChargingSessionRequest
            {
                ChargingSpotId = spotId.Value,
                ReservationId = request.ReservationId,
                VehicleId = request.VehicleId,
                TargetSocPercentage = request.TargetSocPercentage,
                EnergyRequestedKwh = request.EnergyRequestedKwh,
                QrCode = request.QrCode
            };

            var created = await _sessionService.StartSessionAsync(GetUserId(), startRequest);
            await _notifier.NotifySessionChangedAsync(created);
            
            // Notify spot status change (spot becomes occupied)
            var updatedSpot = await _spotService.GetSpotByIdAsync(created.ChargingSpotId);
            if (updatedSpot != null)
            {
                await _notifier.NotifySpotStatusChangedAsync(updatedSpot);
                await _notifier.NotifySpotsListUpdatedAsync(updatedSpot.ChargingStationId);
            }
            
            // Notify station availability change
            if (created.ChargingStationId != Guid.Empty)
            {
                await NotifyStationAvailabilityAsync(created.ChargingStationId);
            }
            
            return CreatedAtAction(nameof(GetSessionById), new { id = created.Id }, created);
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

            if (session.UserId != userId && role != "Admin" && role != "CSStaff")
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

            if (session.UserId != userId && role != "Admin" && role != "CSStaff")
                return Forbid();

            var history = await _progressService.GetProgressHistoryAsync(id);
            return Ok(history);
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
                var availableSpots = station.Status.ToString() == "Active" 
                    ? spotsList.Count(s => s.Status.ToString() == "Available")
                    : 0;
                
                await _notifier.NotifyStationAvailabilityChangedAsync(stationId, totalSpots, availableSpots);
            }
        }
    }
}

