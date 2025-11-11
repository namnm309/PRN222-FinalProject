using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class ReservationDTO
    {
        public Guid Id { get; set; }
        public Guid ChargingSpotId { get; set; }
        public string? ChargingSpotNumber { get; set; }
        public Guid ChargingStationId { get; set; }
        public string? ChargingStationName { get; set; }
        public Guid UserId { get; set; }
        public Guid? VehicleId { get; set; }
        public string? VehicleName { get; set; }
        public string? UserFullName { get; set; }
        public ReservationStatus Status { get; set; }
        public string ConfirmationCode { get; set; } = string.Empty;
        public DateTime ScheduledStartTime { get; set; }
        public DateTime ScheduledEndTime { get; set; }
        public decimal? EstimatedEnergyKwh { get; set; }
        public decimal? EstimatedCost { get; set; }
        public bool IsPrepaid { get; set; }
        public string? Notes { get; set; }
    }
}

