using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_user_vehicle")]
    public class UserVehicle : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid VehicleId { get; set; }

        public bool IsPrimary { get; set; }

        public string? Nickname { get; set; }

        public string? ChargePortLocation { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual Users? User { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }
    }
}

