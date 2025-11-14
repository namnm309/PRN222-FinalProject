using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleDTO>> GetVehiclesByUserAsync(Guid userId);
        Task<VehicleDTO?> GetVehicleByIdAsync(Guid vehicleId);
        Task<VehicleDTO> CreateVehicleAsync(Guid userId, CreateVehicleRequest request);
        Task<VehicleDTO?> UpdateVehicleAsync(Guid vehicleId, UpdateVehicleRequest request);
        Task<bool> DeleteVehicleAsync(Guid vehicleId);
        Task SetPrimaryVehicleAsync(Guid userId, Guid vehicleId);
    }
}

