using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("packages")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPackages([FromQuery] bool activeOnly = true)
        {
            var packages = await _subscriptionService.GetAllPackagesAsync(activeOnly);
            var packageDTOs = packages.Select((DataAccessLayer.Entities.SubscriptionPackage p) => new SubscriptionPackageDTO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                EnergyKwh = p.EnergyKwh,
                IsActive = p.IsActive,
                ValidFrom = p.ValidFrom,
                ValidTo = p.ValidTo,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return Ok(packageDTOs);
        }

        [HttpGet("packages/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPackageById(Guid id)
        {
            var package = await _subscriptionService.GetPackageByIdAsync(id);
            if (package == null)
                return NotFound(new { message = "Package not found" });

            var dto = new SubscriptionPackageDTO
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                Price = package.Price,
                DurationDays = package.DurationDays,
                EnergyKwh = package.EnergyKwh,
                IsActive = package.IsActive,
                ValidFrom = package.ValidFrom,
                ValidTo = package.ValidTo,
                CreatedAt = package.CreatedAt,
                UpdatedAt = package.UpdatedAt
            };

            return Ok(dto);
        }

        [HttpPost("packages")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePackage([FromBody] CreateSubscriptionPackageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _subscriptionService.CreatePackageAsync(request);
            return CreatedAtAction(nameof(GetPackageById), new { id = created.Id }, new SubscriptionPackageDTO
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description,
                Price = created.Price,
                DurationDays = created.DurationDays,
                EnergyKwh = created.EnergyKwh,
                IsActive = created.IsActive,
                ValidFrom = created.ValidFrom,
                ValidTo = created.ValidTo,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt
            });
        }

        [HttpPut("packages/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePackage(Guid id, [FromBody] UpdateSubscriptionPackageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _subscriptionService.UpdatePackageAsync(id, request);
            if (updated == null)
                return NotFound(new { message = "Package not found" });

            return Ok(new SubscriptionPackageDTO
            {
                Id = updated.Id,
                Name = updated.Name,
                Description = updated.Description,
                Price = updated.Price,
                DurationDays = updated.DurationDays,
                EnergyKwh = updated.EnergyKwh,
                IsActive = updated.IsActive,
                ValidFrom = updated.ValidFrom,
                ValidTo = updated.ValidTo,
                CreatedAt = updated.CreatedAt,
                UpdatedAt = updated.UpdatedAt
            });
        }

        [HttpDelete("packages/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePackage(Guid id)
        {
            var result = await _subscriptionService.DeletePackageAsync(id);
            if (!result)
                return NotFound(new { message = "Package not found" });

            return Ok(new { message = "Package deleted successfully" });
        }

        [HttpPost("purchase")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> PurchaseSubscription([FromBody] PurchaseSubscriptionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = Guid.Parse(User.FindFirst("UserId")?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty)
                return Unauthorized();

            try
            {
                var subscription = await _subscriptionService.PurchaseSubscriptionAsync(
                    userId, 
                    request.SubscriptionPackageId, 
                    request.PaymentMethod);

                var dto = new UserSubscriptionDTO
                {
                    Id = subscription.Id,
                    UserId = subscription.UserId,
                    SubscriptionPackageId = subscription.SubscriptionPackageId,
                    PurchasedAt = subscription.PurchasedAt,
                    ActivatedAt = subscription.ActivatedAt,
                    ExpiresAt = subscription.ExpiresAt,
                    RemainingEnergyKwh = subscription.RemainingEnergyKwh,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt,
                    UpdatedAt = subscription.UpdatedAt
                };

                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-packages")]
        [Authorize(Roles = "EVDriver,Admin")]
        public async Task<IActionResult> GetMyPackages([FromQuery] bool activeOnly = false)
        {
            var userId = Guid.Parse(User.FindFirst("UserId")?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty)
                return Unauthorized();

            var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId, activeOnly);
            var dtos = subscriptions.Select((DataAccessLayer.Entities.UserSubscription s) => new UserSubscriptionDTO
            {
                Id = s.Id,
                UserId = s.UserId,
                SubscriptionPackageId = s.SubscriptionPackageId,
                PackageName = s.SubscriptionPackage?.Name,
                PurchasedAt = s.PurchasedAt,
                ActivatedAt = s.ActivatedAt,
                ExpiresAt = s.ExpiresAt,
                RemainingEnergyKwh = s.RemainingEnergyKwh,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return Ok(dtos);
        }
    }
}

