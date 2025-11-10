using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Pages.Payment
{
	public class ResultModel : PageModel
	{
		public bool IsSuccess { get; set; }
		public string Message { get; set; } = string.Empty;
		public string? ResponseCode { get; set; }
		public string? TxnRef { get; set; }
		public string? Amount { get; set; }

		public void OnGet()
		{
			ResponseCode = Request.Query["vnp_ResponseCode"];
			TxnRef = Request.Query["vnp_TxnRef"];
			Amount = Request.Query["vnp_Amount"];

			// VNPay thành công khi ResponseCode == "00"
			IsSuccess = string.Equals(ResponseCode, "00", StringComparison.OrdinalIgnoreCase);
			Message = IsSuccess
				? "Thanh toán thành công."
				: $"Thanh toán thất bại (Code: {ResponseCode ?? "N/A"}).";
		}
	}
}


