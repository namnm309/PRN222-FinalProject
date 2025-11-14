using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IRealtimeNotifier _notifier;
        private readonly IVnPayService _vnPayService;
        private readonly IMoMoService _moMoService;
        private readonly IChargingSessionService _sessionService;
        private readonly IReservationService _reservationService;

        public PaymentController(
            IPaymentService paymentService, 
            IRealtimeNotifier notifier,
            IVnPayService vnPayService,
            IMoMoService moMoService,
            IChargingSessionService sessionService,
            IReservationService reservationService)
        {
            _paymentService = paymentService;
            _notifier = notifier;
            _vnPayService = vnPayService;
            _moMoService = moMoService;
            _sessionService = sessionService;
            _reservationService = reservationService;
        }

        [HttpGet("me")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> GetPaymentsForMe([FromQuery] int limit = 20)
        {
            var userId = GetUserId();
            var payments = await _paymentService.GetPaymentsForUserAsync(userId, limit);
            return Ok(payments);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (payment.UserId != userId && role != "Admin" && role != "CSStaff")
            {
                return Forbid();
            }

            return Ok(payment);
        }

        [HttpPost]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paymentRequest = new CreatePaymentRequest
            {
                ReservationId = request.ReservationId,
                ChargingSessionId = request.ChargingSessionId,
                Amount = request.Amount,
                Currency = request.Currency,
                Method = request.Method,
                Description = request.Description
            };

            var created = await _paymentService.CreatePaymentAsync(GetUserId(), paymentRequest);
            return CreatedAtAction(nameof(GetPaymentById), new { id = created.Id }, created);
        }

        [HttpPost("{id:guid}/status")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _paymentService.UpdatePaymentStatusAsync(id, request.Status, request.ProviderTransactionId);
            if (updated == null)
            {
                return NotFound();
            }

            // Notify related entities if they exist
            if (updated.ReservationId.HasValue)
            {
                var reservation = await _reservationService.GetReservationByIdAsync(updated.ReservationId.Value);
                if (reservation != null)
                {
                    await _notifier.NotifyReservationChangedAsync(reservation);
                }
            }

            if (updated.ChargingSessionId.HasValue)
            {
                var session = await _sessionService.GetSessionByIdAsync(updated.ChargingSessionId.Value);
                if (session != null)
                {
                    await _notifier.NotifySessionChangedAsync(session);
                }
            }

            return Ok(updated);
        }

        [HttpPost("cash")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> CreateCashPayment([FromBody] CreateCashPaymentRequest request)
        {
            if (request == null || request.SessionId == Guid.Empty)
            {
                return BadRequest(new { message = "Session ID is required" });
            }

            var session = await _sessionService.GetSessionByIdAsync(request.SessionId);
            if (session == null)
            {
                return NotFound(new { message = "Charging session not found" });
            }

            var userId = GetUserId();
            if (session.UserId != userId)
            {
                return Forbid();
            }

            // Check if session is completed - validation is done in service layer

            // Check if payment already exists
            var existingPayments = await _paymentService.GetPaymentsForUserAsync(userId, 100);
            var existingPayment = existingPayments.FirstOrDefault(p => p.ChargingSessionId == request.SessionId);

            // Get enum types using reflection
            var statusType = typeof(PaymentTransactionDTO).GetProperty("Status")!.PropertyType;
            var methodType = typeof(PaymentTransactionDTO).GetProperty("Method")!.PropertyType;
            var capturedStatus = Enum.Parse(statusType, "Captured", true);
            var cashMethod = Enum.Parse(methodType, "Cash", true);

            // If payment already exists and is captured, return it
            if (existingPayment != null && existingPayment.Status.ToString() == "Captured")
            {
                return Ok(existingPayment);
            }

            // Create or reuse payment transaction with Cash method
            var payment = existingPayment ?? await _paymentService.CreatePaymentAsync(userId, new CreatePaymentRequest
                {
                    ChargingSessionId = request.SessionId,
                    Amount = request.Amount > 0 ? request.Amount : (session.Cost ?? 0),
                    Currency = "VND",
                    Method = (dynamic)cashMethod,
                Description = request.Description ?? $"Thanh toán tiền mặt cho phiên sạc {session.Id}"
            });

            // Update payment status to Captured (for cash payment)
            // Note: If payment method is different, it will remain as is, but status will be updated
            payment = await _paymentService.UpdatePaymentStatusAsync(
                payment.Id,
                (dynamic)capturedStatus,
                $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}");

            if (payment == null)
            {
                return NotFound();
            }

            // Notify related entities
            if (payment.ChargingSessionId.HasValue)
            {
                var sessionEntity = await _sessionService.GetSessionByIdAsync(payment.ChargingSessionId.Value);
                if (sessionEntity != null)
                {
                    await _notifier.NotifySessionChangedAsync(sessionEntity);
                }
            }

            if (payment.ReservationId.HasValue)
            {
                var reservationEntity = await _reservationService.GetReservationByIdAsync(payment.ReservationId.Value);
                if (reservationEntity != null)
                {
                    await _notifier.NotifyReservationChangedAsync(reservationEntity);
                }
            }

            return Ok(payment);
        }

        [HttpPost("vnpay/create")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> CreateVnPayPayment([FromBody] CreateVnPayPaymentRequest request)
        {
            try
            {
                // Validate request is not null
                if (request == null)
                {
                    return BadRequest(new { message = "Request body cannot be null" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid request data", errors = ModelState });
                }

                // Validate that either SessionId or ReservationId is provided
                if (!request.SessionId.HasValue && !request.ReservationId.HasValue)
                {
                    return BadRequest(new { message = "Either SessionId or ReservationId must be provided" });
            }

            var userId = GetUserId();
            decimal amount = request.Amount;
            string description = "";
                var payment = (PaymentTransactionDTO?)null;

            // Handle reservation payment
            if (request.ReservationId.HasValue)
            {
                var reservation = await _reservationService.GetReservationByIdAsync(request.ReservationId.Value);
                if (reservation == null)
                {
                    return NotFound(new { message = "Reservation not found" });
                }

                if (reservation.UserId != userId)
                {
                    return Forbid();
                }

                amount = request.Amount > 0 ? request.Amount : (reservation.EstimatedCost ?? 0);
                    
                    // Validate amount after getting from reservation
                    if (amount <= 0)
                    {
                        return BadRequest(new { message = "Amount must be greater than 0. Please provide amount or ensure reservation has estimated cost." });
                    }
                    
                description = $"Thanh toán cho đặt lịch {reservation.Id}";

                // Check if payment already exists
                var existingPayments = await _paymentService.GetPaymentsForUserAsync(userId, 100);
                var existingPayment = existingPayments.FirstOrDefault(p => p.ReservationId == request.ReservationId);
                
                // Get enum type using reflection
                var methodType = typeof(PaymentTransactionDTO).GetProperty("Method")!.PropertyType;
                var vnpayMethod = Enum.Parse(methodType, "VNPay", true);
                
                payment = existingPayment ?? await _paymentService.CreatePaymentAsync(userId, new CreatePaymentRequest
                    {
                        ReservationId = request.ReservationId.Value,
                        Amount = amount,
                        Currency = "VND",
                        Method = (dynamic)vnpayMethod,
                    Description = description
                });

                // Validate payment was created successfully
                if (payment == null)
                {
                    return StatusCode(500, new { message = "Failed to create payment transaction" });
                }
            }
            // Handle session payment
            else if (request.SessionId.HasValue)
            {
                var session = await _sessionService.GetSessionByIdAsync(request.SessionId.Value);
                if (session == null)
                {
                    return NotFound(new { message = "Charging session not found" });
                }

                if (session.UserId != userId)
                {
                    return Forbid();
                }

                // Check if session is completed - validation should be in service layer
                // For now, keeping this check but ideally should be moved to service

                amount = request.Amount > 0 ? request.Amount : (session.Cost ?? 0);
                
                // Validate amount after getting from session
                if (amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be greater than 0. Please provide amount or ensure session has cost." });
                }
                
                description = $"Thanh toán cho phiên sạc {session.Id}";

                // Check if payment already exists
                var existingPayments = await _paymentService.GetPaymentsForUserAsync(userId, 100);
                var existingPayment = existingPayments.FirstOrDefault(p => p.ChargingSessionId == request.SessionId);
                
                // Get enum type using reflection
                var methodType = typeof(PaymentTransactionDTO).GetProperty("Method")!.PropertyType;
                var vnpayMethod = Enum.Parse(methodType, "VNPay", true);
                
                payment = existingPayment ?? await _paymentService.CreatePaymentAsync(userId, new CreatePaymentRequest
                    {
                        ChargingSessionId = request.SessionId.Value,
                        Amount = amount,
                        Currency = "VND",
                        Method = (dynamic)vnpayMethod,
                    Description = description
                });

                // Validate payment was created successfully
                if (payment == null)
                {
                    return StatusCode(500, new { message = "Failed to create payment transaction" });
                }
            }
            else
            {
                return BadRequest(new { message = "Either SessionId or ReservationId must be provided" });
            }

            // Validate payment is not null before proceeding
            if (payment == null)
                {
                    return StatusCode(500, new { message = "Payment transaction is null. Cannot create VNPay payment URL." });
                }

                // Validate payment amount one more time
                if (payment.Amount <= 0)
                {
                    return BadRequest(new { message = $"Invalid payment amount: {payment.Amount}. Amount must be greater than 0." });
            }

            // Get client IP
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

                // Get ReturnUrl from appsettings.json
                // For UI testing: VNPay rejects localhost (error 72), so we use valid domain format
                // VNPay only checks URL format, not if domain actually exists - this allows UI testing
                var returnUrl = _vnPayService.GetConfiguredReturnUrl();
                
                // If not configured, use valid domain format for UI testing
                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    returnUrl = "https://example.com/vnpay/return";
                }
                
                // Convert localhost to valid domain format if needed (for UI testing)
                // This prevents error 72 while still allowing UI testing
                if (returnUrl.Contains("localhost") || returnUrl.Contains("127.0.0.1") || returnUrl.Contains("::1"))
                {
                    returnUrl = returnUrl.Replace("localhost", "example.com")
                                       .Replace("127.0.0.1", "example.com")
                                       .Replace("::1", "example.com");
                }
                
                // Add paymentId to returnUrl
                if (!returnUrl.Contains("paymentId="))
                {
                returnUrl += (returnUrl.Contains("?") ? "&" : "?") + $"paymentId={payment.Id}";
            }

                // Create VNPay payment URL with comprehensive error handling
                string paymentUrl;
                try
                {
                    paymentUrl = _vnPayService.CreatePaymentUrl(payment, returnUrl, ipAddress);
                }
                catch (ArgumentNullException ex)
                {
                    return BadRequest(new { message = ex.Message, error = "Invalid payment data" });
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message, error = "Invalid payment parameters" });
                }
                catch (InvalidOperationException ex)
                {
                    // Check if it's a configuration error
                    if (ex.Message.Contains("not configured") || ex.Message.Contains("TmnCode") || ex.Message.Contains("HashSecret"))
                    {
                        return StatusCode(500, new { 
                            message = "Lỗi cấu hình VNPay. Vui lòng kiểm tra appsettings.json.", 
                            error = ex.Message,
                            hint = "Đảm bảo các trường VNPay:TmnCode, HashSecret, Url đã được cấu hình đúng."
                        });
                    }
                    
                    return StatusCode(500, new { 
                        message = "Lỗi khi tạo URL thanh toán VNPay.", 
                        error = ex.Message 
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { 
                        message = "Có lỗi xảy ra khi tạo thanh toán VNPay", 
                        error = ex.Message,
                        errorType = ex.GetType().Name
                    });
                }

            return Ok(new { 
                    paymentUrl = paymentUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo thanh toán VNPay", error = ex.Message });
            }
        }

        [HttpPost("vnpay/callback")]
        [HttpGet("vnpay/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayCallback()
        {
            // VNPay sends callback as GET or POST with query parameters
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            
            var callbackResult = _vnPayService.ValidateCallback(queryParams);

            if (!callbackResult.Success || string.IsNullOrEmpty(callbackResult.OrderId))
            {
                // Return error response but still acknowledge receipt to VNPay
                return BadRequest(new { RspCode = "97", Message = callbackResult.Message ?? "Invalid callback" });
            }

            // Parse payment ID from OrderId (which is the payment transaction ID)
            // VNPay returns TxnRef which is the payment ID (Guid format without dashes)
            Guid paymentId;
            if (string.IsNullOrEmpty(callbackResult.OrderId))
            {
                return BadRequest(new { message = "Missing order ID in callback" });
            }
            
            // Try parsing as Guid with dashes first, then without dashes
            if (!Guid.TryParse(callbackResult.OrderId, out paymentId))
            {
                // Try parsing as Guid without dashes (format "N")
                if (callbackResult.OrderId.Length == 32 && Guid.TryParseExact(callbackResult.OrderId, "N", out paymentId))
                {
                    // Successfully parsed
                }
                else
                {
                    return BadRequest(new { message = $"Invalid payment ID format: {callbackResult.OrderId}" });
                }
            }

            // Update payment status
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            // Store responseCode in Metadata for UI mapping
            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                responseCode = callbackResult.ResponseCode,
                message = callbackResult.Message,
                transactionNo = callbackResult.TransactionNo,
                vnpayTransactionId = callbackResult.TransactionId,
                updatedAt = DateTime.UtcNow
            });

            // Get enum type using reflection
            var statusType = typeof(PaymentTransactionDTO).GetProperty("Status")!.PropertyType;
            var capturedStatus = Enum.Parse(statusType, "Captured", true);
            var failedStatus = Enum.Parse(statusType, "Failed", true);
            var newStatus = callbackResult.Success ? capturedStatus : failedStatus;
            
            var updated = await _paymentService.UpdatePaymentStatusAsync(
                paymentId, 
                (dynamic)newStatus, 
                callbackResult.TransactionNo);

            if (updated == null)
            {
                return NotFound();
            }

            // Update metadata with responseCode
            await _paymentService.UpdatePaymentMetadataAsync(paymentId, metadata);

            // Notify related entities
            if (updated.ChargingSessionId.HasValue)
            {
                var sessionEntity = await _sessionService.GetSessionByIdAsync(updated.ChargingSessionId.Value);
                if (sessionEntity != null)
                {
                    await _notifier.NotifySessionChangedAsync(sessionEntity);
                }
            }

            if (updated.ReservationId.HasValue)
            {
                var reservationEntity = await _reservationService.GetReservationByIdAsync(updated.ReservationId.Value);
                if (reservationEntity != null)
                {
                    await _notifier.NotifyReservationChangedAsync(reservationEntity);
                }
            }

            // Return success response for VNPay IPN
            return Ok(new { RspCode = "00", Message = "Success" });
        }

        [HttpGet("vnpay/return")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> VnPayReturn([FromQuery] Guid? paymentId)
        {
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var callbackResult = _vnPayService.ValidateCallback(queryParams);

            if (paymentId.HasValue)
            {
                var payment = await _paymentService.GetPaymentByIdAsync(paymentId.Value);
                if (payment != null)
                {
                    return Ok(new
                    {
                        success = callbackResult.Success,
                        Success = callbackResult.Success, // Keep for backward compatibility
                        message = callbackResult.Message,
                        Message = callbackResult.Message, // Keep for backward compatibility
                        paymentId = payment.Id,
                        PaymentId = payment.Id, // Keep for backward compatibility
                        amount = payment.Amount,
                        Amount = payment.Amount, // Keep for backward compatibility
                        status = payment.Status.ToString(),
                        Status = payment.Status // Keep for backward compatibility
                    });
                }
            }

            return Ok(new
            {
                success = callbackResult.Success,
                Success = callbackResult.Success, // Keep for backward compatibility
                message = callbackResult.Message,
                Message = callbackResult.Message, // Keep for backward compatibility
                transactionId = callbackResult.TransactionNo,
                TransactionId = callbackResult.TransactionNo // Keep for backward compatibility
            });
        }

        [HttpPost("momo/create")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> CreateMoMoPayment([FromBody] CreateMoMoPaymentRequest request)
        {
            try
            {
                // Log received request for debugging
                Console.WriteLine($"[MoMo Create] ========== START ==========");
                Console.WriteLine($"[MoMo Create] Received request: SessionId={request?.SessionId}, Amount={request?.Amount}");
                
                // Validate request
                if (request == null)
                {
                    Console.WriteLine("[MoMo Create] ERROR: Request is null");
                    return BadRequest(new { message = "Request body cannot be null" });
                }

                // Log ModelState errors if any
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) });
                    Console.WriteLine($"[MoMo Create] ERROR: ModelState is invalid: {JsonSerializer.Serialize(errors)}");
                    return BadRequest(new { message = "Invalid request data", errors = ModelState });
                }

                var userId = GetUserId();
                Console.WriteLine($"[MoMo Create] UserId: {userId}");

                // Prepare payment through business layer
                BusinessLayer.Services.MoMoPaymentRequest momoRequest;
                try
                {
                    Console.WriteLine("[MoMo Create] Preparing payment...");
                    momoRequest = await _paymentService.PrepareMoMoPaymentAsync(userId, request);
                    Console.WriteLine($"[MoMo Create] Payment prepared: OrderId={momoRequest.OrderId}, Amount={momoRequest.Amount}, OrderInfo={momoRequest.OrderInfo}");
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine($"[MoMo Create] ERROR (ArgumentNull): {ex.Message}");
                    Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                    return BadRequest(new { message = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"[MoMo Create] ERROR (Argument): {ex.Message}");
                    Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                    return BadRequest(new { message = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"[MoMo Create] ERROR (InvalidOperation): {ex.Message}");
                    Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                    return NotFound(new { message = ex.Message });
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"[MoMo Create] ERROR (Unauthorized): {ex.Message}");
                    Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                    return Forbid();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MoMo Create] ERROR preparing payment: {ex.Message}");
                    Console.WriteLine($"[MoMo Create] Exception Type: {ex.GetType().Name}");
                    Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                    return StatusCode(500, new { message = ex.Message });
                }

                // Call MoMoService to create payment URL
                string payUrl;
                try
                {
                    Console.WriteLine("[MoMo Create] Calling MoMoService.CreatePaymentUrl...");
                    payUrl = await _moMoService.CreatePaymentUrl(momoRequest.OrderId, momoRequest.Amount, momoRequest.OrderInfo);
                    Console.WriteLine($"[MoMo Create] Payment URL created successfully: {payUrl}");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"[MoMo Create] ERROR (Argument in MoMoService): {ex.Message}");
                    Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                    return BadRequest(new { message = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"[MoMo Create] ERROR (InvalidOperation in MoMoService): {ex.Message}");
                    Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                    return StatusCode(500, new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MoMo Create] ERROR in MoMoService: {ex.Message}");
                    Console.WriteLine($"[MoMo Create] Exception Type: {ex.GetType().Name}");
                    Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                    return StatusCode(500, new { message = ex.Message });
                }

                // Return payUrl
                Console.WriteLine($"[MoMo Create] ========== SUCCESS ==========");
                return Ok(new { paymentUrl = payUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MoMo Create] ========== UNEXPECTED ERROR ==========");
                Console.WriteLine($"[MoMo Create] Message: {ex.Message}");
                Console.WriteLine($"[MoMo Create] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[MoMo Create] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[MoMo Create] Inner Exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("momo/callback")]
        [HttpGet("momo/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> MoMoCallback()
        {
            // MoMo sends callback as GET or POST with query parameters
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            
            // Log received parameters for debugging
            Console.WriteLine($"[MoMo Callback] Received {queryParams.Count} parameters");
            foreach (var param in queryParams)
            {
                Console.WriteLine($"[MoMo Callback] {param.Key} = {param.Value}");
            }
            
            var callbackResult = _moMoService.ValidateCallback(queryParams);

            if (!callbackResult.Success || string.IsNullOrEmpty(callbackResult.OrderId))
            {
                // Log signature verification failure
                Console.WriteLine($"[MoMo Callback] Signature verification failed: {callbackResult.Message}");
                // Return error response but still acknowledge receipt to MoMo
                return BadRequest(new { resultCode = "1", message = callbackResult.Message ?? "Invalid callback" });
            }

            // Parse payment ID from OrderId (which is the payment transaction ID)
            // MoMo returns orderId which is the payment ID (Guid format without dashes)
            Guid paymentId;
            if (string.IsNullOrEmpty(callbackResult.OrderId))
            {
                return BadRequest(new { message = "Missing order ID in callback" });
            }
            
            // Try parsing as Guid without dashes (format "N")
            if (callbackResult.OrderId.Length == 32 && Guid.TryParseExact(callbackResult.OrderId, "N", out paymentId))
            {
                // Successfully parsed
            }
            else if (!Guid.TryParse(callbackResult.OrderId, out paymentId))
            {
                return BadRequest(new { message = $"Invalid payment ID format: {callbackResult.OrderId}" });
            }

            // Update payment status
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            // Get enum type using reflection
            var statusType = typeof(PaymentTransactionDTO).GetProperty("Status")!.PropertyType;
            var capturedStatus = Enum.Parse(statusType, "Captured", true);
            var failedStatus = Enum.Parse(statusType, "Failed", true);
            var newStatus = callbackResult.Success ? capturedStatus : failedStatus;
            
            var updated = await _paymentService.UpdatePaymentStatusAsync(
                paymentId, 
                (dynamic)newStatus, 
                callbackResult.TransactionNo);

            if (updated == null)
            {
                return NotFound();
            }

            // Notify related entities
            if (updated.ChargingSessionId.HasValue)
            {
                var sessionEntity = await _sessionService.GetSessionByIdAsync(updated.ChargingSessionId.Value);
                if (sessionEntity != null)
                {
                    await _notifier.NotifySessionChangedAsync(sessionEntity);
                }
            }

            if (updated.ReservationId.HasValue)
            {
                var reservationEntity = await _reservationService.GetReservationByIdAsync(updated.ReservationId.Value);
                if (reservationEntity != null)
                {
                    await _notifier.NotifyReservationChangedAsync(reservationEntity);
                }
            }

            // Return success response for MoMo IPN
            return Ok(new { resultCode = "0", message = "Success" });
        }

        [HttpGet("momo/return")]
        [AllowAnonymous]
        public async Task<IActionResult> MoMoReturn()
        {
            try
            {
                // Lấy query parameters từ MoMo
                var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
                
                // Log received parameters
                Console.WriteLine($"[MoMo Return] Received {queryParams.Count} parameters");
                foreach (var param in queryParams)
                {
                    Console.WriteLine($"[MoMo Return] {param.Key} = {param.Value}");
                }

                // Xác thực signature
                var callbackResult = _moMoService.ValidateCallback(queryParams);

                if (!callbackResult.Success)
                {
                    return Ok(new
                    {
                        success = false,
                        message = callbackResult.Message ?? "Invalid signature"
                    });
                }

                // Update payment status nếu có OrderId
                if (!string.IsNullOrEmpty(callbackResult.OrderId))
                {
                    // Parse OrderId (có thể là Guid format "N" - không có dashes)
                    Guid paymentId;
                    bool isValidOrderId = false;
                    
                    if (callbackResult.OrderId.Length == 32 && Guid.TryParseExact(callbackResult.OrderId, "N", out paymentId))
                    {
                        isValidOrderId = true;
                    }
                    else if (Guid.TryParse(callbackResult.OrderId, out paymentId))
                    {
                        isValidOrderId = true;
                    }
                    else
                    {
                        Console.WriteLine($"[MoMo Return] Invalid OrderId format: {callbackResult.OrderId}");
                    }

                    // Try to update payment status if payment exists
                    if (isValidOrderId)
                    {
                        try
                        {
                            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
                            if (payment != null)
                            {
                                // Get enum type using reflection
                                var statusType = typeof(PaymentTransactionDTO).GetProperty("Status")!.PropertyType;
                                var capturedStatus = Enum.Parse(statusType, "Captured", true);
                                var failedStatus = Enum.Parse(statusType, "Failed", true);
                                var newStatus = callbackResult.Success ? capturedStatus : failedStatus;
                                
                                await _paymentService.UpdatePaymentStatusAsync(
                                    paymentId,
                                    (dynamic)newStatus,
                                    callbackResult.TransactionNo);

                                // Notify related entities
                                var updatedPayment = await _paymentService.GetPaymentByIdAsync(paymentId);
                                if (updatedPayment?.ChargingSessionId.HasValue == true)
                                {
                                    var sessionEntity = await _sessionService.GetSessionByIdAsync(updatedPayment.ChargingSessionId.Value);
                                    if (sessionEntity != null)
                                    {
                                        await _notifier.NotifySessionChangedAsync(sessionEntity);
                                    }
                                }

                                if (updatedPayment?.ReservationId.HasValue == true)
                                {
                                    var reservationEntity = await _reservationService.GetReservationByIdAsync(updatedPayment.ReservationId.Value);
                                    if (reservationEntity != null)
                                    {
                                        await _notifier.NotifyReservationChangedAsync(reservationEntity);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[MoMo Return] Error updating payment: {ex.Message}");
                            // Continue to return result even if update fails
                        }
                    }
                }

                // Trả JSON kết quả cho FE
                return Ok(new
                {
                    success = callbackResult.Success,
                    message = callbackResult.Message,
                    orderId = callbackResult.OrderId,
                    transactionNo = callbackResult.TransactionNo,
                    amount = callbackResult.Amount,
                    responseCode = callbackResult.ResponseCode
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MoMo Return] Error: {ex.Message}");
                return Ok(new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi xử lý kết quả thanh toán"
                });
            }
        }


        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId!);
        }
    }
}

