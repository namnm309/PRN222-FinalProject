using System;

namespace BusinessLayer.DTOs
{
	public class OrderDto
	{
		public Guid OrderId { get; set; }
		public long Amount { get; set; } // VND
		public string? Description { get; set; }
	}
}


