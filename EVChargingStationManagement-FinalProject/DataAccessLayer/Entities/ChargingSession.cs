using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
	[Table("tbl_charging_session")]
	public class ChargingSession : BaseEntity
	{
		public Guid UserId { get; set; }
		public Guid ChargingStationId { get; set; }
		public Guid? ChargingSpotId { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public decimal? EnergyKwh { get; set; }
		public decimal? PricePerKwh { get; set; }
		public decimal? TotalAmount { get; set; }
		public SessionStatus Status { get; set; } = SessionStatus.Started;

		[ForeignKey("UserId")] public virtual Users? User { get; set; }
		[ForeignKey("ChargingStationId")] public virtual ChargingStation? ChargingStation { get; set; }
		[ForeignKey("ChargingSpotId")] public virtual ChargingSpot? ChargingSpot { get; set; }
	}
}
