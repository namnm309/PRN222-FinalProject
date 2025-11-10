using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
	[Table("tbl_station_staff")]
	public class StationStaffAssignment : BaseEntity
	{
		public Guid ChargingStationId { get; set; }
		public Guid UserId { get; set; }
		public StationStaffRole Role { get; set; } = StationStaffRole.Operator;

		[ForeignKey("ChargingStationId")] public virtual ChargingStation? ChargingStation { get; set; }
		[ForeignKey("UserId")] public virtual Users? User { get; set; }
	}
}
