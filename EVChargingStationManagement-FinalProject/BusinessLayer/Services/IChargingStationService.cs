using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IChargingStationService
    {
        Task<IEnumerable<ChargingStation>> GetAllStationsAsync();
        Task<ChargingStation?> GetStationByIdAsync(Guid id);
        Task<IEnumerable<ChargingStation>> GetStationsByStatusAsync(StationStatus status);
        Task<ChargingStation> CreateStationAsync(ChargingStation station);
        Task<ChargingStation?> UpdateStationAsync(Guid id, ChargingStation station);
        Task<bool> DeleteStationAsync(Guid id);
        Task<bool> StationExistsAsync(Guid id);
    }
}

