using System;

namespace BusinessLayer.DTOs
{
	public class ReviewDTO
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public Guid ChargingStationId { get; set; }
		public int Rating { get; set; }
		public string? Comment { get; set; }
		public DateTime ReviewedAt { get; set; }
	}
}


