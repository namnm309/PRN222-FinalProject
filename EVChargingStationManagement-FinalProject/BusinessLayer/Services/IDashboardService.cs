namespace BusinessLayer.Services
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDTO> GetOverviewAsync(Guid? stationId = null);
        Task<IEnumerable<SessionTimelineDTO>> GetSessionTimelineAsync(Guid? stationId = null);
        Task<DashboardSessionsDTO> GetAllSessionsAsync(
            Guid? stationId = null,
            DataAccessLayer.Enums.ChargingSessionStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50);
    }

    public class DashboardOverviewDTO
    {
        public int TotalSpots { get; set; }
        public int AvailableSpots { get; set; }
        public int ActiveSessions { get; set; }
        public int ReservationsToday { get; set; }
        public decimal EnergyDeliveredToday { get; set; }
        public decimal RevenueToday { get; set; }
    }

    public class SessionTimelineDTO
    {
        public Guid Id { get; set; }
        public string? StationName { get; set; }
        public string? SpotNumber { get; set; }
        public DataAccessLayer.Enums.ChargingSessionStatus Status { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime? SessionEndTime { get; set; }
        public decimal? EnergyDeliveredKwh { get; set; }
        public decimal? Cost { get; set; }
    }

    public class DashboardSessionsDTO
    {
        public IEnumerable<SessionDetailDTO> Data { get; set; } = new List<SessionDetailDTO>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class SessionDetailDTO
    {
        public Guid Id { get; set; }
        public string? StationName { get; set; }
        public Guid? StationId { get; set; }
        public string? SpotNumber { get; set; }
        public string? UserName { get; set; }
        public string? VehicleName { get; set; }
        public DataAccessLayer.Enums.ChargingSessionStatus Status { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime? SessionEndTime { get; set; }
        public decimal? EnergyDeliveredKwh { get; set; }
        public decimal? EnergyRequestedKwh { get; set; }
        public decimal? Cost { get; set; }
        public decimal? PricePerKwh { get; set; }
        public int? DurationMinutes { get; set; }
    }
}

