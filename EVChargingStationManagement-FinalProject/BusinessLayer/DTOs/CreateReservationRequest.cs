using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs
{
    public class CreateReservationRequest
    {
        [Required]
        public Guid ChargingSpotId { get; set; }

        public Guid? VehicleId { get; set; }

        [Required]
        public DateTime ScheduledStartTime { get; set; }

        public DateTime? ScheduledEndTime { get; set; }

        [Range(0, 1000)]
        public decimal? EstimatedEnergyKwh { get; set; }

        [Range(0, 1000000000)]
        public decimal? EstimatedCost { get; set; }

        public bool IsPrepaid { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}

