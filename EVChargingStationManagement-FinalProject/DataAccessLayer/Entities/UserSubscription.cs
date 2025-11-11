using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_user_subscription")]
    public class UserSubscription : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid SubscriptionPackageId { get; set; }

        public DateTime PurchasedAt { get; set; }

        public DateTime? ActivatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public decimal RemainingEnergyKwh { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual Users? User { get; set; }

        [ForeignKey(nameof(SubscriptionPackageId))]
        public virtual SubscriptionPackage? SubscriptionPackage { get; set; }
    }
}

