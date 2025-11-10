using System;

namespace DataAccessLayer.Entities
{
	public class Transaction : BaseEntity
	{
		public Guid ChargingSessionId { get; set; }
		public ChargingSession? ChargingSession { get; set; }
		public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, EWallet
		public string Status { get; set; } = "Pending"; // Pending, Succeeded, Failed, Refunded
		public decimal Amount { get; set; }
		public DateTime? PaidAt { get; set; }
		public string? ReferenceCode { get; set; }
	}
}


