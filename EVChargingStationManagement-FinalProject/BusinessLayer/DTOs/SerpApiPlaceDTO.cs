namespace BusinessLayer.DTOs
{
    public class SerpApiPlaceDTO
    {
        public string? PlaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Rating { get; set; }
        public int? Reviews { get; set; }
    }
}

