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
        Task<ChargingSpot> CreateSpotAsync(ChargingSpot spot);
        Task<ChargingSpot?> UpdateSpotAsync(Guid id, ChargingSpot spot);
        Task<bool> DeleteSpotAsync(Guid id);
        Task<bool> SpotExistsAsync(Guid id);
        Task<bool> SpotNumberExistsInStationAsync(Guid stationId, string spotNumber, Guid? excludeId = null);
    }
}

