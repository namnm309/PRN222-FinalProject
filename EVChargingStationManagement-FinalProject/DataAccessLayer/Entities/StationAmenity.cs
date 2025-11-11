using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_station_amenity")]
    public class StationAmenity : BaseEntity
    {
        public Guid ChargingStationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ChargingStationId))]
        public virtual ChargingStation? ChargingStation { get; set; }
    }
}

