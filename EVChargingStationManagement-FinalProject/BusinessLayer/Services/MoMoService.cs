using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessLayer.DTOs;
using Microsoft.Extensions.Configuration;

namespace BusinessLayer.Services
{
    public class MoMoService : IMoMoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _partnerCode;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _endpoint;
        private readonly string _redirectUrl;
        private readonly string _ipnUrl;
        private readonly string _requestType;

        public MoMoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            
            _partnerCode = _configuration["MoMo:PartnerCode"] ?? "";
            _accessKey = _configuration["MoMo:AccessKey"] ?? "";
            _secretKey = (_configuration["MoMo:SecretKey"] ?? "").Trim();
            _endpoint = _configuration["MoMo:Endpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
            // Read from config: ReturnUrl and NotifyUrl (matching appsettings.json field names)
            _redirectUrl = _configuration["MoMo:ReturnUrl"] ?? "";
            _ipnUrl = _configuration["MoMo:NotifyUrl"] ?? "";
            _requestType = _configuration["MoMo:RequestType"] ?? "captureMoMoWallet";
            
            // Validate required options
            if (string.IsNullOrWhiteSpace(_partnerCode))
                throw new InvalidOperationException("MoMo PartnerCode is not configured");
            if (string.IsNullOrWhiteSpace(_accessKey))
                throw new InvalidOperationException("MoMo AccessKey is not configured");
            if (string.IsNullOrWhiteSpace(_secretKey))
                throw new InvalidOperationException("MoMo SecretKey is not configured");
            if (string.IsNullOrWhiteSpace(_endpoint))
                throw new InvalidOperationException("MoMo Endpoint is not configured");
        }

        public async Task<string> CreatePaymentUrl(string orderId, long amount, string orderInfo)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("OrderId cannot be null or empty", nameof(orderId));
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than 0", nameof(amount));
            if (string.IsNullOrWhiteSpace(orderInfo))
                throw new ArgumentException("OrderInfo cannot be null or empty", nameof(orderInfo));

            // Generate request ID
            var requestId = Guid.NewGuid().ToString();
            var amountStr = amount.ToString();
            var extraData = "";

            // Validate URLs are not empty
            if (string.IsNullOrWhiteSpace(_redirectUrl))
                throw new InvalidOperationException("MoMo RedirectUrl is not configured");
            if (string.IsNullOrWhiteSpace(_ipnUrl))
                throw new InvalidOperationException("MoMo IpnUrl is not configured");

            // Build raw signature string - MUST be in EXACT order as specified by MoMo
            // Format: partnerCode={partnerCode}&accessKey={accessKey}&requestId={requestId}&amount={amount}&orderId={orderId}&orderInfo={orderInfo}&returnUrl={returnUrl}&notifyUrl={notifyUrl}&extraData={extraData}
            // Note: Values are NOT URL encoded in signature string (only keys and values as-is)
            // CRITICAL: Order must be EXACT - any difference will cause "Bad format request" error
            var rawHash = $"partnerCode={_partnerCode}" +
                         $"&accessKey={_accessKey}" +
                         $"&requestId={requestId}" +
                         $"&amount={amountStr}" +
                         $"&orderId={orderId}" +
                         $"&orderInfo={orderInfo}" +
                         $"&returnUrl={_redirectUrl}" +
                         $"&notifyUrl={_ipnUrl}" +
                         $"&extraData={extraData}";

            // Generate HMACSHA256 signature
            var signature = ComputeHmacSha256(rawHash, _secretKey);

            // Create payment request object - MUST match exact format required by MoMo
            // Format: { accessKey, partnerCode, requestType, notifyUrl, returnUrl, orderId, amount, orderInfo, requestId, extraData, signature }
            // Note: Do NOT include: partnerName, storeId, lang, autoCapture, orderGroupId
            var paymentRequest = new Dictionary<string, object>
            {
                { "accessKey", _accessKey },
                { "partnerCode", _partnerCode },
                { "requestType", _requestType },
                { "notifyUrl", _ipnUrl },
                { "returnUrl", _redirectUrl },
                { "orderId", orderId },
                { "amount", amountStr },
                { "orderInfo", orderInfo },
                { "requestId", requestId },
                { "extraData", extraData },
                { "signature", signature }
            };

