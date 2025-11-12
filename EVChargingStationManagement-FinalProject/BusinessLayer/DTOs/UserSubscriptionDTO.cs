namespace BusinessLayer.DTOs
{
    public class UserSubscriptionDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SubscriptionPackageId { get; set; }
        public string? PackageName { get; set; }
        public DateTime PurchasedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public decimal RemainingEnergyKwh { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PurchaseSubscriptionRequest
    {
        public Guid SubscriptionPackageId { get; set; }
        public string PaymentMethod { get; set; } = "Wallet";
    }
}

