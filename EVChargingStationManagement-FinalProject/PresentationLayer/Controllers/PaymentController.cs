using System.Linq;
using System.Security.Claims;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
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
        private readonly IChargingSessionService _sessionService;
        private readonly IReservationService _reservationService;

        public PaymentController(
            IPaymentService paymentService, 
            IRealtimeNotifier notifier,
            IVnPayService vnPayService,
            IChargingSessionService sessionService,
            IReservationService reservationService)
        {
            _paymentService = paymentService;
            _notifier = notifier;
            _vnPayService = vnPayService;
            _sessionService = sessionService;
            _reservationService = reservationService;
        }

        [HttpGet("me")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> GetPaymentsForMe([FromQuery] int limit = 20)
        {
            var userId = GetUserId();
            var payments = await _paymentService.GetPaymentsForUserAsync(userId, limit);
            return Ok(payments.Select(MapToDto));
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
            if (payment.UserId != userId && role != UserRole.Admin.ToString() && role != UserRole.CSStaff.ToString())
            {
                return Forbid();
            }

            return Ok(MapToDto(payment));
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
            return CreatedAtAction(nameof(GetPaymentById), new { id = created.Id }, MapToDto(created));
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

            if (updated.Reservation != null)
            {
                await _notifier.NotifyReservationChangedAsync(updated.Reservation);
            }

            if (updated.ChargingSession != null)
            {
                await _notifier.NotifySessionChangedAsync(updated.ChargingSession);
            }

            return Ok(MapToDto(updated));
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

            // Check if session is completed
            if (session.Status != DataAccessLayer.Enums.ChargingSessionStatus.Completed)
            {
                return BadRequest(new { message = "Session must be completed before payment" });
            }

            // Check if payment already exists
            var existingPayments = await _paymentService.GetPaymentsForUserAsync(userId, 100);
            var existingPayment = existingPayments.FirstOrDefault(p => p.ChargingSessionId == request.SessionId);

            PaymentTransaction payment;
            if (existingPayment != null)
            {
                // If payment already exists and is captured, return it
                if (existingPayment.Status == PaymentStatus.Captured)
                {
                    return Ok(MapToDto(existingPayment));
                }
                // Use existing payment
                payment = existingPayment;
            }
            else
            {
                // Create new payment transaction with Cash method
                var paymentRequest = new CreatePaymentRequest
                {
                    ChargingSessionId = request.SessionId,
                    Amount = request.Amount > 0 ? request.Amount : (session.Cost ?? 0),
                    Currency = "VND",
                    Method = PaymentMethod.Cash,
                    Description = request.Description ?? $"Thanh toán tiền mặt cho phiên sạc {session.Id}"
                };

                payment = await _paymentService.CreatePaymentAsync(userId, paymentRequest);
            }

            // Update payment status to Captured (for cash payment)
            // Note: If payment method is different, it will remain as is, but status will be updated
            payment = await _paymentService.UpdatePaymentStatusAsync(
                payment.Id,
                PaymentStatus.Captured,
                $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}");

            if (payment == null)
            {
                return NotFound();
            }

            // Notify related entities
            if (payment.ChargingSession != null)
            {
                await _notifier.NotifySessionChangedAsync(payment.ChargingSession);
            }

            if (payment.Reservation != null)
            {
                await _notifier.NotifyReservationChangedAsync(payment.Reservation);
            }

            return Ok(MapToDto(payment));
        }

        [HttpPost("vnpay/create")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> CreateVnPayPayment([FromBody] CreateVnPayPaymentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid request data", errors = ModelState });
                }

                var userId = GetUserId();
                PaymentTransaction payment;
                decimal amount = request.Amount;
                string description = "";

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
                    description = $"Thanh toán cho đặt lịch {reservation.Id}";

                // Check if payment already exists
                var existingPayments = await _paymentService.GetPaymentsForUserAsync(userId, 100);
                var existingPayment = existingPayments.FirstOrDefault(p => p.ReservationId == request.ReservationId);
                
                if (existingPayment != null)
                {
                    payment = existingPayment;
                }
                else
                {
                    // Create new payment transaction
                    var paymentRequest = new CreatePaymentRequest
                    {
                        ReservationId = request.ReservationId.Value,
                        Amount = amount,
                        Currency = "VND",
                        Method = PaymentMethod.VNPay,
                        Description = description
                    };

                    payment = await _paymentService.CreatePaymentAsync(userId, paymentRequest);
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

                    // Check if session is completed
                    if (session.Status != DataAccessLayer.Enums.ChargingSessionStatus.Completed)
                    {
                        return BadRequest(new { message = "Session must be completed before payment" });
                    }

                    amount = request.Amount > 0 ? request.Amount : (session.Cost ?? 0);
                    description = $"Thanh toán cho phiên sạc {session.Id}";

                // Check if payment already exists
                var existingPayments = await _paymentService.GetPaymentsForUserAsync(userId, 100);
                var existingPayment = existingPayments.FirstOrDefault(p => p.ChargingSessionId == request.SessionId);
                
                if (existingPayment != null)
                {
                    payment = existingPayment;
                }
                else
                {
                    // Create new payment transaction
                    var paymentRequest = new CreatePaymentRequest
                    {
                        ChargingSessionId = request.SessionId.Value,
                        Amount = amount,
                        Currency = "VND",
                        Method = PaymentMethod.VNPay,
                        Description = description
                    };

                    payment = await _paymentService.CreatePaymentAsync(userId, paymentRequest);
                }
            }
            else
            {
                return BadRequest(new { message = "Either SessionId or ReservationId must be provided" });
            }

                // Get client IP
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                if (ipAddress == "::1")
                {
                    ipAddress = "127.0.0.1";
                }

                // Get ReturnUrl from appsettings.json - Simple approach for demo
                var returnUrl = _vnPayService.GetConfiguredReturnUrl();
                
                // If not configured, use default localhost URL
                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    returnUrl = $"{Request.Scheme}://{Request.Host}/Driver/Payment/VnPayReturn";
                }
                
                // Add paymentId to returnUrl
                if (!returnUrl.Contains("paymentId="))
                {
                    returnUrl += (returnUrl.Contains("?") ? "&" : "?") + $"paymentId={payment.Id}";
                }
                
                Console.WriteLine($"[VNPay] Creating payment URL with ReturnUrl: {returnUrl}");

                // Create VNPay payment URL
                string paymentUrl;
                try
                {
                    paymentUrl = _vnPayService.CreatePaymentUrl(payment, returnUrl, ipAddress);
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    Console.WriteLine($"[VNPay Create] Error creating payment URL: {ex.Message}");
                    Console.WriteLine($"[VNPay Create] Stack trace: {ex.StackTrace}");
                    return StatusCode(500, new { message = "Lỗi khi tạo URL thanh toán VNPay. Vui lòng kiểm tra cấu hình VNPay.", error = ex.Message });
                }

                return Ok(new { 
                    paymentUrl = paymentUrl
                });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"[VNPay Create] Unexpected error: {ex.Message}");
                Console.WriteLine($"[VNPay Create] Stack trace: {ex.StackTrace}");
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
            
            // Log received parameters for debugging
            Console.WriteLine($"[VNPay Callback] Received {queryParams.Count} parameters");
            foreach (var param in queryParams)
            {
                Console.WriteLine($"[VNPay Callback] {param.Key} = {param.Value}");
            }
            
            var callbackResult = _vnPayService.ValidateCallback(queryParams);

            if (!callbackResult.Success || string.IsNullOrEmpty(callbackResult.OrderId))
            {
                // Log signature verification failure
                Console.WriteLine($"[VNPay Callback] Signature verification failed: {callbackResult.Message}");
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

            var newStatus = callbackResult.Success ? PaymentStatus.Captured : PaymentStatus.Failed;
            var updated = await _paymentService.UpdatePaymentStatusAsync(
                paymentId, 
                newStatus, 
                callbackResult.TransactionNo);

            if (updated == null)
            {
                return NotFound();
            }

            // Notify related entities
            if (updated.ChargingSession != null)
            {
                await _notifier.NotifySessionChangedAsync(updated.ChargingSession);
            }

            if (updated.Reservation != null)
            {
                await _notifier.NotifyReservationChangedAsync(updated.Reservation);
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

        private PaymentTransactionDTO MapToDto(PaymentTransaction payment)
        {
            return new PaymentTransactionDTO
            {
                Id = payment.Id,
                UserId = payment.UserId,
                ReservationId = payment.ReservationId,
                ChargingSessionId = payment.ChargingSessionId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Method = payment.Method,
                Status = payment.Status,
                ProviderTransactionId = payment.ProviderTransactionId,
                ProcessedAt = payment.ProcessedAt,
                Description = payment.Description
            };
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId!);
        }
    }
}

