using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_charging_station")]
    public class ChargingStation : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? Province { get; set; }

        public string? PostalCode { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public StationStatus Status { get; set; } = StationStatus.Active;

        public string? Description { get; set; }

        public TimeOnly? OpeningTime { get; set; }

        public TimeOnly? ClosingTime { get; set; }

        public bool Is24Hours { get; set; } = false;

        // SerpApi integration fields
        public string? SerpApiPlaceId { get; set; }
        public decimal? ExternalRating { get; set; }
        public int? ExternalReviewCount { get; set; }
        public bool IsFromSerpApi { get; set; } = false;
        public DateTime? SerpApiLastSynced { get; set; }

        // Navigation property
        public virtual ICollection<ChargingSpot> ChargingSpots { get; set; } = new List<ChargingSpot>();

        public virtual ICollection<StationAmenity> Amenities { get; set; } = new List<StationAmenity>();

        public virtual ICollection<StationReport> StationReports { get; set; } = new List<StationReport>();
    }
}

