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
			
			try
			{
				var created = await _bookingService.CreateAsync(userId, request);
				return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
			}
			catch (InvalidOperationException ex)
			{
				// Return user-friendly error message
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				// Log unexpected errors
				return StatusCode(500, new { message = "An error occurred while creating the booking. Please try again." });
			}
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
			var returnUrlPath = _config["Vnpay:ReturnUrl"]!;
			// Build absolute URL - check if already absolute, otherwise build from request
			string returnUrl;
			if (returnUrlPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
			    returnUrlPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				// Already absolute URL, use as is
				returnUrl = returnUrlPath.TrimEnd('/');
			}
			else
			{
				// Relative path, build absolute URL from request
				returnUrl = $"{Request.Scheme}://{Request.Host}{returnUrlPath}".TrimEnd('/');
			}

			// Format vnp_TxnRef: VNPay requires max 50 chars, unique per transaction
			// Use timestamp (14 chars) + first 8 chars of Guid = 22 chars total (safe)
			var now = DateTime.UtcNow.AddHours(7);
			var dateStr = now.ToString("yyyyMMddHHmmss");
			var guidStr = id.ToString("N").Substring(0, 8); // First 8 chars only
			var txnRef = $"{dateStr}{guidStr}"; // Total: 22 chars
			
			// Format vnp_OrderInfo: Remove Vietnamese accents and special characters
			var orderInfo = RemoveVietnameseAccents(description ?? $"Thanh toan dat cho {txnRef.Substring(Math.Max(0, txnRef.Length - 20))}");
			// Limit to 255 characters
			if (orderInfo.Length > 255) orderInfo = orderInfo.Substring(0, 255);

			// Format vnp_CreateDate: Vietnam timezone (UTC+7)
			var createDate = now.ToString("yyyyMMddHHmmss");

			var vnpParams = new SortedDictionary<string, string>
			{
				{"vnp_Version", "2.1.0"},
				{"vnp_Command", "pay"},
				{"vnp_TmnCode", tmnCode},
				{"vnp_Amount", (amount * 100).ToString()}, // Must be integer, multiply by 100
				{"vnp_CurrCode", "VND"},
				{"vnp_TxnRef", txnRef},
				{"vnp_OrderInfo", orderInfo},
				{"vnp_OrderType", "other"}, // Required by VNPay
				{"vnp_ReturnUrl", returnUrl},
				{"vnp_CreateDate", createDate},
				{"vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"},
				{"vnp_Locale", "vn"}
			};

			string hashData = VnPayHelper.BuildHash(vnpParams, hashSecret);
			vnpParams["vnp_SecureHash"] = hashData;
			string paymentUrl = VnPayHelper.BuildPaymentUrl(baseUrl, vnpParams);
			return Ok(new { paymentUrl });
		}

		private static string RemoveVietnameseAccents(string text)
		{
			if (string.IsNullOrEmpty(text)) return text;
			
			// Remove Vietnamese accents
			var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
			var sb = new System.Text.StringBuilder();
			foreach (var c in normalized)
			{
				var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
				if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
				{
					sb.Append(c);
				}
			}
			
			// Remove special characters, keep only alphanumeric and spaces
			var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString().Normalize(System.Text.NormalizationForm.FormC), @"[^a-zA-Z0-9\s]", "");
			// Replace multiple spaces with single space
			result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
			return result.Trim();
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


