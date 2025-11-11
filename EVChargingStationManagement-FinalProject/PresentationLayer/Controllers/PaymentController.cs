using System.Linq;
using System.Security.Claims;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
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

        public PaymentController(
            IPaymentService paymentService, 
            IRealtimeNotifier notifier,
            IVnPayService vnPayService,
            IChargingSessionService sessionService)
        {
            _paymentService = paymentService;
            _notifier = notifier;
            _vnPayService = vnPayService;
            _sessionService = sessionService;
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

            var payment = new PaymentTransaction
            {
                ReservationId = request.ReservationId,
                ChargingSessionId = request.ChargingSessionId,
                Amount = request.Amount,
                Currency = request.Currency,
                Method = request.Method,
                Description = request.Description
            };

            var created = await _paymentService.CreatePaymentAsync(GetUserId(), payment);
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
                payment = new PaymentTransaction
                {
                    ChargingSessionId = request.SessionId,
                    Amount = request.Amount > 0 ? request.Amount : (session.Cost ?? 0),
                    Currency = "VND",
                    Method = PaymentMethod.Cash,
                    Description = request.Description ?? $"Thanh toán tiền mặt cho phiên sạc {session.Id}",
                    Status = PaymentStatus.Pending
                };

                payment = await _paymentService.CreatePaymentAsync(userId, payment);
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get session to get amount
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
                payment = existingPayment;
            }
            else
            {
                // Create new payment transaction
                payment = new PaymentTransaction
                {
                    ChargingSessionId = request.SessionId,
                    Amount = request.Amount > 0 ? request.Amount : (session.Cost ?? 0),
                    Currency = "VND",
                    Method = PaymentMethod.VNPay,
                    Description = $"Thanh toán cho phiên sạc {session.Id}",
                    Status = PaymentStatus.Pending
                };

                payment = await _paymentService.CreatePaymentAsync(userId, payment);
            }

            // Get client IP
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            // Use provided return URL or default
            var returnUrl = request.ReturnUrl ?? $"{Request.Scheme}://{Request.Host}/Driver/Payment/VnPayReturn?paymentId={payment.Id}";

            // Create VNPay payment URL
            var paymentUrl = _vnPayService.CreatePaymentUrl(payment, returnUrl, ipAddress);

            return Ok(new { PaymentUrl = paymentUrl, PaymentId = payment.Id });
        }

        [HttpPost("vnpay/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayCallback()
        {
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var callbackResult = _vnPayService.ValidateCallback(queryParams);

            if (!callbackResult.Success || string.IsNullOrEmpty(callbackResult.OrderId))
            {
                return BadRequest(new { message = callbackResult.Message ?? "Invalid callback" });
            }

            // Parse payment ID from OrderId (which is the payment transaction ID)
            if (!Guid.TryParse(callbackResult.OrderId, out var paymentId))
            {
                return BadRequest(new { message = "Invalid payment ID" });
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
                        Success = callbackResult.Success,
                        Message = callbackResult.Message,
                        PaymentId = payment.Id,
                        Amount = payment.Amount,
                        Status = payment.Status
                    });
                }
            }

            return Ok(new
            {
                Success = callbackResult.Success,
                Message = callbackResult.Message,
                TransactionId = callbackResult.TransactionNo
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

