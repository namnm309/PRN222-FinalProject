using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly EVDbContext _context;

        public SubscriptionService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubscriptionPackage>> GetAllPackagesAsync(bool activeOnly = false)
        {
            var query = _context.SubscriptionPackages.AsQueryable();
            
            if (activeOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(p => p.IsActive &&
                    (!p.ValidFrom.HasValue || p.ValidFrom.Value <= now) &&
                    (!p.ValidTo.HasValue || p.ValidTo.Value >= now));
            }

            return await query.OrderBy(p => p.Price).ToListAsync();
        }

        public async Task<SubscriptionPackage?> GetPackageByIdAsync(Guid id)
        {
            return await _context.SubscriptionPackages.FindAsync(id);
        }

        public async Task<SubscriptionPackage> CreatePackageAsync(CreateSubscriptionPackageRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var package = new SubscriptionPackage
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                DurationDays = request.DurationDays,
                EnergyKwh = request.EnergyKwh,
                IsActive = request.IsActive,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SubscriptionPackages.Add(package);
            await _context.SaveChangesAsync();

            return package;
        }

        public async Task<SubscriptionPackage?> UpdatePackageAsync(Guid id, UpdateSubscriptionPackageRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existing = await _context.SubscriptionPackages.FindAsync(id);
            if (existing == null)
                return null;

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.Price = request.Price;
            existing.DurationDays = request.DurationDays;
            existing.EnergyKwh = request.EnergyKwh;
            existing.IsActive = request.IsActive;
            existing.ValidFrom = request.ValidFrom;
            existing.ValidTo = request.ValidTo;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeletePackageAsync(Guid id)
        {
            var package = await _context.SubscriptionPackages.FindAsync(id);
            if (package == null)
                return false;

            _context.SubscriptionPackages.Remove(package);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserSubscription> PurchaseSubscriptionAsync(Guid userId, Guid packageId, string paymentMethod)
        {
            var package = await _context.SubscriptionPackages.FindAsync(packageId);
            if (package == null)
                throw new ArgumentException("Package not found", nameof(packageId));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            var subscription = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionPackageId = packageId,
                PurchasedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(package.DurationDays),
                RemainingEnergyKwh = package.EnergyKwh,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return subscription;
        }

        public async Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(Guid userId, bool activeOnly = false)
        {
            var query = _context.UserSubscriptions
                .Include(us => us.SubscriptionPackage)
                .Where(us => us.UserId == userId);

            if (activeOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(us => us.IsActive &&
                    (!us.ExpiresAt.HasValue || us.ExpiresAt.Value >= now) &&
                    us.RemainingEnergyKwh > 0);
            }

            return await query.OrderByDescending(us => us.PurchasedAt).ToListAsync();
        }

        public async Task<UserSubscription?> GetUserSubscriptionByIdAsync(Guid id)
        {
            return await _context.UserSubscriptions
                .Include(us => us.SubscriptionPackage)
                .FirstOrDefaultAsync(us => us.Id == id);
        }

        public async Task<bool> UseSubscriptionEnergyAsync(Guid subscriptionId, decimal energyKwh)
        {
            var subscription = await _context.UserSubscriptions.FindAsync(subscriptionId);
            if (subscription == null || !subscription.IsActive)
                return false;

            if (subscription.RemainingEnergyKwh < energyKwh)
                return false;

            subscription.RemainingEnergyKwh -= energyKwh;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (subscription.RemainingEnergyKwh <= 0)
            {
                subscription.IsActive = false;
            }

            await _context.SaveChangesAsync();

            return true;
        }
    }
}

