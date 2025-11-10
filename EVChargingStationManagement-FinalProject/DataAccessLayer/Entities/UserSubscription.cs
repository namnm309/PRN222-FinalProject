using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
	[Table("tbl_user_subscription")]
	public class UserSubscription : BaseEntity
	{
		public Guid UserId { get; set; }
		public Guid SubscriptionPlanId { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
		public bool AutoRenew { get; set; } = true;

		[ForeignKey("UserId")] public virtual Users? User { get; set; }
		[ForeignKey("SubscriptionPlanId")] public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
	}
}
