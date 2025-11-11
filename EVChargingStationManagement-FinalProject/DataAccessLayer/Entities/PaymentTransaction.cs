using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_payment_transaction")]
    public class PaymentTransaction : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid? ReservationId { get; set; }

        public Guid? ChargingSessionId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "VND";

        public PaymentMethod Method { get; set; } = PaymentMethod.Wallet;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string? ProviderTransactionId { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string? Description { get; set; }

        public string? Metadata { get; set; }

        // E-wallet and subscription fields
        public Guid? SubscriptionPackageId { get; set; }
        public decimal? WalletBalanceBefore { get; set; }
        public decimal? WalletBalanceAfter { get; set; }
        public string? EWalletProvider { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual Users? User { get; set; }

        [ForeignKey(nameof(ReservationId))]
        public virtual Reservation? Reservation { get; set; }

        [ForeignKey(nameof(ChargingSessionId))]
        public virtual ChargingSession? ChargingSession { get; set; }

        [ForeignKey(nameof(SubscriptionPackageId))]
        public virtual SubscriptionPackage? SubscriptionPackage { get; set; }
    }
}

