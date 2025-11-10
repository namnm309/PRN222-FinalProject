using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_charging_session")]
    public class ChargingSession : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid ChargingStationId { get; set; }

        public Guid ChargingSpotId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public decimal EnergyConsumed { get; set; } = 0; // kWh

        public decimal TotalCost { get; set; } = 0; // VND

        public SessionStatus Status { get; set; } = SessionStatus.Active;

        public string? PaymentMethod { get; set; } // Momo, VNPay, Cash, etc.

        public string? TransactionId { get; set; }

        public int? CurrentSoC { get; set; } // State of Charge (%)

        public int? TargetSoC { get; set; } // Target State of Charge (%)

        public decimal? PowerOutput { get; set; } // kW (công suất đang sạc)

        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }

        [ForeignKey("ChargingStationId")]
        public virtual ChargingStation? ChargingStation { get; set; }

        [ForeignKey("ChargingSpotId")]
        public virtual ChargingSpot? ChargingSpot { get; set; }
    }
}

