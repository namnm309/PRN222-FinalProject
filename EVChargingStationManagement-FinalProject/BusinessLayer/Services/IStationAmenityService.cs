using BusinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface IStationAmenityService
    {
        Task<IEnumerable<StationAmenity>> GetAmenitiesByStationAsync(Guid stationId);
        Task<StationAmenity?> GetAmenityByIdAsync(Guid id);
        Task<StationAmenity> CreateAmenityAsync(CreateStationAmenityRequest request);
        Task<StationAmenity?> UpdateAmenityAsync(Guid id, UpdateStationAmenityRequest request);
        Task<bool> DeleteAmenityAsync(Guid id);
    }
}

