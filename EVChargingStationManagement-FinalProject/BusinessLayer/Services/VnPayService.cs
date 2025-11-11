using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using Microsoft.Extensions.Configuration;

namespace BusinessLayer.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _url;
        private readonly string _returnUrl;
        private readonly string _ipnUrl;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _tmnCode = _configuration["VNPay:TmnCode"] ?? "";
            _hashSecret = _configuration["VNPay:HashSecret"] ?? "";
            _url = _configuration["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _returnUrl = _configuration["VNPay:ReturnUrl"] ?? "";
            _ipnUrl = _configuration["VNPay:IpnUrl"] ?? "";
        }

        public string CreatePaymentUrl(PaymentTransaction payment, string returnUrl, string ipAddress)
        {
            var vnpay = new Dictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", ((long)(payment.Amount * 100)).ToString() }, // VNPay expects amount in cents
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", payment.Id.ToString() },
                { "vnp_OrderInfo", $"Thanh toan don hang {payment.Id}" },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", "vn" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_IpAddr", ipAddress },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") }
            };

            // Remove empty values and null values before calculating hash (VNPay requirement)
            var filteredDict = vnpay
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && !string.IsNullOrWhiteSpace(kvp.Value))
                .ToDictionary(x => x.Key, x => x.Value);

            // Sort by key using Ordinal comparison (VNPay requirement)
            var sortedDict = filteredDict.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value);

            // Build query string with URL encoding for both key and value (VNPay requirement)
            var queryString = new StringBuilder();
            foreach (var kvp in sortedDict)
            {
                queryString.Append(Uri.EscapeDataString(kvp.Key));
                queryString.Append("=");
                queryString.Append(Uri.EscapeDataString(kvp.Value));
                queryString.Append("&");
            }
            
            // Remove last '&'
            if (queryString.Length > 0)
            {
                queryString.Length -= 1;
            }
            
            // Create signature from the encoded query string
            var signData = queryString.ToString();
            var vnp_SecureHash = HmacSHA512(_hashSecret, signData);
            
            // Debug logging
            Console.WriteLine($"[VNPay] SignData: {signData}");
            Console.WriteLine($"[VNPay] HashSecret: {_hashSecret}");
            Console.WriteLine($"[VNPay] SecureHash: {vnp_SecureHash}");
            
            // Add secure hash to query string
            queryString.Append("&vnp_SecureHash=");
            queryString.Append(vnp_SecureHash);

            return $"{_url}?{queryString}";
        }

        public VnPayCallbackResult ValidateCallback(Dictionary<string, string> queryParams)
        {
            var result = new VnPayCallbackResult();

            if (queryParams == null || !queryParams.Any())
            {
                result.Success = false;
                result.Message = "Invalid callback data";
                return result;
            }

            var vnpayData = new Dictionary<string, string>();
            foreach (var item in queryParams)
            {
                if (!string.IsNullOrEmpty(item.Value) && item.Key.StartsWith("vnp_"))
                {
                    vnpayData.Add(item.Key, item.Value);
                }
            }

            // Extract secure hash and secure hash type (if exists)
            var vnp_SecureHash = vnpayData.ContainsKey("vnp_SecureHash") ? vnpayData["vnp_SecureHash"] : "";
            vnpayData.Remove("vnp_SecureHash");
            vnpayData.Remove("vnp_SecureHashType"); // Remove if exists, not used in signature calculation

            // Verify signature
            if (!VerifySignature(vnpayData, vnp_SecureHash))
            {
                result.Success = false;
                result.Message = "Invalid signature";
                return result;
            }

            // Extract data
            result.OrderId = vnpayData.ContainsKey("vnp_TxnRef") ? vnpayData["vnp_TxnRef"] : null;
            result.TransactionNo = vnpayData.ContainsKey("vnp_TransactionNo") ? vnpayData["vnp_TransactionNo"] : null;
            result.ResponseCode = vnpayData.ContainsKey("vnp_ResponseCode") ? vnpayData["vnp_ResponseCode"] : null;

            if (vnpayData.ContainsKey("vnp_Amount"))
            {
                if (long.TryParse(vnpayData["vnp_Amount"], out var amount))
                {
                    result.Amount = amount / 100m; // Convert from cents to VND
                }
            }

            // Check response code
            result.Success = result.ResponseCode == "00";
            result.Message = result.Success ? "Giao dịch thành công" : GetResponseMessage(result.ResponseCode);

            return result;
        }

        public bool VerifySignature(Dictionary<string, string> data, string signature)
        {
            if (string.IsNullOrEmpty(signature))
                return false;

            // Remove empty values and null values before calculating hash (VNPay requirement)
            var filteredDict = data
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && !string.IsNullOrWhiteSpace(kvp.Value))
                .ToDictionary(x => x.Key, x => x.Value);

            // Sort by key using Ordinal comparison (VNPay requirement)
            var sortedDict = filteredDict.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value);

            // Build query string with URL encoding for both key and value (VNPay requirement)
            // This matches the way signature was created
            var signDataBuilder = new StringBuilder();
            foreach (var kvp in sortedDict)
            {
                signDataBuilder.Append(Uri.EscapeDataString(kvp.Key));
                signDataBuilder.Append("=");
                signDataBuilder.Append(Uri.EscapeDataString(kvp.Value));
                signDataBuilder.Append("&");
            }
            
            // Remove last '&'
            if (signDataBuilder.Length > 0)
            {
                signDataBuilder.Length -= 1;
            }
            
            var signData = signDataBuilder.ToString();

            // Compute hash
            var computedHash = HmacSHA512(_hashSecret, signData);
            
            // Debug logging
            Console.WriteLine($"[VNPay Verify] SignData: {signData}");
            Console.WriteLine($"[VNPay Verify] Expected Hash: {signature}");
            Console.WriteLine($"[VNPay Verify] Computed Hash: {computedHash}");
            Console.WriteLine($"[VNPay Verify] Match: {computedHash.Equals(signature, StringComparison.OrdinalIgnoreCase)}");

            return computedHash.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (byte theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        private string GetResponseMessage(string? responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking",
                "10" => "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Đã hết hạn chờ thanh toán. Xin vui lòng thực hiện lại giao dịch.",
                "12" => "Thẻ/Tài khoản bị khóa.",
                "13" => "Nhập sai mật khẩu xác thực giao dịch (OTP). Xin vui lòng thực hiện lại giao dịch.",
                "51" => "Tài khoản không đủ số dư để thực hiện giao dịch.",
                "65" => "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định. Xin vui lòng thực hiện lại giao dịch.",
                "99" => "Lỗi không xác định",
                _ => $"Mã lỗi: {responseCode}"
            };
        }
    }
}

