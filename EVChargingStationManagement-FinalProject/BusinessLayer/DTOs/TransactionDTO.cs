using System;

namespace BusinessLayer.DTOs
{
	public class TransactionDTO
	{
		public Guid Id { get; set; }
		public Guid ChargingSessionId { get; set; }
		public string PaymentMethod { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public DateTime? PaidAt { get; set; }
		public string? ReferenceCode { get; set; }
	}
}


