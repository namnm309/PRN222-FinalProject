using System.ComponentModel.DataAnnotations;

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

    public class CreateStationAmenityRequest
    {
        [Required]
        public Guid ChargingStationId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }
    }

    public class UpdateStationAmenityRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }
    }
}

