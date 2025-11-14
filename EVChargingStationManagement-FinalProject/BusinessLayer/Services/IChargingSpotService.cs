using BusinessLayer.DTOs;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IChargingSpotService
    {
        Task<IEnumerable<ChargingSpotDTO>> GetAllSpotsAsync();
        Task<ChargingSpotDTO?> GetSpotByIdAsync(Guid id);
        Task<IEnumerable<ChargingSpotDTO>> GetSpotsByStationIdAsync(Guid stationId);
        Task<IEnumerable<ChargingSpotDTO>> GetSpotsByStatusAsync(SpotStatus status);
        Task<IEnumerable<ChargingSpotDTO>> GetAvailableSpotsByStationIdAsync(Guid stationId);
        Task<ChargingSpotDTO> CreateSpotAsync(CreateChargingSpotRequest request);
        Task<ChargingSpotDTO?> UpdateSpotAsync(Guid id, UpdateChargingSpotRequest request);
        Task<bool> DeleteSpotAsync(Guid id);
        Task<bool> SpotExistsAsync(Guid id);
        Task<bool> SpotNumberExistsInStationAsync(Guid stationId, string spotNumber, Guid? excludeId = null);
    }
}

