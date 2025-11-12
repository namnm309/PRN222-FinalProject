using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class UpdatePaymentStatusRequest
    {
        [Required]
        public PaymentStatus Status { get; set; }

        public string? ProviderTransactionId { get; set; }
    }
}

