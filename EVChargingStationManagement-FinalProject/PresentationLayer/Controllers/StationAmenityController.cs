using System.Linq;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
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
            return Ok(amenities);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAmenity(Guid stationId, [FromBody] CreateStationAmenityRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            request.ChargingStationId = stationId;
            var created = await _amenityService.CreateAmenityAsync(request);
            return CreatedAtAction(nameof(GetAmenities), new { stationId }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAmenity(Guid stationId, Guid id, [FromBody] UpdateStationAmenityRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _amenityService.UpdateAmenityAsync(id, request);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
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
    }
}

