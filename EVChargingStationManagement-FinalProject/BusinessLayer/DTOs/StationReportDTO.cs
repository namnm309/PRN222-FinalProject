namespace BusinessLayer.DTOs
{
    public class StationReportDTO
    {
        public Guid Id { get; set; }
        public Guid ChargingStationId { get; set; }
        public string? StationName { get; set; }
        public DateOnly ReportDate { get; set; }
        public int TotalSessions { get; set; }
        public decimal TotalEnergyDeliveredKwh { get; set; }
        public decimal TotalRevenue { get; set; }
        public int? PeakHour { get; set; }
        public decimal? AverageSessionDurationMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RevenueReportDTO
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalSessions { get; set; }
        public decimal TotalEnergyDeliveredKwh { get; set; }
        public List<DailyRevenueDTO> DailyRevenues { get; set; } = new();
    }

    public class DailyRevenueDTO
    {
        public DateOnly Date { get; set; }
        public decimal Revenue { get; set; }
        public int Sessions { get; set; }
        public decimal EnergyDeliveredKwh { get; set; }
    }

    public class UsageStatisticsDTO
    {
        public Guid? StationId { get; set; }
        public string? StationName { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int TotalSessions { get; set; }
        public decimal TotalEnergyDeliveredKwh { get; set; }
        public decimal AverageSessionDurationMinutes { get; set; }
        public Dictionary<int, int> SessionsByHour { get; set; } = new();
        public int? PeakHour { get; set; }
    }
}

