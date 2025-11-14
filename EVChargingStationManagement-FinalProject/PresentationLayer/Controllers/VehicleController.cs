using System.Linq;
using System.Security.Claims;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "EVDriver,Admin")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyVehicles()
        {
            var userId = GetUserId();
            var vehicles = await _vehicleService.GetVehiclesByUserAsync(userId);
            return Ok(vehicles);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetVehicle(Guid id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);

            if (vehicle == null)
            {
                return NotFound();
            }

            return Ok(vehicle);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = ModelState });
            }

            try
            {
                var userId = GetUserId();
                var created = await _vehicleService.CreateVehicleAsync(userId, request);

                return CreatedAtAction(nameof(GetVehicle), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] UpdateVehicleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _vehicleService.UpdateVehicleAsync(id, request);

            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteVehicle(Guid id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            await _vehicleService.DeleteVehicleAsync(id);
            return NoContent();
        }

        [HttpPost("{id:guid}/primary")]
        public async Task<IActionResult> SetPrimaryVehicle(Guid id)
        {
            var userId = GetUserId();
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            await _vehicleService.SetPrimaryVehicleAsync(userId, id);
            return Ok();
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId!);
        }
    }
}

