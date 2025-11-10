using BusinessLayer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PresentationLayer.Helpers;
using BusinessLayer.Services;
using DataAccessLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class VnPayController : Controller
	{
		private readonly IConfiguration _config;
		private readonly IBookingService _bookingService;

		public VnPayController(IConfiguration config, IBookingService bookingService)
		{
			_config = config;
			_bookingService = bookingService;
		}

		[HttpPost("CreatePayment")]
		public IActionResult CreatePayment([FromBody] OrderDto order)
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
			var now = DateTime.UtcNow.AddHours(7);
			var dateStr = now.ToString("yyyyMMddHHmmss");
			var guidStr = order.OrderId.ToString("N").Substring(0, 8); // First 8 chars only
			var txnRef = $"{dateStr}{guidStr}"; // Total: 22 chars
			
			// Format vnp_OrderInfo: Remove Vietnamese accents and special characters
			var orderInfo = RemoveVietnameseAccents(order.Description ?? $"Thanh toan don {txnRef.Substring(Math.Max(0, txnRef.Length - 20))}");
			if (orderInfo.Length > 255) orderInfo = orderInfo.Substring(0, 255);

			// Format vnp_CreateDate: Vietnam timezone (UTC+7)
			var createDate = now.ToString("yyyyMMddHHmmss");

			var vnpParams = new SortedDictionary<string, string>
			{
				{"vnp_Version", "2.1.0"},
				{"vnp_Command", "pay"},
				{"vnp_TmnCode", tmnCode},
				{"vnp_Amount", (order.Amount * 100).ToString()}, // Must be integer
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
			
			var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString().Normalize(System.Text.NormalizationForm.FormC), @"[^a-zA-Z0-9\s]", "");
			result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
			return result.Trim();
		}

		[HttpGet("Callback")]
		public async Task<IActionResult> Callback()
		{
			var queryCollection = HttpContext.Request.Query;
			var hashSecret = _config["Vnpay:HashSecret"]!;
			var valid = VnPayHelper.VerifyHash(queryCollection, hashSecret);

			// Extract info
			var txnRef = queryCollection["vnp_TxnRef"].ToString();
			var responseCode = queryCollection["vnp_ResponseCode"].ToString();
			var amountRaw = queryCollection["vnp_Amount"].ToString();
			long.TryParse(amountRaw, out var raw);
			var amount = raw / 100; // VNPay returns amount * 100
			var bankCode = queryCollection["vnp_BankCode"].ToString();
			var transNo = queryCollection["vnp_TransactionNo"].ToString();

			// Parse bookingId from txnRef
			// Format: timestamp (14 chars) + first 8 chars of Guid
			// Try to find booking by matching the Guid prefix
			Guid? bookingId = null;
			if (txnRef.Length >= 22)
			{
				// Extract the 8-char Guid prefix (last 8 chars of txnRef after timestamp)
				var guidPrefix = txnRef.Substring(14, 8); // Skip first 14 chars (timestamp)
				// Try to find booking by matching Guid prefix
				// We'll search in BookingPayments first, or use a service method
				bookingId = await FindBookingIdByTxnRefPrefixAsync(guidPrefix);
			}
			else if (Guid.TryParse(txnRef, out var directGuid))
			{
				// Fallback: if txnRef is a full Guid (old format)
				bookingId = directGuid;
			}

			// Record payment if bookingId found
			if (bookingId.HasValue)
			{
				var isSuccess = valid && responseCode == "00";
				await _bookingService.RecordVnPayResultAsync(bookingId.Value, isSuccess, amount, bankCode, txnRef, transNo);
			}

			var query = HttpContext.Request.QueryString.Value ?? string.Empty;
			// Redirect to UI page to show modal result
			return Redirect($"/Payment/Result{query}");
		}

		private async Task<Guid?> FindBookingIdByTxnRefPrefixAsync(string guidPrefix)
		{
			using var scope = HttpContext.RequestServices.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<DataAccessLayer.Data.EVDbContext>();
			
			// First, try to find in BookingPayments by matching Guid prefix in BookingId
			var recentPayments = await dbContext.BookingPayments
				.Include(p => p.Booking)
				.Where(p => p.CreatedAt >= DateTime.UtcNow.AddHours(-24))
				.ToListAsync();
			
			foreach (var payment in recentPayments)
			{
				var bookingGuidStr = payment.BookingId.ToString("N");
				if (bookingGuidStr.StartsWith(guidPrefix, StringComparison.OrdinalIgnoreCase))
				{
					return payment.BookingId;
				}
			}
			
			// If not found, search in recent bookings by matching Guid prefix
			var recentBookings = await dbContext.Bookings
				.Where(b => b.CreatedAt >= DateTime.UtcNow.AddHours(-24))
				.ToListAsync();
			
			foreach (var booking in recentBookings)
			{
				var bookingGuidStr = booking.Id.ToString("N");
				if (bookingGuidStr.StartsWith(guidPrefix, StringComparison.OrdinalIgnoreCase))
				{
					return booking.Id;
				}
			}
			
			return null;
		}

		[HttpGet("IPN")]
		public async Task<IActionResult> IPN()
		{
			var query = HttpContext.Request.Query;
			var hashSecret = _config["Vnpay:HashSecret"]!;
			var valid = VnPayHelper.VerifyHash(query, hashSecret);
			if (!valid) return Json(new { RspCode = "97", Message = "Invalid signature" });

			var txnRef = query["vnp_TxnRef"].ToString();
			var responseCode = query["vnp_ResponseCode"].ToString();
			var amountRaw = query["vnp_Amount"].ToString();
			long.TryParse(amountRaw, out var raw);
			var amount = raw / 100;
			var bankCode = query["vnp_BankCode"].ToString();
			var transNo = query["vnp_TransactionNo"].ToString();

			if (!Guid.TryParse(txnRef, out var bookingId))
				return Json(new { RspCode = "01", Message = "Order not found" });

			var isSuccess = responseCode == "00";
			await _bookingService.RecordVnPayResultAsync(bookingId, isSuccess, amount, bankCode, txnRef, transNo);
			return Json(new { RspCode = "00", Message = "Confirm Success" });
		}
	}
}


