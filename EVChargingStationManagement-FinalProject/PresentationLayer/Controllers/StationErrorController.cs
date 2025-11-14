using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.DTOs;
using System.Security.Claims;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StationErrorController : ControllerBase
    {
        private readonly IStationErrorService _errorService;

        public StationErrorController(IStationErrorService errorService)
        {
            _errorService = errorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllErrors()
        {
            var errors = await _errorService.GetAllErrorsAsync();
            return Ok(errors);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetErrorById(Guid id)
        {
            var error = await _errorService.GetErrorByIdAsync(id);
            if (error == null)
                return NotFound(new { message = "Error record not found" });

            return Ok(error);
        }

        [HttpGet("station/{stationId}")]
        public async Task<IActionResult> GetErrorsByStationId(Guid stationId)
        {
            var errors = await _errorService.GetErrorsByStationIdAsync(stationId);
            return Ok(errors);
        }

        [HttpGet("spot/{spotId}")]
        public async Task<IActionResult> GetErrorsBySpotId(Guid spotId)
        {
            var errors = await _errorService.GetErrorsBySpotIdAsync(spotId);
            return Ok(errors);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetErrorsByStatus([FromRoute] string status)
        {
            try
            {
                var errors = await _errorService.GetErrorsByStatusStringAsync(status);
                return Ok(errors);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetErrorsByUserId(Guid userId)
        {
            var errors = await _errorService.GetErrorsByUserIdAsync(userId);
            return Ok(errors);
        }

        [HttpPost]
        public async Task<IActionResult> CreateError([FromBody] CreateStationErrorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Get userId from User context if not provided in request
                if (request.ReportedByUserId == Guid.Empty)
                {
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
                    {
                        request.ReportedByUserId = userId;
                    }
                    else
                    {
                        return BadRequest(new { message = "User ID is required" });
                    }
                }

                var createdError = await _errorService.CreateErrorAsync(request);
                return CreatedAtAction(nameof(GetErrorById), new { id = createdError.Id }, createdError);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateError(Guid id, [FromBody] UpdateStationErrorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingError = await _errorService.GetErrorByIdAsync(id);
            if (existingError == null)
                return NotFound(new { message = "Error record not found" });

            // Set default values from existing error if not provided in request
            if (!request.ResolvedByUserId.HasValue && existingError.ResolvedByUserId.HasValue)
            {
                request.ResolvedByUserId = existingError.ResolvedByUserId;
            }
            if (!request.ResolvedAt.HasValue && existingError.ResolvedAt.HasValue)
            {
                request.ResolvedAt = existingError.ResolvedAt;
            }
            if (request.ResolutionNotes == null && existingError.ResolutionNotes != null)
            {
                request.ResolutionNotes = existingError.ResolutionNotes;
            }
            if (request.Severity == null && existingError.Severity != null)
            {
                request.Severity = existingError.Severity;
            }

            var updatedError = await _errorService.UpdateErrorAsync(id, request);
            if (updatedError == null)
                return NotFound(new { message = "Error record not found" });

            return Ok(updatedError);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteError(Guid id)
        {
            var result = await _errorService.DeleteErrorAsync(id);
            if (!result)
                return NotFound(new { message = "Error record not found" });

            return Ok(new { message = "Error record deleted successfully" });
        }
    }
}

