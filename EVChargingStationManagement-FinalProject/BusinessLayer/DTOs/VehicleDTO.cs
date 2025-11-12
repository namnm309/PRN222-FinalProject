using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class VehicleDTO
    {
        public Guid Id { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? ModelYear { get; set; }
        public string? LicensePlate { get; set; }
        public VehicleType VehicleType { get; set; }
        public decimal? BatteryCapacityKwh { get; set; }
        public decimal? MaxChargingPowerKw { get; set; }
        public string? Color { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }
        public string? Nickname { get; set; }
        public string? ChargePortLocation { get; set; }
    }
}

