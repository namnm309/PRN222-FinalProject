using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_station_error")]
    public class StationError : BaseEntity
    {
        public Guid ChargingStationId { get; set; }

        public Guid? ChargingSpotId { get; set; } // Nullable - có thể lỗi toàn bộ station

        public Guid ReportedByUserId { get; set; } // Người báo cáo lỗi

        public Guid? ResolvedByUserId { get; set; } // Người xử lý lỗi

        public ErrorStatus Status { get; set; } = ErrorStatus.Reported;

        public string ErrorCode { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? ReportedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public string? ResolutionNotes { get; set; }

        public string? Severity { get; set; } // e.g., "Low", "Medium", "High", "Critical"

        // Navigation properties
        [ForeignKey("ChargingStationId")]
        public virtual ChargingStation? ChargingStation { get; set; }

        [ForeignKey("ChargingSpotId")]
        public virtual ChargingSpot? ChargingSpot { get; set; }

        [ForeignKey("ReportedByUserId")]
        public virtual Users? ReportedByUser { get; set; }

        [ForeignKey("ResolvedByUserId")]
        public virtual Users? ResolvedByUser { get; set; }
    }
}

