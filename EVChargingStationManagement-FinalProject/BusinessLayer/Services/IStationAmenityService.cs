using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface IStationAmenityService
    {
        Task<IEnumerable<StationAmenity>> GetAmenitiesByStationAsync(Guid stationId);
        Task<StationAmenity?> GetAmenityByIdAsync(Guid id);
        Task<StationAmenity> CreateAmenityAsync(StationAmenity amenity);
        Task<StationAmenity?> UpdateAmenityAsync(Guid id, StationAmenity amenity);
        Task<bool> DeleteAmenityAsync(Guid id);
    }
}

