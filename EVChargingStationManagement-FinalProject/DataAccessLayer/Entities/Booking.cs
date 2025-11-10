using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_booking")]
    public class Booking : BaseEntity
	{
        
        public Guid UserId { get; set; }
		public Users? User { get; set; }
		public Guid VehicleId { get; set; }
		public Vehicle? Vehicle { get; set; }
		public Guid ChargingStationId { get; set; }
		public ChargingStation? ChargingStation { get; set; }
		public Guid ChargingSpotId { get; set; }
		public ChargingSpot? ChargingSpot { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed
		public string? Notes { get; set; }
		public ChargingSession? ChargingSession { get; set; }
	}
}


