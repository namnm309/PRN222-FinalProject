using System;

namespace BusinessLayer.DTOs
{
	public class VehicleDTO
	{
		public Guid Id { get; set; }
		public string LicensePlate { get; set; } = string.Empty;
		public string? Make { get; set; }
		public string? Model { get; set; }
		public int? Year { get; set; }
		public string? Vin { get; set; }
		public string? ConnectorType { get; set; }
		public Guid UserId { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class CreateVehicleRequest
	{
		public string LicensePlate { get; set; } = string.Empty;
		public string? Make { get; set; }
		public string? Model { get; set; }
		public int? Year { get; set; }
		public string? Vin { get; set; }
		public string? ConnectorType { get; set; }
	}
}


