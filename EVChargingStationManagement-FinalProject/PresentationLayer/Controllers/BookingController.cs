using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using PresentationLayer.Helpers;

namespace PresentationLayer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class BookingController : ControllerBase
	{
		private readonly IBookingService _bookingService;
		private readonly IConfiguration _config;

		public BookingController(IBookingService bookingService, IConfiguration config)
		{
			_bookingService = bookingService;
			_config = config;
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(Guid id)
		{
			var booking = await _bookingService.GetByIdAsync(id);
			if (booking == null) return NotFound(new { message = "Booking not found" });
			return Ok(booking);
		}

		[HttpGet("me")]
		public async Task<IActionResult> GetMyBookings()
		{
			var userId = GetUserId();
			var list = await _bookingService.GetByUserAsync(userId);
			return Ok(list);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);
			var userId = GetUserId();
			var created = await _bookingService.CreateAsync(userId, request);
			return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
		}

		[HttpPost("{id}/status")]
		[Authorize(Roles = "Admin,CSStaff")]
		public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusRequest request)
		{
			var updated = await _bookingService.UpdateStatusAsync(id, request.Status, request.Notes);
			if (updated == null) return NotFound(new { message = "Booking not found" });
			return Ok(updated);
		}

		[HttpPost("{id}/cancel")]
		public async Task<IActionResult> Cancel(Guid id)
		{
			var userId = GetUserId();
			var ok = await _bookingService.CancelAsync(id, userId);
			if (!ok) return NotFound(new { message = "Booking not found" });
			return Ok(new { message = "Booking cancelled" });
		}

		[HttpPost("{id}/start")]
		public async Task<IActionResult> StartSession(Guid id, [FromQuery] string? qr = null)
		{
			var sessionId = await _bookingService.StartSessionAsync(id, qr);
			return Ok(new { sessionId });
		}

		[HttpPost("sessions/{sessionId}/end")]
		public async Task<IActionResult> EndSession(Guid sessionId, [FromQuery] decimal energyKwh)
		{
			var ok = await _bookingService.EndSessionAsync(sessionId, energyKwh);
			if (!ok) return NotFound(new { message = "Session not found" });
			return Ok(new { message = "Session completed" });
		}

		[HttpPost("sessions/{sessionId}/pay")]
		public async Task<IActionResult> Pay(Guid sessionId, [FromQuery] string method = "Cash", [FromQuery] string? reference = null)
		{
			var trxId = await _bookingService.PayAsync(sessionId, method, reference);
			return Ok(new { transactionId = trxId });
		}

		// Prepayment via VNPay for a booking (deposit or full)
		[HttpPost("{id}/pay/vnpay")]
		public IActionResult CreateVnPayPayment(Guid id, [FromQuery] long amount, [FromQuery] string? description = null)
		{
			string tmnCode = _config["Vnpay:TmnCode"]!;
			string hashSecret = _config["Vnpay:HashSecret"]!;
			string baseUrl = _config["Vnpay:BaseUrl"]!;
			string returnUrl = _config["Vnpay:ReturnUrl"]!;

			var vnpParams = new SortedDictionary<string, string>
			{
				{"vnp_Version", "2.1.0"},
				{"vnp_Command", "pay"},
				{"vnp_TmnCode", tmnCode},
				{"vnp_Amount", (amount * 100).ToString()},
				{"vnp_CurrCode", "VND"},
				{"vnp_TxnRef", id.ToString()},
				{"vnp_OrderInfo", description ?? $"Thanh toan dat cho {id}"},
				{"vnp_ReturnUrl", returnUrl},
				{"vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss")},
				{"vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"},
				{"vnp_Locale", "vn"}
			};

			string hashData = VnPayHelper.BuildHash(vnpParams, hashSecret);
			vnpParams["vnp_SecureHash"] = hashData;
			string paymentUrl = VnPayHelper.BuildPaymentUrl(baseUrl, vnpParams);
			return Ok(new { paymentUrl });
		}
		[HttpGet("sessions/{sessionId}")]
		public async Task<IActionResult> GetSession(Guid sessionId)
		{
			var s = await _bookingService.GetSessionAsync(sessionId);
			if (s == null) return NotFound(new { message = "Session not found" });
			return Ok(s);
		}

		private Guid GetUserId()
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
				throw new UnauthorizedAccessException("Invalid user token");
			return userId;
		}
	}
}


