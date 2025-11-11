using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QrCodeController : ControllerBase
    {
        private readonly IQrCodeService _qrCodeService;
        private readonly IChargingSpotService _spotService;
        private readonly IReservationService _reservationService;

        public QrCodeController(
            IQrCodeService qrCodeService,
            IChargingSpotService spotService,
            IReservationService reservationService)
        {
            _qrCodeService = qrCodeService;
            _spotService = spotService;
            _reservationService = reservationService;
        }

        [HttpGet("spot/{spotId}")]
        public async Task<IActionResult> GetSpotQrCode(Guid spotId)
        {
            var spot = await _spotService.GetSpotByIdAsync(spotId);
            if (spot == null)
            {
                return NotFound(new { message = "Charging spot not found" });
            }

            var qrCode = _qrCodeService.GenerateQrCodeForSpot(spotId);
            return Ok(new { QrCode = qrCode, SpotId = spotId });
        }

        [HttpGet("reservation/{reservationId}")]
        public async Task<IActionResult> GetReservationQrCode(Guid reservationId)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(reservationId);
            if (reservation == null)
            {
                return NotFound(new { message = "Reservation not found" });
            }

            var userId = GetUserId();
            if (reservation.UserId != userId)
            {
                return Forbid();
            }

            var spotId = reservation.ChargingSpotId;
            var qrCode = _qrCodeService.GenerateQrCodeForSpot(spotId);
            return Ok(new { QrCode = qrCode, SpotId = spotId, ReservationId = reservationId });
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                throw new UnauthorizedAccessException("User ID not found");
            }
            return Guid.Parse(userIdClaim.Value);
        }
    }
}

