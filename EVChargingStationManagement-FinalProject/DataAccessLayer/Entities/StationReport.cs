using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_station_report")]
    public class StationReport : BaseEntity
    {
        public Guid ChargingStationId { get; set; }

        public DateOnly ReportDate { get; set; }

        public int TotalSessions { get; set; }

        public decimal TotalEnergyDeliveredKwh { get; set; }

        public decimal TotalRevenue { get; set; }

        public int? PeakHour { get; set; }

        public decimal? AverageSessionDurationMinutes { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ChargingStationId))]
        public virtual ChargingStation? ChargingStation { get; set; }
    }
}

