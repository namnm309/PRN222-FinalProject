using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.DTOs;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChargingSessionController : ControllerBase
    {
        private readonly IChargingSessionService _sessionService;

        public ChargingSessionController(IChargingSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSessions()
        {
            var sessions = await _sessionService.GetAllSessionsAsync();
            var sessionDTOs = sessions.Select(s => MapToDTO(s)).ToList();
            return Ok(sessionDTOs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null)
                return NotFound(new { message = "Charging session not found" });

            return Ok(MapToDTO(session));
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetSessionsByUserId(Guid userId)
        {
            var sessions = await _sessionService.GetSessionsByUserIdAsync(userId);
            var sessionDTOs = sessions.Select(s => MapToDTO(s)).ToList();
            return Ok(sessionDTOs);
        }

        [HttpGet("station/{stationId}")]
        public async Task<IActionResult> GetSessionsByStationId(Guid stationId)
        {
            var sessions = await _sessionService.GetSessionsByStationIdAsync(stationId);
            var sessionDTOs = sessions.Select(s => MapToDTO(s)).ToList();
            return Ok(sessionDTOs);
        }

        [HttpGet("spot/{spotId}")]
        public async Task<IActionResult> GetSessionsBySpotId(Guid spotId)
        {
            var sessions = await _sessionService.GetSessionsBySpotIdAsync(spotId);
            var sessionDTOs = sessions.Select(s => MapToDTO(s)).ToList();
            return Ok(sessionDTOs);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetSessionsByStatus(SessionStatus status)
        {
            var sessions = await _sessionService.GetSessionsByStatusAsync(status);
            var sessionDTOs = sessions.Select(s => MapToDTO(s)).ToList();
            return Ok(sessionDTOs);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var sessions = await _sessionService.GetActiveSessionsAsync();
            var sessionDTOs = sessions.Select(s => MapToDTO(s)).ToList();
            return Ok(sessionDTOs);
        }

        [HttpGet("spot/{spotId}/active")]
        public async Task<IActionResult> GetActiveSessionBySpotId(Guid spotId)
        {
            var session = await _sessionService.GetActiveSessionBySpotIdAsync(spotId);
            if (session == null)
                return NotFound(new { message = "No active session found for this spot" });

            return Ok(MapToDTO(session));
        }

        [HttpGet("user/{userId}/active")]
        public async Task<IActionResult> GetActiveSessionByUserId(Guid userId)
        {
            var session = await _sessionService.GetActiveSessionByUserIdAsync(userId);
            if (session == null)
                return NotFound(new { message = "No active session found for this user" });

            return Ok(MapToDTO(session));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CSStaff,EVDriver")]
        public async Task<IActionResult> CreateSession([FromBody] CreateChargingSessionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var session = new ChargingSession
                {
                    UserId = request.UserId,
                    ChargingStationId = request.ChargingStationId,
                    ChargingSpotId = request.ChargingSpotId,
                    TargetSoC = request.TargetSoC,
                    Notes = request.Notes
                };

                var createdSession = await _sessionService.CreateSessionAsync(session);
                return CreatedAtAction(nameof(GetSessionById), new { id = createdSession.Id }, MapToDTO(createdSession));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateChargingSessionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingSession = await _sessionService.GetSessionByIdAsync(id);
            if (existingSession == null)
                return NotFound(new { message = "Charging session not found" });

            var session = new ChargingSession
            {
                Status = request.Status,
                EnergyConsumed = request.EnergyConsumed ?? existingSession.EnergyConsumed,
                TotalCost = request.TotalCost ?? existingSession.TotalCost,
                CurrentSoC = request.CurrentSoC ?? existingSession.CurrentSoC,
                PowerOutput = request.PowerOutput ?? existingSession.PowerOutput,
                PaymentMethod = request.PaymentMethod ?? existingSession.PaymentMethod,
                TransactionId = request.TransactionId ?? existingSession.TransactionId,
                Notes = request.Notes ?? existingSession.Notes
            };

            var updatedSession = await _sessionService.UpdateSessionAsync(id, session);
            if (updatedSession == null)
                return NotFound(new { message = "Charging session not found" });

            return Ok(MapToDTO(updatedSession));
        }

        [HttpPost("{id}/stop")]
        [Authorize(Roles = "Admin,CSStaff,EVDriver")]
        public async Task<IActionResult> StopSession(Guid id, [FromBody] StopChargingSessionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var stoppedSession = await _sessionService.StopSessionAsync(
                    id, 
                    request.EnergyConsumed, 
                    request.TotalCost, 
                    request.PaymentMethod, 
                    request.Notes
                );

                if (stoppedSession == null)
                    return NotFound(new { message = "Charging session not found" });

                return Ok(MapToDTO(stoppedSession));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/pause")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> PauseSession(Guid id, [FromBody] string? notes)
        {
            try
            {
                var pausedSession = await _sessionService.PauseSessionAsync(id, notes);
                if (pausedSession == null)
                    return NotFound(new { message = "Charging session not found" });

                return Ok(MapToDTO(pausedSession));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/resume")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> ResumeSession(Guid id, [FromBody] string? notes)
        {
            try
            {
                var resumedSession = await _sessionService.ResumeSessionAsync(id, notes);
                if (resumedSession == null)
                    return NotFound(new { message = "Charging session not found" });

                return Ok(MapToDTO(resumedSession));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> CancelSession(Guid id, [FromBody] string? reason)
        {
            try
            {
                var cancelledSession = await _sessionService.CancelSessionAsync(id, reason);
                if (cancelledSession == null)
                    return NotFound(new { message = "Charging session not found" });

                return Ok(MapToDTO(cancelledSession));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSession(Guid id)
        {
            var result = await _sessionService.DeleteSessionAsync(id);
            if (!result)
                return NotFound(new { message = "Charging session not found" });

            return Ok(new { message = "Charging session deleted successfully" });
        }

        [HttpGet("spot/{spotId}/can-start")]
        public async Task<IActionResult> CanStartSession(Guid spotId)
        {
            var canStart = await _sessionService.CanStartSessionAsync(spotId);
            return Ok(new { canStart = canStart });
        }

        private ChargingSessionDTO MapToDTO(ChargingSession session)
        {
            return new ChargingSessionDTO
            {
                Id = session.Id,
                UserId = session.UserId,
                UserName = session.User?.Username,
                UserFullName = session.User?.FullName,
                ChargingStationId = session.ChargingStationId,
                ChargingStationName = session.ChargingStation?.Name,
                ChargingSpotId = session.ChargingSpotId,
                ChargingSpotNumber = session.ChargingSpot?.SpotNumber,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                EnergyConsumed = session.EnergyConsumed,
                TotalCost = session.TotalCost,
                Status = session.Status,
                PaymentMethod = session.PaymentMethod,
                TransactionId = session.TransactionId,
                CurrentSoC = session.CurrentSoC,
                TargetSoC = session.TargetSoC,
                PowerOutput = session.PowerOutput,
                Notes = session.Notes,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };
        }
    }
}

