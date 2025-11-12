using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class PaymentTransactionDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? ChargingSessionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public string? ProviderTransactionId { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? Description { get; set; }
    }
}

