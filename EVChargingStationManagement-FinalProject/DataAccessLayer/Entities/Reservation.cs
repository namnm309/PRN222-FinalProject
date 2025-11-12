using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_reservation")]
    public class Reservation : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid ChargingSpotId { get; set; }

        public Guid? VehicleId { get; set; }

        public DateTime ScheduledStartTime { get; set; }

        public DateTime ScheduledEndTime { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public string ConfirmationCode { get; set; } = string.Empty;

        public decimal? EstimatedEnergyKwh { get; set; }

        public decimal? EstimatedCost { get; set; }

        public string? Notes { get; set; }

        public bool IsPrepaid { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual Users? User { get; set; }

        [ForeignKey(nameof(ChargingSpotId))]
        public virtual ChargingSpot? ChargingSpot { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }

        public virtual ICollection<ChargingSession> ChargingSessions { get; set; } = new List<ChargingSession>();

        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}

