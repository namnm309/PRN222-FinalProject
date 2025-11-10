using System;
using System.Collections.Generic;

namespace DataAccessLayer.Entities
{
	public class Vehicle : BaseEntity
	{
		public string LicensePlate { get; set; } = string.Empty;
		public string? Make { get; set; }
		public string? Model { get; set; }
		public int? Year { get; set; }
		public string? Vin { get; set; }
		public string? ConnectorType { get; set; }
		public Guid UserId { get; set; }
		public Users? User { get; set; }
		public ICollection<Booking>? Bookings { get; set; }
	}
}


