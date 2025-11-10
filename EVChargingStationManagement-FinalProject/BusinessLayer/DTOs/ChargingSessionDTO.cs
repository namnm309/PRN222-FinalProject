using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class ChargingSessionDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserFullName { get; set; }
        public Guid ChargingStationId { get; set; }
        public string? ChargingStationName { get; set; }
        public Guid ChargingSpotId { get; set; }
        public string? ChargingSpotNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal EnergyConsumed { get; set; }
        public decimal TotalCost { get; set; }
        public SessionStatus Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public int? CurrentSoC { get; set; }
        public int? TargetSoC { get; set; }
        public decimal? PowerOutput { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Calculated fields
        public TimeSpan? Duration 
        { 
            get 
            {
                if (EndTime.HasValue)
                    return EndTime.Value - StartTime;
                return DateTime.UtcNow - StartTime;
            }
        }
    }

    public class CreateChargingSessionRequest
    {
        public Guid UserId { get; set; }
        public Guid ChargingStationId { get; set; }
        public Guid ChargingSpotId { get; set; }
        public int? TargetSoC { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateChargingSessionRequest
    {
        public SessionStatus Status { get; set; }
        public decimal? EnergyConsumed { get; set; }
        public decimal? TotalCost { get; set; }
        public int? CurrentSoC { get; set; }
        public decimal? PowerOutput { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
    }

    public class StopChargingSessionRequest
    {
        public decimal EnergyConsumed { get; set; }
        public decimal TotalCost { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}

