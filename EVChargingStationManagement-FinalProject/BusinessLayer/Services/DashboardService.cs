using DataAccessLayer.Data;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly EVDbContext _context;

        public DashboardService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardOverviewDTO> GetOverviewAsync(Guid? stationId = null)
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

            return new DashboardOverviewDTO
            {
                TotalSpots = totalSpots,
                AvailableSpots = availableSpots,
                ActiveSessions = activeSessions,
                ReservationsToday = reservationsToday,
                EnergyDeliveredToday = energyDeliveredToday,
                RevenueToday = revenueToday
            };
        }

        public async Task<IEnumerable<SessionTimelineDTO>> GetSessionTimelineAsync(Guid? stationId = null)
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

            return sessions.Select(s => new SessionTimelineDTO
            {
                Id = s.Id,
                StationName = s.ChargingSpot?.ChargingStation?.Name,
                SpotNumber = s.ChargingSpot?.SpotNumber,
                Status = s.Status,
                SessionStartTime = s.SessionStartTime,
                SessionEndTime = s.SessionEndTime,
                EnergyDeliveredKwh = s.EnergyDeliveredKwh,
                Cost = s.Cost
            });
        }

        public async Task<DashboardSessionsDTO> GetAllSessionsAsync(
            Guid? stationId = null,
            ChargingSessionStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50)
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
                var startDateTimeUtc = startDate.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                    : startDate.Value.ToUniversalTime();
                query = query.Where(s => s.SessionStartTime >= startDateTimeUtc);
            }

            if (endDate.HasValue)
            {
                var endDateTime = endDate.Value.Date.AddDays(1);
                var endDateTimeUtc = endDateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc)
                    : endDateTime.ToUniversalTime();
                query = query.Where(s => s.SessionStartTime < endDateTimeUtc);
            }

            var totalCount = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(s => s.SessionStartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = sessions.Select(s => new SessionDetailDTO
            {
                Id = s.Id,
                StationName = s.ChargingSpot?.ChargingStation?.Name,
                StationId = s.ChargingSpot?.ChargingStationId,
                SpotNumber = s.ChargingSpot?.SpotNumber,
                UserName = s.User?.FullName ?? s.User?.Email ?? "N/A",
                VehicleName = s.Vehicle != null ? $"{s.Vehicle.Make} {s.Vehicle.Model}" : null,
                Status = s.Status,
                SessionStartTime = s.SessionStartTime,
                SessionEndTime = s.SessionEndTime,
                EnergyDeliveredKwh = s.EnergyDeliveredKwh,
                EnergyRequestedKwh = s.EnergyRequestedKwh,
                Cost = s.Cost,
                PricePerKwh = s.PricePerKwh,
                DurationMinutes = s.SessionEndTime.HasValue
                    ? (int)(s.SessionEndTime.Value - s.SessionStartTime).TotalMinutes
                    : (int?)(DateTime.UtcNow - s.SessionStartTime).TotalMinutes
            });

            return new DashboardSessionsDTO
            {
                Data = data,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}

