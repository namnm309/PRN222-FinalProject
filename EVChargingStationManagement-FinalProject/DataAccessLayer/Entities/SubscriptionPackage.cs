using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_subscription_package")]
    public class SubscriptionPackage : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public decimal EnergyKwh { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        // Navigation properties
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}

