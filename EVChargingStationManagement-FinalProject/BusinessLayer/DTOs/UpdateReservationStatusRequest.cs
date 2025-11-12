using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class UpdateReservationStatusRequest
    {
        public ReservationStatus Status { get; set; }

        public string? Notes { get; set; }
    }
}

