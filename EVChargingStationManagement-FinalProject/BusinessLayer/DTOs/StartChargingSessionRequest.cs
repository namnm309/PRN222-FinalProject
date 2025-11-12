using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs
{
    public class StartChargingSessionRequest
    {
        [Required]
        public Guid ChargingSpotId { get; set; }

        public Guid? ReservationId { get; set; }

        public Guid? VehicleId { get; set; }

        public decimal? EnergyRequestedKwh { get; set; }

        public decimal? PricePerKwh { get; set; }

        public string? Notes { get; set; }
    }
}

