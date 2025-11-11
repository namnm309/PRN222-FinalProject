using System.Linq;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly EVDbContext _context;

        public PaymentService(EVDbContext context)
        {
            _context = context;
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

        public async Task<PaymentTransaction> CreatePaymentAsync(Guid userId, PaymentTransaction payment)
        {
            payment.Id = Guid.NewGuid();
            payment.UserId = userId;
            payment.Status = PaymentStatus.Pending;
            payment.CreatedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

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
    }
}

