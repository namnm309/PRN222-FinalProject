using BusinessLayer.DTOs;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentTransactionDTO>> GetPaymentsForUserAsync(Guid userId, int limit = 20);
        Task<PaymentTransactionDTO?> GetPaymentByIdAsync(Guid id);
        Task<PaymentTransactionDTO> CreatePaymentAsync(Guid userId, CreatePaymentRequest request);
        Task<PaymentTransactionDTO?> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status, string? providerTransactionId = null);
        Task<PaymentTransactionDTO?> UpdatePaymentMetadataAsync(Guid paymentId, string metadata);
        
        // Method to prepare payment for MoMo/VNPay gateway
        Task<MoMoPaymentRequest> PrepareMoMoPaymentAsync(Guid userId, CreateMoMoPaymentRequest request);
    }
    
    public class MoMoPaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
    }
}

