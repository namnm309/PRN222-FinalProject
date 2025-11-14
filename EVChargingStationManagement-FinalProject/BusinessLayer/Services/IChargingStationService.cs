using BusinessLayer.DTOs;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IChargingStationService
    {
        Task<IEnumerable<ChargingStationDTO>> GetAllStationsAsync();
        Task<ChargingStationDTO?> GetStationByIdAsync(Guid id);
        Task<IEnumerable<ChargingStationDTO>> GetStationsByStatusAsync(StationStatus status);
        Task<IEnumerable<ChargingStationDTO>> GetNearestStationsAsync(decimal latitude, decimal longitude, double radiusKm = 10, StationStatus? status = null, string? connectorType = null);
        Task<ChargingStationDTO> CreateStationAsync(CreateChargingStationRequest request);
        Task<ChargingStationDTO?> UpdateStationAsync(Guid id, UpdateChargingStationRequest request);
        Task<bool> DeleteStationAsync(Guid id);
        Task<bool> StationExistsAsync(Guid id);
    }
}

