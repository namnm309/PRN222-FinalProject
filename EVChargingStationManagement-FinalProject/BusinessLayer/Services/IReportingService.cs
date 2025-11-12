using BusinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface IReportingService
    {
        Task<StationReportDTO?> GetStationReportAsync(Guid stationId, DateOnly reportDate);
        Task<List<StationReportDTO>> GetStationReportsAsync(Guid stationId, DateOnly startDate, DateOnly endDate);
        Task<RevenueReportDTO> GetRevenueReportAsync(DateOnly startDate, DateOnly endDate, Guid? stationId = null);
        Task<UsageStatisticsDTO> GetUsageStatisticsAsync(DateOnly startDate, DateOnly endDate, Guid? stationId = null);
        Task<StationReport> GenerateDailyReportAsync(Guid stationId, DateOnly reportDate);
    }
}

