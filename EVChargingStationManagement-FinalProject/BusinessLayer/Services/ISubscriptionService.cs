using BusinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<SubscriptionPackage>> GetAllPackagesAsync(bool activeOnly = false);
        Task<SubscriptionPackage?> GetPackageByIdAsync(Guid id);
        Task<SubscriptionPackage> CreatePackageAsync(SubscriptionPackage package);
        Task<SubscriptionPackage?> UpdatePackageAsync(Guid id, SubscriptionPackage package);
        Task<bool> DeletePackageAsync(Guid id);
        Task<UserSubscription> PurchaseSubscriptionAsync(Guid userId, Guid packageId, string paymentMethod);
        Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(Guid userId, bool activeOnly = false);
        Task<UserSubscription?> GetUserSubscriptionByIdAsync(Guid id);
        Task<bool> UseSubscriptionEnergyAsync(Guid subscriptionId, decimal energyKwh);
    }
}

