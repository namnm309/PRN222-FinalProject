using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs
{
    public class CompleteChargingSessionRequest
    {
        [Required]
        public decimal EnergyDeliveredKwh { get; set; }

        [Required]
        public decimal Cost { get; set; }

        public decimal? PricePerKwh { get; set; }

        public string? Notes { get; set; }
    }
}

