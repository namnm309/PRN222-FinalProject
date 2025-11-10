using BusinessLayer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PresentationLayer.Helpers;
using BusinessLayer.Services;

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
			string returnUrl = _config["Vnpay:ReturnUrl"]!;

			var vnpParams = new SortedDictionary<string, string>
			{
				{"vnp_Version", "2.1.0"},
				{"vnp_Command", "pay"},
				{"vnp_TmnCode", tmnCode},
				{"vnp_Amount", (order.Amount * 100).ToString()},
				{"vnp_CurrCode", "VND"},
				{"vnp_TxnRef", order.OrderId.ToString()},
				{"vnp_OrderInfo", order.Description ?? $"Thanh toan don {order.OrderId}"},
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

			// Record payment if possible
			if (Guid.TryParse(txnRef, out var bookingId))
			{
				var isSuccess = valid && responseCode == "00";
				await _bookingService.RecordVnPayResultAsync(bookingId, isSuccess, amount, bankCode, txnRef, transNo);
			}

			var query = HttpContext.Request.QueryString.Value ?? string.Empty;
			// Redirect to UI page to show modal result
			return Redirect($"/Payment/Result{query}");
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


