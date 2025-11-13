using System.Linq;
using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly EVDbContext _context;
        private readonly IChargingSessionService _sessionService;
        private readonly IReservationService _reservationService;

        public PaymentService(EVDbContext context, IChargingSessionService sessionService, IReservationService reservationService)
        {
            _context = context;
            _sessionService = sessionService;
            _reservationService = reservationService;
        }

        public async Task<IEnumerable<PaymentTransaction>> GetPaymentsForUserAsync(Guid userId, int limit = 20)
        {
            return await _context.PaymentTransactions
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<PaymentTransaction?> GetPaymentByIdAsync(Guid id)
        {
            return await _context.PaymentTransactions
                .Include(p => p.Reservation)
                .Include(p => p.ChargingSession)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PaymentTransaction> CreatePaymentAsync(Guid userId, CreatePaymentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var payment = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReservationId = request.ReservationId,
                ChargingSessionId = request.ChargingSessionId,
                Amount = request.Amount,
                Currency = request.Currency,
                Method = request.Method,
                Description = request.Description,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<PaymentTransaction?> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status, string? providerTransactionId = null)
        {
            var payment = await _context.PaymentTransactions
                .Include(p => p.Reservation)
                .Include(p => p.ChargingSession)
                    .ThenInclude(cs => cs.ChargingSpot)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
            
            if (payment == null)
            {
                return null;
            }

            payment.Status = status;
            payment.ProviderTransactionId = providerTransactionId ?? payment.ProviderTransactionId;
            payment.ProcessedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            // Nếu thanh toán thành công và có session đã completed
            if (status == PaymentStatus.Captured && payment.ChargingSession != null)
            {
                var session = payment.ChargingSession;
                
                // Cập nhật reservation status nếu có
                if (payment.Reservation != null && session.Status == DataAccessLayer.Enums.ChargingSessionStatus.Completed)
                {
                    payment.Reservation.Status = DataAccessLayer.Enums.ReservationStatus.Completed;
                    payment.Reservation.UpdatedAt = DateTime.UtcNow;
                }
                
                // Đảm bảo spot được trả về Available nếu session đã completed
                if (session.Status == DataAccessLayer.Enums.ChargingSessionStatus.Completed && session.ChargingSpot != null)
                {
                    session.ChargingSpot.Status = DataAccessLayer.Enums.SpotStatus.Available;
                    session.ChargingSpot.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<PaymentTransaction?> UpdatePaymentMetadataAsync(Guid paymentId, string metadata)
        {
            var payment = await _context.PaymentTransactions.FindAsync(paymentId);
            if (payment == null)
            {
                return null;
            }

            payment.Metadata = metadata;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<MoMoPaymentRequest> PrepareMoMoPaymentAsync(Guid userId, CreateMoMoPaymentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.SessionId == Guid.Empty)
                throw new ArgumentException("SessionId must be provided", nameof(request));

            // Get session
            var session = await _sessionService.GetSessionByIdAsync(request.SessionId);
            if (session == null)
                throw new InvalidOperationException("Charging session not found");

            if (session.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to access this session");

            // Check if session is completed
            if (session.Status != ChargingSessionStatus.Completed)
                throw new InvalidOperationException("Session must be completed before payment");

            // Calculate amount
            decimal amount = request.Amount > 0 ? request.Amount : (session.Cost ?? 0);
            
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than 0. Please provide amount or ensure session has cost.");

            string description = $"Thanh toán cho phiên sạc {session.Id}";

            // Check if payment already exists
            var existingPayments = await GetPaymentsForUserAsync(userId, 100);
            var existingPayment = existingPayments.FirstOrDefault(p => p.ChargingSessionId == request.SessionId);
            
            PaymentTransaction payment = existingPayment ?? await CreatePaymentAsync(userId, new CreatePaymentRequest
            {
                ChargingSessionId = request.SessionId,
                Amount = amount,
                Currency = "VND",
                Method = PaymentMethod.MoMo,
                Description = description
            });

            if (payment == null)
                throw new InvalidOperationException("Failed to create payment transaction");

            if (payment.Amount <= 0)
                throw new ArgumentException($"Invalid payment amount: {payment.Amount}. Amount must be greater than 0.");

            // Prepare MoMo payment request
            var orderId = payment.Id.ToString("N"); // Format Guid without dashes
            var orderInfo = description;
            var amountLong = (long)Math.Round(payment.Amount, 0);

            return new MoMoPaymentRequest
            {
                OrderId = orderId,
                Amount = amountLong,
                OrderInfo = orderInfo
            };
        }
    }
}

