using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class ChargingSessionDTO
    {
        public Guid Id { get; set; }
        public Guid ChargingSpotId { get; set; }
        public string? ChargingSpotNumber { get; set; }
        public Guid ChargingStationId { get; set; }
        public string? ChargingStationName { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public Guid? VehicleId { get; set; }
        public string? VehicleName { get; set; }
        public Guid? ReservationId { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }
        public ChargingSessionStatus Status { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime? SessionEndTime { get; set; }
        public decimal? EnergyDeliveredKwh { get; set; }
        public decimal? EnergyRequestedKwh { get; set; }
        public decimal? Cost { get; set; }
        public decimal? PricePerKwh { get; set; }
        public decimal? ChargingSpotPower { get; set; }
        public string? ExternalSessionId { get; set; }
        public string? Notes { get; set; }
    }
}

