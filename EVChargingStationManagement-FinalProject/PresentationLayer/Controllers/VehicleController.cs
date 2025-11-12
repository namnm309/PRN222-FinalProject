using System.Linq;
using System.Security.Claims;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
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
            var dtos = vehicles.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetVehicle(Guid id)
        {
            var userId = GetUserId();
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);

            if (vehicle == null || vehicle.UserVehicles.All(uv => uv.UserId != userId))
            {
                return NotFound();
            }

            return Ok(MapToDto(vehicle));
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
                var vehicle = new Vehicle
                {
                    Make = request.Make,
                    Model = request.Model,
                    ModelYear = request.ModelYear,
                    LicensePlate = request.LicensePlate,
                    VehicleType = request.VehicleType,
                    BatteryCapacityKwh = request.BatteryCapacityKwh,
                    MaxChargingPowerKw = request.MaxChargingPowerKw,
                    Color = request.Color,
                    Notes = request.Notes
                };

                var created = await _vehicleService.CreateVehicleAsync(
                    userId,
                    vehicle,
                    request.IsPrimary,
                    request.Nickname,
                    request.ChargePortLocation);

                return CreatedAtAction(nameof(GetVehicle), new { id = created.Id }, MapToDto(created));
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

            var userId = GetUserId();
            var vehicle = new Vehicle
            {
                Make = request.Make,
                Model = request.Model,
                ModelYear = request.ModelYear,
                LicensePlate = request.LicensePlate,
                VehicleType = request.VehicleType,
                BatteryCapacityKwh = request.BatteryCapacityKwh,
                MaxChargingPowerKw = request.MaxChargingPowerKw,
                Color = request.Color,
                Notes = request.Notes
            };

            var updated = await _vehicleService.UpdateVehicleAsync(
                id,
                vehicle,
                request.IsPrimary,
                request.Nickname,
                request.ChargePortLocation);

            if (updated == null || updated.UserVehicles.All(uv => uv.UserId != userId))
            {
                return NotFound();
            }

            return Ok(MapToDto(updated));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteVehicle(Guid id)
        {
            var userId = GetUserId();
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null || vehicle.UserVehicles.All(uv => uv.UserId != userId))
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
            if (vehicle == null || vehicle.UserVehicles.All(uv => uv.UserId != userId))
            {
                return NotFound();
            }

            await _vehicleService.SetPrimaryVehicleAsync(userId, id);
            return Ok();
        }

        private VehicleDTO MapToDto(Vehicle vehicle)
        {
            var userVehicle = vehicle.UserVehicles.FirstOrDefault();
            return new VehicleDTO
            {
                Id = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                ModelYear = vehicle.ModelYear,
                LicensePlate = vehicle.LicensePlate,
                VehicleType = vehicle.VehicleType,
                BatteryCapacityKwh = vehicle.BatteryCapacityKwh,
                MaxChargingPowerKw = vehicle.MaxChargingPowerKw,
                Color = vehicle.Color,
                Notes = vehicle.Notes,
                IsPrimary = userVehicle?.IsPrimary ?? false,
                Nickname = userVehicle?.Nickname,
                ChargePortLocation = userVehicle?.ChargePortLocation
            };
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userId!);
        }
    }
}

