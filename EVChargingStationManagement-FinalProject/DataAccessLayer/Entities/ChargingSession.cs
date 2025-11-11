using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_charging_session")]
    public class ChargingSession : BaseEntity
    {
        public Guid ChargingSpotId { get; set; }

        public Guid UserId { get; set; }

        public Guid? ReservationId { get; set; }

        public Guid? VehicleId { get; set; }

        public ChargingSessionStatus Status { get; set; } = ChargingSessionStatus.Scheduled;

        public DateTime SessionStartTime { get; set; }

        public DateTime? SessionEndTime { get; set; }

        public decimal? EnergyDeliveredKwh { get; set; }

        public decimal? EnergyRequestedKwh { get; set; }

        public decimal? Cost { get; set; }

        public decimal? PricePerKwh { get; set; }

        public string? ExternalSessionId { get; set; }

        public string? Notes { get; set; }

        // SOC tracking fields
        public decimal? CurrentSocPercentage { get; set; }
        public decimal? InitialSocPercentage { get; set; }
        public decimal? TargetSocPercentage { get; set; }
        public decimal? CurrentPowerKw { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? QrCodeScanned { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ChargingSpotId))]
        public virtual ChargingSpot? ChargingSpot { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual Users? User { get; set; }

        [ForeignKey(nameof(ReservationId))]
        public virtual Reservation? Reservation { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }

        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

        public virtual ICollection<ChargingSessionProgress> ProgressHistory { get; set; } = new List<ChargingSessionProgress>();
    }
}

