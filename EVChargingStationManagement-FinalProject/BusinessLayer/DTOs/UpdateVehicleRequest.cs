using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class UpdateVehicleRequest
    {
        [Required]
        public string Make { get; set; } = string.Empty;

        [Required]
        public string Model { get; set; } = string.Empty;

        [Range(1990, 2100)]
        public int? ModelYear { get; set; }

        [StringLength(50)]
        public string? LicensePlate { get; set; }

        public VehicleType VehicleType { get; set; } = VehicleType.Car;

        [Range(0, 1000)]
        public decimal? BatteryCapacityKwh { get; set; }

        [Range(0, 500)]
        public decimal? MaxChargingPowerKw { get; set; }

        public string? Color { get; set; }

        public string? Notes { get; set; }

        public bool IsPrimary { get; set; }

        public string? Nickname { get; set; }

        public string? ChargePortLocation { get; set; }
    }
}

