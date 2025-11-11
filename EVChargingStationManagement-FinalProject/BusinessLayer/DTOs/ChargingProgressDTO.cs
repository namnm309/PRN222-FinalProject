namespace BusinessLayer.DTOs
{
    public class ChargingProgressDTO
    {
        public Guid SessionId { get; set; }
        public decimal? CurrentSocPercentage { get; set; }
        public decimal? InitialSocPercentage { get; set; }
        public decimal? TargetSocPercentage { get; set; }
        public decimal? CurrentPowerKw { get; set; }
        public decimal? EnergyDeliveredKwh { get; set; }
        public decimal? EstimatedTimeRemainingMinutes { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateChargingProgressRequest
    {
        public decimal SocPercentage { get; set; }
        public decimal? PowerKw { get; set; }
        public decimal EnergyDeliveredKwh { get; set; }
        public decimal? EstimatedTimeRemainingMinutes { get; set; }
    }

    public class QrCodeScanRequest
    {
        public string QrCode { get; set; } = string.Empty;
        public Guid? VehicleId { get; set; }
        public Guid? ReservationId { get; set; }
        public decimal? TargetSocPercentage { get; set; }
        public decimal? EnergyRequestedKwh { get; set; }
    }
}

