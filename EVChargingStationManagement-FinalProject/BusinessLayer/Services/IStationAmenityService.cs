using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface IStationAmenityService
    {
        Task<IEnumerable<StationAmenityDTO>> GetAmenitiesByStationAsync(Guid stationId);
        Task<StationAmenityDTO?> GetAmenityByIdAsync(Guid id);
        Task<StationAmenityDTO> CreateAmenityAsync(CreateStationAmenityRequest request);
        Task<StationAmenityDTO?> UpdateAmenityAsync(Guid id, UpdateStationAmenityRequest request);
        Task<bool> DeleteAmenityAsync(Guid id);
    }
}

