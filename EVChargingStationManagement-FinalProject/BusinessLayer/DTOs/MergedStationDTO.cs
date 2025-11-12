using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class MergedStationDTO
    {
        // Database fields
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Province { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public StationStatus? Status { get; set; }
        public int TotalSpots { get; set; }
        public int AvailableSpots { get; set; }
        public decimal? PricePerKwh { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }
        public TimeOnly? OpeningTime { get; set; }
        public TimeOnly? ClosingTime { get; set; }
        public bool Is24Hours { get; set; }

        // SerpApi fields
        public string? SerpApiPlaceId { get; set; }
        public decimal? ExternalRating { get; set; }
        public int? ExternalReviewCount { get; set; }
        public string? SerpApiTitle { get; set; }
        public string? SerpApiAddress { get; set; }

        // Flags
        public bool HasDbData { get; set; }
        public bool HasSerpApiData { get; set; }
    }
}