            // Log request for debugging
            Console.WriteLine($"[MoMo] ========== Creating Payment URL ==========");
            Console.WriteLine($"[MoMo] Endpoint: {_endpoint}");
            Console.WriteLine($"[MoMo] PartnerCode: {_partnerCode}");
            Console.WriteLine($"[MoMo] AccessKey: {_accessKey}");
            Console.WriteLine($"[MoMo] OrderId: {orderId}");
            Console.WriteLine($"[MoMo] Amount: {amount} VND");
            Console.WriteLine($"[MoMo] OrderInfo: {orderInfo}");
            Console.WriteLine($"[MoMo] RequestId: {requestId}");
            Console.WriteLine($"[MoMo] RedirectUrl: {_redirectUrl}");
            Console.WriteLine($"[MoMo] IpnUrl: {_ipnUrl}");
            Console.WriteLine($"[MoMo] RequestType: {_requestType}");
            Console.WriteLine($"[MoMo] RawHash (for signature): {rawHash}");
            Console.WriteLine($"[MoMo] Signature: {signature}");
            Console.WriteLine($"[MoMo] Request JSON: {JsonSerializer.Serialize(paymentRequest)}");
            Console.WriteLine($"[MoMo] ===========================================");

            try
            {
                // Call MoMo sandbox API
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                // Serialize to JSON string first to ensure proper format
                // Note: MoMo API requires exact field names (camelCase), so we keep Dictionary keys as-is
                var jsonContent = JsonSerializer.Serialize(paymentRequest, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_endpoint, content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[MoMo] Response Status: {response.StatusCode}");
                Console.WriteLine($"[MoMo] Response Content: {responseContent}");

                // Parse response
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Check for HTTP errors
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = responseObject.TryGetProperty("message", out var msg) 
                        ? msg.GetString() ?? "Unknown error" 
                        : "Unknown error";
                    throw new InvalidOperationException($"MoMo API error: {errorMsg}");
                }

                // Check errorCode (MoMo returns errorCode when there's an application-level error)
                if (responseObject.TryGetProperty("errorCode", out var errorCodeElement))
                {
                    var errorCode = errorCodeElement.GetInt32();
                    if (errorCode != 0)
                    {
                        // Map specific error codes to user-friendly messages
                        string userFriendlyMessage = errorCode switch
                        {
                            6 => "Đơn hàng đã tồn tại đang chờ xử lý", // OrderId exists
                            _ => responseObject.TryGetProperty("localMessage", out var localMsgElement)
                                ? localMsgElement.GetString() ?? "Có lỗi xảy ra khi xử lý thanh toán"
                                : "Có lỗi xảy ra khi xử lý thanh toán"
                        };
                        
                        throw new InvalidOperationException(userFriendlyMessage);
                    }
                }

                // Check resultCode (alternative error indicator)
                if (responseObject.TryGetProperty("resultCode", out var resultCodeElement))
                {
                    var code = resultCodeElement.GetInt32();
                    if (code != 0)
                    {
                        var errorMsg = responseObject.TryGetProperty("message", out var msg) 
                            ? msg.GetString() ?? "Unknown error" 
                            : "Unknown error";
                        throw new InvalidOperationException($"MoMo API error - ResultCode: {code}, Message: {errorMsg}");
                    }
                }

                // Extract payUrl
                if (responseObject.TryGetProperty("payUrl", out var payUrlElement))
                {
                    var payUrl = payUrlElement.GetString();
                    if (!string.IsNullOrEmpty(payUrl))
                    {
                        Console.WriteLine($"[MoMo] Payment URL created successfully: {payUrl}");
                        return payUrl;
                    }
                }

                // If we reach here, MoMo didn't return payUrl and no error code was set
                throw new InvalidOperationException("MoMo API did not return payUrl");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[MoMo] HTTP Request Exception: {ex.Message}");
                throw new InvalidOperationException($"Không thể kết nối đến MoMo API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[MoMo] Request Timeout: {ex.Message}");
                throw new InvalidOperationException("MoMo API request timeout", ex);
            }
        }

        public MoMoCallbackResult ValidateCallback(Dictionary<string, string> queryParams)
        {
            var result = new MoMoCallbackResult();

            if (queryParams == null || !queryParams.Any())
            {
                result.Success = false;
                result.Message = "Invalid callback data";
                return result;
            }

            // Extract signature
            var signature = queryParams.ContainsKey("signature") ? queryParams["signature"] : "";
            if (string.IsNullOrEmpty(signature))
            {
                result.Success = false;
                result.Message = "Missing signature";
                return result;
            }

            // Verify signature
            if (!VerifySignature(queryParams, signature))
            {
                result.Success = false;
                result.Message = "Invalid signature";
                return result;
            }

            // Extract data
            result.PartnerCode = queryParams.ContainsKey("partnerCode") ? queryParams["partnerCode"] : null;
            result.OrderId = queryParams.ContainsKey("orderId") ? queryParams["orderId"] : null;
            result.RequestId = queryParams.ContainsKey("requestId") ? queryParams["requestId"] : null;
            result.TransactionNo = queryParams.ContainsKey("transId") ? queryParams["transId"] : null;
            result.ResponseCode = queryParams.ContainsKey("resultCode") ? queryParams["resultCode"] : null;
            result.Message = queryParams.ContainsKey("message") ? queryParams["message"] : null;

            if (queryParams.ContainsKey("amount"))
            {
                if (long.TryParse(queryParams["amount"], out var amount))
                {
                    result.Amount = amount;
                }
            }

            // Check result code (0 = success)
            result.Success = result.ResponseCode == "0";
            if (string.IsNullOrEmpty(result.Message))
            {
                result.Message = result.Success ? "Giao dịch thành công" : GetResponseMessage(result.ResponseCode);
            }

            return result;
        }

        public bool VerifySignature(Dictionary<string, string> data, string signature)
        {
            if (string.IsNullOrEmpty(signature))
                return false;

            // Build raw hash string for callback verification
            // MoMo callback signature includes: accessKey, amount, extraData, message, orderId, orderInfo, orderType, partnerCode, payType, requestId, responseTime, resultCode, transId
            var parts = new List<string>();
            
            if (data.ContainsKey("accessKey")) parts.Add($"accessKey={data["accessKey"]}");
            if (data.ContainsKey("amount")) parts.Add($"amount={data["amount"]}");
            if (data.ContainsKey("extraData")) parts.Add($"extraData={data["extraData"]}");
            if (data.ContainsKey("message")) parts.Add($"message={data["message"]}");
            if (data.ContainsKey("orderId")) parts.Add($"orderId={data["orderId"]}");
            if (data.ContainsKey("orderInfo")) parts.Add($"orderInfo={data["orderInfo"]}");
            if (data.ContainsKey("orderType")) parts.Add($"orderType={data["orderType"]}");
            if (data.ContainsKey("partnerCode")) parts.Add($"partnerCode={data["partnerCode"]}");
            if (data.ContainsKey("payType")) parts.Add($"payType={data["payType"]}");
            if (data.ContainsKey("requestId")) parts.Add($"requestId={data["requestId"]}");
            if (data.ContainsKey("responseTime")) parts.Add($"responseTime={data["responseTime"]}");
            if (data.ContainsKey("resultCode")) parts.Add($"resultCode={data["resultCode"]}");
            if (data.ContainsKey("transId")) parts.Add($"transId={data["transId"]}");

            var rawHash = string.Join("&", parts);
            var computedHash = ComputeHmacSha256(rawHash, _secretKey);

            Console.WriteLine($"[MoMo Verify] RawHash: {rawHash}");
            Console.WriteLine($"[MoMo Verify] Expected: {signature}");
            Console.WriteLine($"[MoMo Verify] Computed: {computedHash}");
            Console.WriteLine($"[MoMo Verify] Match: {computedHash.Equals(signature, StringComparison.OrdinalIgnoreCase)}");

            return computedHash.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeHmacSha256(string data, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
            }
        }

        private string GetResponseMessage(string? responseCode)
        {
            return responseCode switch
            {
                "0" => "Giao dịch thành công",
                "1" => "Giao dịch bị từ chối",
                "2" => "Giao dịch bị lỗi",
                "3" => "Giao dịch đang được xử lý",
                "4" => "Giao dịch đã bị hủy",
                "5" => "Giao dịch đã hết hạn",
                "6" => "Giao dịch không tồn tại",
                "7" => "Giao dịch không hợp lệ",
                "8" => "Giao dịch đã được xử lý",
                "9" => "Giao dịch đang được xử lý",
                "10" => "Giao dịch bị lỗi hệ thống",
                "11" => "Giao dịch bị lỗi kết nối",
                "12" => "Giao dịch bị lỗi xác thực",
                "13" => "Giao dịch bị lỗi dữ liệu",
                "14" => "Giao dịch bị lỗi chữ ký",
                "15" => "Giao dịch bị lỗi số tiền",
                "16" => "Giao dịch bị lỗi đơn hàng",
                "17" => "Giao dịch bị lỗi đối tác",
                "18" => "Giao dịch bị lỗi ngân hàng",
                "19" => "Giao dịch bị lỗi ví",
                "20" => "Giao dịch bị lỗi khác",
                _ => $"Mã lỗi: {responseCode}"
            };
        }
    }
}
