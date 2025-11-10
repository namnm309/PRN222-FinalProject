using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class StationMaintenanceDTO
    {
        public Guid Id { get; set; }
        public Guid ChargingStationId { get; set; }
        public string? ChargingStationName { get; set; }
        public Guid? ChargingSpotId { get; set; }
        public string? ChargingSpotNumber { get; set; }
        public Guid? ReportedByUserId { get; set; }
        public string? ReportedByUserName { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public MaintenanceStatus Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateStationMaintenanceRequest
    {
        public Guid ChargingStationId { get; set; }
        public Guid? ChargingSpotId { get; set; }
        public Guid? ReportedByUserId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class UpdateStationMaintenanceRequest
    {
        public Guid? ChargingSpotId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public MaintenanceStatus Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}

