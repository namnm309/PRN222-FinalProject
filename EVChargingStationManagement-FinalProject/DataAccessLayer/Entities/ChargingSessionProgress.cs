using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_charging_session_progress")]
    public class ChargingSessionProgress : BaseEntity
    {
        public Guid ChargingSessionId { get; set; }

        public DateTime RecordedAt { get; set; }

        public decimal SocPercentage { get; set; }

        public decimal? PowerKw { get; set; }

        public decimal EnergyDeliveredKwh { get; set; }

        public decimal? EstimatedTimeRemainingMinutes { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ChargingSessionId))]
        public virtual ChargingSession? ChargingSession { get; set; }
    }
}

