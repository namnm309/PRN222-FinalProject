using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IStationMaintenanceService
    {
        Task<IEnumerable<StationMaintenance>> GetAllMaintenancesAsync();
        Task<StationMaintenance?> GetMaintenanceByIdAsync(Guid id);
        Task<IEnumerable<StationMaintenance>> GetMaintenancesByStationIdAsync(Guid stationId);
        Task<IEnumerable<StationMaintenance>> GetMaintenancesBySpotIdAsync(Guid spotId);
        Task<IEnumerable<StationMaintenance>> GetMaintenancesByStatusAsync(MaintenanceStatus status);
        Task<IEnumerable<StationMaintenance>> GetMaintenancesByUserIdAsync(Guid userId);
        Task<StationMaintenance> CreateMaintenanceAsync(CreateStationMaintenanceRequest request);
        Task<StationMaintenance?> UpdateMaintenanceAsync(Guid id, UpdateStationMaintenanceRequest request);
        Task<bool> DeleteMaintenanceAsync(Guid id);
        Task<bool> MaintenanceExistsAsync(Guid id);
    }
}

