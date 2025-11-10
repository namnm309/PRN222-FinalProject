using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_review")]
    public class Review : BaseEntity
	{
		public Guid UserId { get; set; }
		public Users? User { get; set; }
		public Guid ChargingStationId { get; set; }
		public ChargingStation? ChargingStation { get; set; }
		public int Rating { get; set; } // 1-5
		public string? Comment { get; set; }
		public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
	}
}


