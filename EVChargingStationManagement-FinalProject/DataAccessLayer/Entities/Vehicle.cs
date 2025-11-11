using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_vehicle")]
    public class Vehicle : BaseEntity
    {
        public string Make { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int? ModelYear { get; set; }

        public string? LicensePlate { get; set; }

        public string? Vin { get; set; }

        public VehicleType VehicleType { get; set; } = VehicleType.Car;

        public decimal? BatteryCapacityKwh { get; set; }

        public decimal? MaxChargingPowerKw { get; set; }

        public string? Color { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public virtual ICollection<UserVehicle> UserVehicles { get; set; } = new List<UserVehicle>();

        public virtual ICollection<ChargingSession> ChargingSessions { get; set; } = new List<ChargingSession>();
    }
}

