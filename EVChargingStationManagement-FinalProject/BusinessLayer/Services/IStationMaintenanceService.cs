using BusinessLayer.DTOs;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IStationMaintenanceService
    {
        Task<IEnumerable<StationMaintenanceDTO>> GetAllMaintenancesAsync();
        Task<StationMaintenanceDTO?> GetMaintenanceByIdAsync(Guid id);
        Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesByStationIdAsync(Guid stationId);
        Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesBySpotIdAsync(Guid spotId);
        Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesByStatusAsync(MaintenanceStatus status);
        Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesByStatusStringAsync(string status);
        Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesByUserIdAsync(Guid userId);
        Task<StationMaintenanceDTO> CreateMaintenanceAsync(CreateStationMaintenanceRequest request);
        Task<StationMaintenanceDTO?> UpdateMaintenanceAsync(Guid id, UpdateStationMaintenanceRequest request);
        Task<bool> DeleteMaintenanceAsync(Guid id);
        Task<bool> MaintenanceExistsAsync(Guid id);
    }
}

