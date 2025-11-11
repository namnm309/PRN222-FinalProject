using System.Linq;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/stations/{stationId:guid}/amenities")]
    [Authorize(Roles = "Admin,CSStaff")]
    public class StationAmenityController : ControllerBase
    {
        private readonly IStationAmenityService _amenityService;

        public StationAmenityController(IStationAmenityService amenityService)
        {
            _amenityService = amenityService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAmenities(Guid stationId)
        {
            var amenities = await _amenityService.GetAmenitiesByStationAsync(stationId);
            return Ok(amenities.Select(MapToDto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAmenity(Guid stationId, [FromBody] StationAmenityDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var amenity = new StationAmenity
            {
                ChargingStationId = stationId,
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder
            };

            var created = await _amenityService.CreateAmenityAsync(amenity);
            return CreatedAtAction(nameof(GetAmenities), new { stationId }, MapToDto(created));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAmenity(Guid stationId, Guid id, [FromBody] StationAmenityDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var amenity = new StationAmenity
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder
            };

            var updated = await _amenityService.UpdateAmenityAsync(id, amenity);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(updated));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAmenity(Guid stationId, Guid id)
        {
            var deleted = await _amenityService.DeleteAmenityAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        private StationAmenityDTO MapToDto(StationAmenity amenity)
        {
            return new StationAmenityDTO
            {
                Id = amenity.Id,
                ChargingStationId = amenity.ChargingStationId,
                Name = amenity.Name,
                Description = amenity.Description,
                IsActive = amenity.IsActive,
                DisplayOrder = amenity.DisplayOrder
            };
        }
    }
}

