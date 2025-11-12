using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class ChargingStationDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public StationStatus Status { get; set; }
        public string? Description { get; set; }
        public TimeOnly? OpeningTime { get; set; }
        public TimeOnly? ClosingTime { get; set; }
        public bool Is24Hours { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalSpots { get; set; }
        public int AvailableSpots { get; set; }
        // Charging spot info (from first spot)
        public string? ConnectorType { get; set; } // Loại sạc
        public decimal? PricePerKwh { get; set; } // Giá sạc
        // SerpApi fields
        public string? SerpApiPlaceId { get; set; }
        public decimal? ExternalRating { get; set; }
        public int? ExternalReviewCount { get; set; }
        public bool IsFromSerpApi { get; set; }
        public DateTime? SerpApiLastSynced { get; set; }
        public decimal? DistanceKm { get; set; } // Khoảng cách từ vị trí tìm kiếm (km)
    }

    public class CreateChargingStationRequest
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
        // SerpApi fields
        public string? SerpApiPlaceId { get; set; }
        public decimal? ExternalRating { get; set; }
        public int? ExternalReviewCount { get; set; }
        // Charging spots fields - hỗ trợ 2 cách:
        // Cách 1: Tự động tạo với giá trị mặc định
        public int TotalSpots { get; set; } = 0; // Số chỗ sạc
        public string? DefaultConnectorType { get; set; } // Loại cổng sạc (CCS, CHAdeMO, AC/Type2)
        public decimal? DefaultPowerOutput { get; set; } // Công suất mặc định (kW)
        public decimal? DefaultPricePerKwh { get; set; } // Giá mặc định (VND/kWh)
        
        // Cách 2: Tạo từng spot chi tiết (từ form admin)
        public List<CreateSpotItem>? Spots { get; set; }
    }

    public class CreateSpotItem
    {
        public string SpotNumber { get; set; } = string.Empty;
        public string? ConnectorType { get; set; }
        public decimal? PowerOutput { get; set; }
        public decimal? PricePerKwh { get; set; }
        public SpotStatus Status { get; set; } = SpotStatus.Available;
    }

    public class UpdateSpotItem
    {
        public Guid? Id { get; set; } // null = tạo mới, có giá trị = cập nhật
        public string SpotNumber { get; set; } = string.Empty;
        public string? ConnectorType { get; set; }
        public decimal? PowerOutput { get; set; }
        public decimal? PricePerKwh { get; set; }
        public SpotStatus Status { get; set; } = SpotStatus.Available;
    }

    public class UpdateChargingStationRequest
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
        public StationStatus Status { get; set; }
        public string? Description { get; set; }
        public TimeOnly? OpeningTime { get; set; }
        public TimeOnly? ClosingTime { get; set; }
        public bool Is24Hours { get; set; }
        // SerpApi fields
        public string? SerpApiPlaceId { get; set; }
        public decimal? ExternalRating { get; set; }
        public int? ExternalReviewCount { get; set; }
        // Charging spots fields (optional - only update if provided)
        public int? TotalSpots { get; set; } // null = không thay đổi, > 0 = cập nhật số lượng
        public string? DefaultConnectorType { get; set; } // null = không thay đổi
        public decimal? DefaultPowerOutput { get; set; } // null = không thay đổi
        public decimal? DefaultPricePerKwh { get; set; } // null = không thay đổi
        
        // Cách 2: Cập nhật từng spot chi tiết (từ form admin)
        public List<UpdateSpotItem>? Spots { get; set; }
    }
}

