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
			
			// Message rõ ràng về trạng thái đặt trạm sạc
			if (IsSuccess)
			{
				Message = "Thanh toán thành công! Đã đặt trạm sạc thành công.";
			}
			else
			{
				var codeMsg = ResponseCode switch
				{
					"07" => "Trừ tiền thành công nhưng bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
					"09" => "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking.",
					"10" => "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.",
					"11" => "Đã hết hạn chờ thanh toán. Vui lòng thử lại.",
					"12" => "Thẻ/Tài khoản bị khóa.",
					"13" => "Nhập sai mật khẩu xác thực giao dịch (OTP).",
					"51" => "Tài khoản không đủ số dư để thực hiện giao dịch.",
					"65" => "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
					"75" => "Ngân hàng thanh toán đang bảo trì.",
					"79" => "Nhập sai mật khẩu thanh toán quá số lần quy định.",
					_ => $"Thanh toán thất bại (Mã lỗi: {ResponseCode ?? "N/A"})."
				};
				Message = $"Đặt trạm sạc không thành công. {codeMsg}";
			}
		}
	}
}


