using System;

namespace BusinessLayer.DTOs
{
	public class BookingDTO
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public Guid VehicleId { get; set; }
		public Guid ChargingStationId { get; set; }
		public Guid ChargingSpotId { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; } = string.Empty;
		public string? Notes { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class CreateBookingRequest
	{
		public Guid VehicleId { get; set; }
		public Guid ChargingStationId { get; set; }
		public Guid ChargingSpotId { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string? Notes { get; set; }
	}

	public class UpdateBookingStatusRequest
	{
		public string Status { get; set; } = string.Empty;
		public string? Notes { get; set; }
	}
}


