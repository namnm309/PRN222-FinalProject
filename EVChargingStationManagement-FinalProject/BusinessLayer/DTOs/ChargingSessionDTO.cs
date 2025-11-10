using System;

namespace BusinessLayer.DTOs
{
	public class ChargingSessionDTO
	{
		public Guid Id { get; set; }
		public Guid BookingId { get; set; }
		public Guid ChargingStationId { get; set; }
		public Guid ChargingSpotId { get; set; }
		public DateTime? StartedAt { get; set; }
		public DateTime? EndedAt { get; set; }
		public decimal? EnergyKwh { get; set; }
		public decimal? PricePerKwh { get; set; }
		public decimal? TotalAmount { get; set; }
		public string Status { get; set; } = string.Empty;
	}
}


