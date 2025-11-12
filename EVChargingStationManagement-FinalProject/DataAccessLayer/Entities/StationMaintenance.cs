using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_station_maintenance")]
    public class StationMaintenance : BaseEntity
    {
        public Guid ChargingStationId { get; set; }

        public Guid? ChargingSpotId { get; set; } // Nullable - có thể bảo trì toàn bộ station

        public Guid? ReportedByUserId { get; set; } // Người báo cáo

        public Guid? AssignedToUserId { get; set; } // Người được giao nhiệm vụ bảo trì

        public DateTime ScheduledDate { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("ChargingStationId")]
        public virtual ChargingStation? ChargingStation { get; set; }

        [ForeignKey("ChargingSpotId")]
        public virtual ChargingSpot? ChargingSpot { get; set; }

        [ForeignKey("ReportedByUserId")]
        public virtual Users? ReportedByUser { get; set; }

        [ForeignKey("AssignedToUserId")]
        public virtual Users? AssignedToUser { get; set; }
    }
}

