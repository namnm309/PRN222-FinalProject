using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<SubscriptionPackageDTO>> GetAllPackagesAsync(bool activeOnly = false);
        Task<SubscriptionPackageDTO?> GetPackageByIdAsync(Guid id);
        Task<SubscriptionPackageDTO> CreatePackageAsync(CreateSubscriptionPackageRequest request);
        Task<SubscriptionPackageDTO?> UpdatePackageAsync(Guid id, UpdateSubscriptionPackageRequest request);
        Task<bool> DeletePackageAsync(Guid id);
        Task<UserSubscriptionDTO> PurchaseSubscriptionAsync(Guid userId, Guid packageId, string paymentMethod);
        Task<IEnumerable<UserSubscriptionDTO>> GetUserSubscriptionsAsync(Guid userId, bool activeOnly = false);
        Task<UserSubscriptionDTO?> GetUserSubscriptionByIdAsync(Guid id);
        Task<bool> UseSubscriptionEnergyAsync(Guid subscriptionId, decimal energyKwh);
    }
}

