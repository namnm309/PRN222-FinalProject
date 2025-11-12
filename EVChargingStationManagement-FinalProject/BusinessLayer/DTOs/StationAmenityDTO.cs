namespace BusinessLayer.DTOs
{
    public class StationAmenityDTO
    {
        public Guid Id { get; set; }
        public Guid ChargingStationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }
}

