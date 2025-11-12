using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class CreatePaymentRequest
    {
        [Required]
        public Guid? ReservationId { get; set; }

        public Guid? ChargingSessionId { get; set; }

        [Required]
        [Range(0.0, 1000000000)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "VND";

        public PaymentMethod Method { get; set; } = PaymentMethod.QrCode;

        public string? Description { get; set; }
    }
}

