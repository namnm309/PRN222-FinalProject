using DataAccessLayer.Enums;

namespace PresentationLayer.DTOs
{
    public class ChargingSpotDTO
    {
        public Guid Id { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
        public Guid ChargingStationId { get; set; }
        public string? ChargingStationName { get; set; }
        public SpotStatus Status { get; set; }
        public string? ConnectorType { get; set; }
        public decimal? PowerOutput { get; set; }
        public decimal? PricePerKwh { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateChargingSpotRequest
    {
        public string SpotNumber { get; set; } = string.Empty;
        public Guid ChargingStationId { get; set; }
        public SpotStatus Status { get; set; } = SpotStatus.Available;
        public string? ConnectorType { get; set; }
        public decimal? PowerOutput { get; set; }
        public decimal? PricePerKwh { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateChargingSpotRequest
    {
        public string SpotNumber { get; set; } = string.Empty;
        public SpotStatus Status { get; set; }
        public string? ConnectorType { get; set; }
        public decimal? PowerOutput { get; set; }
        public decimal? PricePerKwh { get; set; }
        public string? Description { get; set; }
    }
}

