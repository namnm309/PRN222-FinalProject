using BusinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface IVehicleService
    {
        Task<IEnumerable<Vehicle>> GetVehiclesByUserAsync(Guid userId);
        Task<Vehicle?> GetVehicleByIdAsync(Guid vehicleId);
        Task<Vehicle> CreateVehicleAsync(Guid userId, CreateVehicleRequest request);
        Task<Vehicle?> UpdateVehicleAsync(Guid vehicleId, UpdateVehicleRequest request);
        Task<bool> DeleteVehicleAsync(Guid vehicleId);
        Task SetPrimaryVehicleAsync(Guid userId, Guid vehicleId);
    }
}

