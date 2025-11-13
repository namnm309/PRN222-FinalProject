using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IChargingStationService
    {
        Task<IEnumerable<ChargingStation>> GetAllStationsAsync();
        Task<ChargingStation?> GetStationByIdAsync(Guid id);
        Task<IEnumerable<ChargingStation>> GetStationsByStatusAsync(StationStatus status);
        Task<IEnumerable<ChargingStation>> GetNearestStationsAsync(decimal latitude, decimal longitude, double radiusKm = 10, StationStatus? status = null, string? connectorType = null);
        Task<ChargingStation> CreateStationAsync(CreateChargingStationRequest request);
        Task<ChargingStation?> UpdateStationAsync(Guid id, UpdateChargingStationRequest request);
        Task<bool> DeleteStationAsync(Guid id);
        Task<bool> StationExistsAsync(Guid id);
    }
}

