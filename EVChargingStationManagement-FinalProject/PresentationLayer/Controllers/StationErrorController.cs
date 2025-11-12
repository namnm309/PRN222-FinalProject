using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
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
            var errorDTOs = errors.Select(e => MapToDTO(e)).ToList();
            return Ok(errorDTOs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetErrorById(Guid id)
        {
            var error = await _errorService.GetErrorByIdAsync(id);
            if (error == null)
                return NotFound(new { message = "Error record not found" });

            return Ok(MapToDTO(error));
        }

        [HttpGet("station/{stationId}")]
        public async Task<IActionResult> GetErrorsByStationId(Guid stationId)
        {
            var errors = await _errorService.GetErrorsByStationIdAsync(stationId);
            var errorDTOs = errors.Select(e => MapToDTO(e)).ToList();
            return Ok(errorDTOs);
        }

        [HttpGet("spot/{spotId}")]
        public async Task<IActionResult> GetErrorsBySpotId(Guid spotId)
        {
            var errors = await _errorService.GetErrorsBySpotIdAsync(spotId);
            var errorDTOs = errors.Select(e => MapToDTO(e)).ToList();
            return Ok(errorDTOs);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetErrorsByStatus(ErrorStatus status)
        {
            var errors = await _errorService.GetErrorsByStatusAsync(status);
            var errorDTOs = errors.Select(e => MapToDTO(e)).ToList();
            return Ok(errorDTOs);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetErrorsByUserId(Guid userId)
        {
            var errors = await _errorService.GetErrorsByUserIdAsync(userId);
            var errorDTOs = errors.Select(e => MapToDTO(e)).ToList();
            return Ok(errorDTOs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateError([FromBody] CreateStationErrorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Get userId from User context if not provided in request
                var reportedByUserId = request.ReportedByUserId;
                if (reportedByUserId == Guid.Empty)
                {
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
                    {
                        reportedByUserId = userId;
                    }
                    else
                    {
                        return BadRequest(new { message = "User ID is required" });
                    }
                }

                var error = new StationError
                {
                    ChargingStationId = request.ChargingStationId,
                    ChargingSpotId = request.ChargingSpotId,
                    ReportedByUserId = reportedByUserId,
                    Status = request.Status,
                    ErrorCode = request.ErrorCode,
                    Title = request.Title,
                    Description = request.Description,
                    Severity = request.Severity
                };

                var createdError = await _errorService.CreateErrorAsync(error);
                return CreatedAtAction(nameof(GetErrorById), new { id = createdError.Id }, MapToDTO(createdError));
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

            var error = new StationError
            {
                ResolvedByUserId = request.ResolvedByUserId ?? existingError.ResolvedByUserId,
                Status = request.Status,
                ResolvedAt = request.ResolvedAt ?? existingError.ResolvedAt,
                ResolutionNotes = request.ResolutionNotes ?? existingError.ResolutionNotes,
                Severity = request.Severity ?? existingError.Severity
            };

            var updatedError = await _errorService.UpdateErrorAsync(id, error);
            if (updatedError == null)
                return NotFound(new { message = "Error record not found" });

            return Ok(MapToDTO(updatedError));
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

        private StationErrorDTO MapToDTO(StationError error)
        {
            return new StationErrorDTO
            {
                Id = error.Id,
                ChargingStationId = error.ChargingStationId,
                ChargingStationName = error.ChargingStation?.Name,
                ChargingSpotId = error.ChargingSpotId,
                ChargingSpotNumber = error.ChargingSpot?.SpotNumber,
                ReportedByUserId = error.ReportedByUserId,
                ReportedByUserName = error.ReportedByUser?.FullName,
                ResolvedByUserId = error.ResolvedByUserId,
                ResolvedByUserName = error.ResolvedByUser?.FullName,
                Status = error.Status,
                ErrorCode = error.ErrorCode,
                Title = error.Title,
                Description = error.Description,
                ReportedAt = error.ReportedAt,
                ResolvedAt = error.ResolvedAt,
                ResolutionNotes = error.ResolutionNotes,
                Severity = error.Severity,
                CreatedAt = error.CreatedAt,
                UpdatedAt = error.UpdatedAt
            };
        }
    }
}

