using System;
using DataAccessLayer.Data;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,CSStaff")]
    public class DashboardController : ControllerBase
    {
        private readonly EVDbContext _context;

        public DashboardController(EVDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] Guid? stationId = null)
        {
            var now = DateTime.UtcNow;
            var startOfDay = now.Date;

            var querySpots = _context.ChargingSpots.AsQueryable();
            var querySessions = _context.ChargingSessions.AsQueryable();
            var queryReservations = _context.Reservations.AsQueryable();

            if (stationId.HasValue)
            {
                querySpots = querySpots.Where(s => s.ChargingStationId == stationId);
                querySessions = querySessions.Where(s => s.ChargingSpot != null && s.ChargingSpot.ChargingStationId == stationId);
                queryReservations = queryReservations.Where(r => r.ChargingSpot != null && r.ChargingSpot.ChargingStationId == stationId);
            }

            var totalSpots = await querySpots.CountAsync();
            var availableSpots = await querySpots.CountAsync(s => s.Status == SpotStatus.Available);
            var activeSessions = await querySessions.CountAsync(s => s.Status == ChargingSessionStatus.InProgress);
            var reservationsToday = await queryReservations.CountAsync(r => r.ScheduledStartTime >= startOfDay && r.ScheduledStartTime < startOfDay.AddDays(1));

            var energyDeliveredToday = await querySessions
                .Where(s => s.SessionEndTime >= startOfDay && s.SessionEndTime < startOfDay.AddDays(1))
                .SumAsync(s => s.EnergyDeliveredKwh ?? 0);

            var revenueToday = await _context.PaymentTransactions
                .Where(p => p.ProcessedAt >= startOfDay && p.ProcessedAt < startOfDay.AddDays(1) && p.Status == PaymentStatus.Captured)
                .SumAsync(p => p.Amount);

            return Ok(new
            {
                totalSpots,
                availableSpots,
                activeSessions,
                reservationsToday,
                energyDeliveredToday,
                revenueToday
            });
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessionTimeline([FromQuery] Guid? stationId = null)
        {
            var since = DateTime.UtcNow.AddHours(-12);

            var sessions = await _context.ChargingSessions
                .Where(s => s.SessionStartTime >= since)
                .Include(s => s.ChargingSpot)!
                    .ThenInclude(cs => cs.ChargingStation)
                .ToListAsync();

            if (stationId.HasValue)
            {
                sessions = sessions.Where(s => s.ChargingSpot?.ChargingStationId == stationId).ToList();
            }

            var data = sessions.Select(s => new
            {
                s.Id,
                StationName = s.ChargingSpot?.ChargingStation?.Name,
                SpotNumber = s.ChargingSpot?.SpotNumber,
                s.Status,
                s.SessionStartTime,
                s.SessionEndTime,
                s.EnergyDeliveredKwh,
                s.Cost
            });

            return Ok(data);
        }

        [HttpGet("sessions/all")]
        public async Task<IActionResult> GetAllSessions(
            [FromQuery] Guid? stationId = null,
            [FromQuery] ChargingSessionStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.ChargingSessions
                .Include(s => s.ChargingSpot)!
                    .ThenInclude(cs => cs.ChargingStation)
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .AsQueryable();

            if (stationId.HasValue)
            {
                query = query.Where(s => s.ChargingSpot != null && s.ChargingSpot.ChargingStationId == stationId);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(s => s.SessionStartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endDateTime = endDate.Value.Date.AddDays(1);
                query = query.Where(s => s.SessionStartTime < endDateTime);
            }

            var totalCount = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(s => s.SessionStartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = sessions.Select(s => new
            {
                s.Id,
                StationName = s.ChargingSpot?.ChargingStation?.Name,
                StationId = s.ChargingSpot?.ChargingStationId,
                SpotNumber = s.ChargingSpot?.SpotNumber,
                UserName = s.User?.FullName ?? s.User?.Email ?? "N/A",
                VehicleName = s.Vehicle != null ? $"{s.Vehicle.Make} {s.Vehicle.Model}" : null,
                s.Status,
                s.SessionStartTime,
                s.SessionEndTime,
                s.EnergyDeliveredKwh,
                s.EnergyRequestedKwh,
                s.Cost,
                s.PricePerKwh,
                DurationMinutes = s.SessionEndTime.HasValue 
                    ? (int)(s.SessionEndTime.Value - s.SessionStartTime).TotalMinutes 
                    : (int?)(DateTime.UtcNow - s.SessionStartTime).TotalMinutes
            });

            return Ok(new
            {
                data = data,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
    }
}

