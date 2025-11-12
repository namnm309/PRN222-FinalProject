using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_charging_spot")]
    public class ChargingSpot : BaseEntity
    {
        public string SpotNumber { get; set; } = string.Empty;

        public Guid ChargingStationId { get; set; }

        public SpotStatus Status { get; set; } = SpotStatus.Available;

        public string? ConnectorType { get; set; } // e.g., "Type 2", "CCS", "CHAdeMO"

        public decimal? PowerOutput { get; set; } // kW

        public decimal? PricePerKwh { get; set; }

        public string? Description { get; set; }

        // QR Code and online status
        public string? QrCode { get; set; }
        public bool IsOnline { get; set; } = true;

        // Navigation properties
        [ForeignKey("ChargingStationId")]
        public virtual ChargingStation? ChargingStation { get; set; }

        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        public virtual ICollection<ChargingSession> ChargingSessions { get; set; } = new List<ChargingSession>();
    }
}

