using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class UpdateChargingSessionStatusRequest
    {
        [Required]
        public ChargingSessionStatus Status { get; set; }

        public string? Notes { get; set; }
    }
}

