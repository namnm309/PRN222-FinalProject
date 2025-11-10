using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_charging_session")]
    public class ChargingSession : BaseEntity
	{
        
        public Guid BookingId { get; set; }
		public Booking? Booking { get; set; }
		public Guid ChargingStationId { get; set; }
		public ChargingStation? ChargingStation { get; set; }
		public Guid ChargingSpotId { get; set; }
		public ChargingSpot? ChargingSpot { get; set; }
		public DateTime? StartedAt { get; set; }
		public DateTime? EndedAt { get; set; }
		public decimal? EnergyKwh { get; set; }
		public decimal? PricePerKwh { get; set; }
		public decimal? TotalAmount { get; set; }
		public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed
		public Transaction? Transaction { get; set; }
	}
}


