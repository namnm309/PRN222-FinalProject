using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentTransaction>> GetPaymentsForUserAsync(Guid userId, int limit = 20);
        Task<PaymentTransaction?> GetPaymentByIdAsync(Guid id);
        Task<PaymentTransaction> CreatePaymentAsync(Guid userId, PaymentTransaction payment);
        Task<PaymentTransaction?> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status, string? providerTransactionId = null);
    }
}

