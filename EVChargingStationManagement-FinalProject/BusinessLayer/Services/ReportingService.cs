using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ReportingService : IReportingService
    {
        private readonly EVDbContext _context;

        public ReportingService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<StationReportDTO?> GetStationReportAsync(Guid stationId, DateOnly reportDate)
        {
            var report = await _context.StationReports
                .Include(r => r.ChargingStation)
                .FirstOrDefaultAsync(r => r.ChargingStationId == stationId && r.ReportDate == reportDate);

            if (report == null)
                return null;

            return new StationReportDTO
            {
                Id = report.Id,
                ChargingStationId = report.ChargingStationId,
                StationName = report.ChargingStation?.Name,
                ReportDate = report.ReportDate,
                TotalSessions = report.TotalSessions,
                TotalEnergyDeliveredKwh = report.TotalEnergyDeliveredKwh,
                TotalRevenue = report.TotalRevenue,
                PeakHour = report.PeakHour,
                AverageSessionDurationMinutes = report.AverageSessionDurationMinutes,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }

        public async Task<List<StationReportDTO>> GetStationReportsAsync(Guid stationId, DateOnly startDate, DateOnly endDate)
        {
            var reports = await _context.StationReports
                .Include(r => r.ChargingStation)
                .Where(r => r.ChargingStationId == stationId &&
                           r.ReportDate >= startDate &&
                           r.ReportDate <= endDate)
                .OrderBy(r => r.ReportDate)
                .ToListAsync();

            return reports.Select(r => new StationReportDTO
            {
                Id = r.Id,
                ChargingStationId = r.ChargingStationId,
                StationName = r.ChargingStation?.Name,
                ReportDate = r.ReportDate,
                TotalSessions = r.TotalSessions,
                TotalEnergyDeliveredKwh = r.TotalEnergyDeliveredKwh,
                TotalRevenue = r.TotalRevenue,
                PeakHour = r.PeakHour,
                AverageSessionDurationMinutes = r.AverageSessionDurationMinutes,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();
        }

        public async Task<RevenueReportDTO> GetRevenueReportAsync(DateOnly startDate, DateOnly endDate, Guid? stationId = null)
        {
            var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            var query = _context.PaymentTransactions
                .Include(pt => pt.ChargingSession)
                    .ThenInclude(cs => cs!.ChargingSpot)
                        .ThenInclude(s => s!.ChargingStation)
                .Where(pt => pt.Status == DataAccessLayer.Enums.PaymentStatus.Captured &&
                            pt.ProcessedAt >= startDateTime &&
                            pt.ProcessedAt <= endDateTime);

            if (stationId.HasValue)
            {
                query = query.Where(pt => pt.ChargingSession != null &&
                                         pt.ChargingSession.ChargingSpot != null &&
                                         pt.ChargingSession.ChargingSpot.ChargingStationId == stationId.Value);
            }

            var transactions = await query.ToListAsync();

            var totalRevenue = transactions.Sum(t => t.Amount);
            var totalSessions = transactions.Count(t => t.ChargingSessionId.HasValue);
            var totalEnergy = transactions
                .Where(t => t.ChargingSession != null && t.ChargingSession.EnergyDeliveredKwh.HasValue)
                .Sum(t => t.ChargingSession!.EnergyDeliveredKwh!.Value);

            var dailyRevenues = transactions
                .GroupBy(t => DateOnly.FromDateTime(t.ProcessedAt!.Value))
                .Select(g => new DailyRevenueDTO
                {
                    Date = g.Key,
                    Revenue = g.Sum(t => t.Amount),
                    Sessions = g.Count(t => t.ChargingSessionId.HasValue),
                    EnergyDeliveredKwh = g.Where(t => t.ChargingSession != null && t.ChargingSession.EnergyDeliveredKwh.HasValue)
                                         .Sum(t => t.ChargingSession!.EnergyDeliveredKwh!.Value)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new RevenueReportDTO
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = totalRevenue,
                TotalSessions = totalSessions,
                TotalEnergyDeliveredKwh = totalEnergy,
                DailyRevenues = dailyRevenues
            };
        }

        public async Task<UsageStatisticsDTO> GetUsageStatisticsAsync(DateOnly startDate, DateOnly endDate, Guid? stationId = null)
        {
            var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            var query = _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)
                    .ThenInclude(s => s!.ChargingStation)
                .Where(cs => cs.SessionStartTime >= startDateTime &&
                            cs.SessionStartTime <= endDateTime &&
                            cs.Status == DataAccessLayer.Enums.ChargingSessionStatus.Completed);

            if (stationId.HasValue)
            {
                query = query.Where(cs => cs.ChargingSpot != null &&
                                         cs.ChargingSpot.ChargingStationId == stationId.Value);
            }

            var sessions = await query.ToListAsync();

            var totalSessions = sessions.Count;
            var totalEnergy = sessions
                .Where(s => s.EnergyDeliveredKwh.HasValue)
                .Sum(s => s.EnergyDeliveredKwh!.Value);

            var avgDuration = sessions
                .Where(s => s.SessionEndTime.HasValue)
                .Select(s => (s.SessionEndTime!.Value - s.SessionStartTime).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            var sessionsByHour = sessions
                .GroupBy(s => s.SessionStartTime.Hour)
                .ToDictionary(g => g.Key, g => g.Count());

            var peakHour = sessionsByHour.Any() 
                ? sessionsByHour.OrderByDescending(kvp => kvp.Value).First().Key 
                : (int?)null;

            var station = stationId.HasValue 
                ? await _context.ChargingStations.FindAsync(stationId.Value) 
                : null;

            return new UsageStatisticsDTO
            {
                StationId = stationId,
                StationName = station?.Name,
                StartDate = startDate,
                EndDate = endDate,
                TotalSessions = totalSessions,
                TotalEnergyDeliveredKwh = totalEnergy,
                AverageSessionDurationMinutes = (decimal)avgDuration,
                SessionsByHour = sessionsByHour,
                PeakHour = peakHour
            };
        }

        public async Task<StationReport> GenerateDailyReportAsync(Guid stationId, DateOnly reportDate)
        {
            var startDateTime = DateTime.SpecifyKind(reportDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var endDateTime = DateTime.SpecifyKind(reportDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            var sessions = await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)
                .Where(cs => cs.ChargingSpot != null &&
                            cs.ChargingSpot.ChargingStationId == stationId &&
                            cs.SessionStartTime >= startDateTime &&
                            cs.SessionStartTime <= endDateTime &&
                            cs.Status == DataAccessLayer.Enums.ChargingSessionStatus.Completed)
                .ToListAsync();

            var totalSessions = sessions.Count;
            var totalEnergy = sessions
                .Where(s => s.EnergyDeliveredKwh.HasValue)
                .Sum(s => s.EnergyDeliveredKwh!.Value);

            var totalRevenue = sessions
                .Where(s => s.Cost.HasValue)
                .Sum(s => s.Cost!.Value);

            var avgDuration = sessions
                .Where(s => s.SessionEndTime.HasValue)
                .Select(s => (s.SessionEndTime!.Value - s.SessionStartTime).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            var sessionsByHour = sessions
                .GroupBy(s => s.SessionStartTime.Hour)
                .ToDictionary(g => g.Key, g => g.Count());

            var peakHour = sessionsByHour.Any()
                ? sessionsByHour.OrderByDescending(kvp => kvp.Value).First().Key
                : (int?)null;

            var existingReport = await _context.StationReports
                .FirstOrDefaultAsync(r => r.ChargingStationId == stationId && r.ReportDate == reportDate);

            if (existingReport != null)
            {
                existingReport.TotalSessions = totalSessions;
                existingReport.TotalEnergyDeliveredKwh = totalEnergy;
                existingReport.TotalRevenue = totalRevenue;
                existingReport.PeakHour = peakHour;
                existingReport.AverageSessionDurationMinutes = (decimal)avgDuration;
                existingReport.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existingReport;
            }

            var report = new StationReport
            {
                Id = Guid.NewGuid(),
                ChargingStationId = stationId,
                ReportDate = reportDate,
                TotalSessions = totalSessions,
                TotalEnergyDeliveredKwh = totalEnergy,
                TotalRevenue = totalRevenue,
                PeakHour = peakHour,
                AverageSessionDurationMinutes = (decimal)avgDuration,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.StationReports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }
    }
}

