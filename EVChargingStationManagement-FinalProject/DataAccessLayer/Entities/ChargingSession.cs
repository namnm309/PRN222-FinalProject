using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_charging_session")]
    public class ChargingSession : BaseEntity
    {
        // TuDev branch fields (standalone sessions)
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

        // Main branch fields (booking-based sessions) - nullable for backward compatibility
        public Guid? BookingId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public decimal? EnergyKwh { get; set; }
        public decimal? PricePerKwh { get; set; }
        public decimal? TotalAmount { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }

        [ForeignKey("ChargingStationId")]
        public virtual ChargingStation? ChargingStation { get; set; }

        [ForeignKey("ChargingSpotId")]
        public virtual ChargingSpot? ChargingSpot { get; set; }

        // TODO: Uncomment after merging Booking/Transaction entities from main branch
        // [ForeignKey("BookingId")]
        // public virtual Booking? Booking { get; set; }
        
        // One-to-one with Transaction (Main branch)
        // public virtual Transaction? Transaction { get; set; }
    }
}
