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
            return Ok(packages);
        }

        [HttpGet("packages/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPackageById(Guid id)
        {
            var package = await _subscriptionService.GetPackageByIdAsync(id);
            if (package == null)
                return NotFound(new { message = "Package not found" });

            return Ok(package);
        }

        [HttpPost("packages")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePackage([FromBody] CreateSubscriptionPackageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _subscriptionService.CreatePackageAsync(request);
            return CreatedAtAction(nameof(GetPackageById), new { id = created.Id }, created);
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

            return Ok(updated);
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

                return Ok(subscription);
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
            return Ok(subscriptions);
        }
    }
}

