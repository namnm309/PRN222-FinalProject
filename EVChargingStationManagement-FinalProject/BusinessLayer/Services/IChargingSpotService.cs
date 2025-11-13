using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IChargingSpotService
    {
        Task<IEnumerable<ChargingSpot>> GetAllSpotsAsync();
        Task<ChargingSpot?> GetSpotByIdAsync(Guid id);
        Task<IEnumerable<ChargingSpot>> GetSpotsByStationIdAsync(Guid stationId);
        Task<IEnumerable<ChargingSpot>> GetSpotsByStatusAsync(SpotStatus status);
        Task<IEnumerable<ChargingSpot>> GetAvailableSpotsByStationIdAsync(Guid stationId);
        Task<ChargingSpot> CreateSpotAsync(CreateChargingSpotRequest request);
        Task<ChargingSpot?> UpdateSpotAsync(Guid id, UpdateChargingSpotRequest request);
        Task<bool> DeleteSpotAsync(Guid id);
        Task<bool> SpotExistsAsync(Guid id);
        Task<bool> SpotNumberExistsInStationAsync(Guid stationId, string spotNumber, Guid? excludeId = null);
    }
}

