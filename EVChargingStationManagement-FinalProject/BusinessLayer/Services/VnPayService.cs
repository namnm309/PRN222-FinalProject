using System.Globalization;
using System.Linq;
using System.Net;
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
            _hashSecret = (_configuration["VNPay:HashSecret"] ?? "").Trim(); // Trim to avoid spaces
            _url = _configuration["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _returnUrl = _configuration["VNPay:ReturnUrl"] ?? "";
            _ipnUrl = _configuration["VNPay:IpnUrl"] ?? "";
        }

        /// <summary>
        /// Gets the configured return URL from appsettings.json
        /// This should be set to ngrok URL or production domain to avoid VNPay error 72
        /// </summary>
        public string GetConfiguredReturnUrl()
        {
            return _returnUrl;
        }

        public string CreatePaymentUrl(PaymentTransaction payment, string returnUrl, string ipAddress)
        {
            // Validate payment is not null
            if (payment == null)
            {
                throw new ArgumentNullException(nameof(payment), "Payment transaction cannot be null");
            }

            // Validate payment amount
            if (payment.Amount <= 0)
            {
                throw new ArgumentException($"Payment amount must be greater than 0. Current amount: {payment.Amount}", nameof(payment));
            }
            
            // Validate amount doesn't cause overflow when multiplied by 100
            // VNPay expects amount in cents, so max is long.MaxValue / 100
            var maxAmount = 92233720368547758.07m; // long.MaxValue / 100
            if (payment.Amount > maxAmount)
            {
                throw new ArgumentException($"Payment amount too large. Maximum amount: {maxAmount} VND. Current: {payment.Amount}", nameof(payment));
            }
            
            // Validate payment ID is valid Guid
            if (payment.Id == Guid.Empty)
            {
                throw new ArgumentException("Payment ID cannot be empty Guid", nameof(payment));
            }

            // Validate returnUrl - For UI testing, use a valid domain format (not localhost)
            // Note: VNPay rejects localhost with error 72, but accepts valid domain format even if domain doesn't exist
            // This allows UI testing without needing a real domain
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                // Use a valid domain format for UI testing (VNPay only checks format, not if domain exists)
                returnUrl = "https://example.com/vnpay/return";
                Console.WriteLine("[VNPay] ReturnUrl is empty, using valid domain format for UI testing: " + returnUrl);
            }
            
            // Replace localhost with valid domain format for UI testing
            if (returnUrl.Contains("localhost") || returnUrl.Contains("127.0.0.1") || returnUrl.Contains("::1"))
            {
                // Convert localhost to a valid domain format
                // VNPay checks URL format but doesn't verify if domain actually exists
                returnUrl = returnUrl.Replace("localhost", "example.com")
                                   .Replace("127.0.0.1", "example.com")
                                   .Replace("::1", "example.com");
                Console.WriteLine("[VNPay] Converted localhost to valid domain format for UI testing: " + returnUrl);
            }

            // Validate configuration
            if (string.IsNullOrWhiteSpace(_tmnCode))
            {
                throw new InvalidOperationException("VNPay TmnCode is not configured. Please set VNPay:TmnCode in appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(_hashSecret))
            {
                throw new InvalidOperationException("VNPay HashSecret is not configured. Please set VNPay:HashSecret in appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(_url))
            {
                throw new InvalidOperationException("VNPay Url is not configured. Please set VNPay:Url in appsettings.json");
            }

            // Calculate amount in cents (VNPay requirement)
            // Round to avoid precision issues, then multiply by 100
            var amountInCents = (long)Math.Round(payment.Amount * 100, 0);
            if (amountInCents <= 0)
            {
                throw new ArgumentException($"Invalid amount calculation. Amount: {payment.Amount}, AmountInCents: {amountInCents}", nameof(payment));
            }
            
            // Format TxnRef: Guid without dashes (VNPay requirement)
            var txnRef = payment.Id.ToString("N");
            if (string.IsNullOrEmpty(txnRef) || txnRef.Length != 32)
            {
                throw new ArgumentException($"Invalid TxnRef format. Expected 32 characters, got: {txnRef?.Length ?? 0}", nameof(payment));
            }
            
            // Format CreateDate: yyyyMMddHHmmss (VNPay requirement)
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            if (createDate.Length != 14)
            {
                throw new InvalidOperationException($"Invalid CreateDate format. Expected 14 characters, got: {createDate.Length}");
            }
            
            // Validate IP address format
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = "127.0.0.1";
                Console.WriteLine("[VNPay] Warning: IP address is empty, using default: " + ipAddress);
            }
            
            var vnpay = new Dictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", amountInCents.ToString() },
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", txnRef },
                { "vnp_OrderInfo", $"Thanh toan don hang {payment.Id}" },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", "vn" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_IpAddr", ipAddress },
                { "vnp_CreateDate", createDate }
            };
            
            // Debug logging for UI testing
            Console.WriteLine($"[VNPay CreatePaymentUrl] Payment ID: {payment.Id}");
            Console.WriteLine($"[VNPay CreatePaymentUrl] Amount: {payment.Amount} VND -> {amountInCents} cents");
            Console.WriteLine($"[VNPay CreatePaymentUrl] TxnRef: {txnRef}");
            Console.WriteLine($"[VNPay CreatePaymentUrl] ReturnUrl: {returnUrl}");
            Console.WriteLine($"[VNPay CreatePaymentUrl] IP Address: {ipAddress}");

            // Remove empty values and null values before calculating hash (VNPay requirement)
            var filteredDict = vnpay
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && !string.IsNullOrWhiteSpace(kvp.Value))
                .ToDictionary(x => x.Key, x => x.Value);

            // Sort by key using Ordinal comparison (VNPay requirement)
            var sortedDict = filteredDict.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value);

            // Build query string for signature: KEY giữ nguyên, chỉ encode VALUE
            var signDataBuilder = new StringBuilder();
            foreach (var kvp in sortedDict)
            {
                signDataBuilder.Append(kvp.Key); // KEY giữ nguyên, không encode
                signDataBuilder.Append("=");
                signDataBuilder.Append(WebUtility.UrlEncode(kvp.Value)); // Chỉ encode VALUE
                signDataBuilder.Append("&");
            }
            
            // Remove last '&'
            if (signDataBuilder.Length > 0)
            {
                signDataBuilder.Length -= 1;
            }
            
            // Create signature from the sign data (key not encoded, value encoded)
            var signData = signDataBuilder.ToString();
            var vnp_SecureHash = HmacSHA512(_hashSecret, signData);
            
            // Debug logging
            Console.WriteLine($"[VNPay] SignData: {signData}");
            Console.WriteLine($"[VNPay] HashSecret: {_hashSecret}");
            Console.WriteLine($"[VNPay] SecureHash: {vnp_SecureHash}");
            
            // Build final query string for URL (encode both key and value for URL)
            var queryString = new StringBuilder();
            foreach (var kvp in sortedDict)
            {
                queryString.Append(WebUtility.UrlEncode(kvp.Key));
                queryString.Append("=");
                queryString.Append(WebUtility.UrlEncode(kvp.Value));
                queryString.Append("&");
            }
            
            // Add vnp_SecureHashType and vnp_SecureHash (after signing)
            queryString.Append("vnp_SecureHashType=HMACSHA512&");
            queryString.Append("vnp_SecureHash=");
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
                Console.WriteLine("[VNPay ValidateCallback] No query parameters received");
                return result;
            }

            // Extract secure hash first before filtering
            var vnp_SecureHash = queryParams.ContainsKey("vnp_SecureHash") ? queryParams["vnp_SecureHash"] : "";
            if (string.IsNullOrEmpty(vnp_SecureHash))
            {
                result.Success = false;
                result.Message = "Missing vnp_SecureHash";
                Console.WriteLine("[VNPay ValidateCallback] Missing vnp_SecureHash parameter");
                return result;
            }

            var vnpayData = new Dictionary<string, string>();
            foreach (var item in queryParams)
            {
                // Only include vnp_ parameters and exclude SecureHash and SecureHashType
                if (item.Key.StartsWith("vnp_") && 
                    item.Key != "vnp_SecureHash" && 
                    item.Key != "vnp_SecureHashType" &&
                    !string.IsNullOrEmpty(item.Value))
                {
                    vnpayData.Add(item.Key, item.Value);
                }
            }

            Console.WriteLine($"[VNPay ValidateCallback] Processing {vnpayData.Count} parameters for signature verification");
            Console.WriteLine($"[VNPay ValidateCallback] Received vnp_SecureHash: {vnp_SecureHash}");

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

            // Build query string for signature verification: KEY giữ nguyên, chỉ encode VALUE
            // This matches VNPay's signature calculation method
            var signDataBuilder = new StringBuilder();
            foreach (var kvp in sortedDict)
            {
                signDataBuilder.Append(kvp.Key); // KEY giữ nguyên, không encode
                signDataBuilder.Append("=");
                signDataBuilder.Append(WebUtility.UrlEncode(kvp.Value)); // Chỉ encode VALUE
                signDataBuilder.Append("&");
            }
            
            // Remove last '&'
            if (signDataBuilder.Length > 0)
            {
                signDataBuilder.Length -= 1;
            }
            
            var signData = signDataBuilder.ToString();

            // Compute hash using trimmed secret
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

