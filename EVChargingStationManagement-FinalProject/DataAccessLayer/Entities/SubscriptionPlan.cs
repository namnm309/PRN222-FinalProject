using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
	[Table("tbl_subscription_plan")]
	public class SubscriptionPlan : BaseEntity
	{
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public BillingType BillingType { get; set; } = BillingType.Prepaid;
		public decimal? PricePerMonth { get; set; }
		public decimal? PricePerKwh { get; set; }
		public decimal? IncludedKwh { get; set; }
		public bool IsActive { get; set; } = true;
	}
}
