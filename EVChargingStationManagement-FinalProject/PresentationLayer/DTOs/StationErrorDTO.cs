using DataAccessLayer.Enums;

namespace PresentationLayer.DTOs
{
    public class StationErrorDTO
    {
        public Guid Id { get; set; }
        public Guid ChargingStationId { get; set; }
        public string? ChargingStationName { get; set; }
        public Guid? ChargingSpotId { get; set; }
        public string? ChargingSpotNumber { get; set; }
        public Guid ReportedByUserId { get; set; }
        public string? ReportedByUserName { get; set; }
        public Guid? ResolvedByUserId { get; set; }
        public string? ResolvedByUserName { get; set; }
        public ErrorStatus Status { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? ReportedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }
        public string? Severity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateStationErrorRequest
    {
        public Guid ChargingStationId { get; set; }
        public Guid? ChargingSpotId { get; set; }
        public Guid ReportedByUserId { get; set; }
        public ErrorStatus Status { get; set; } = ErrorStatus.Reported;
        public string ErrorCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Severity { get; set; }
    }

    public class UpdateStationErrorRequest
    {
        public Guid? ResolvedByUserId { get; set; }
        public ErrorStatus Status { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }
        public string? Severity { get; set; }
    }
}

