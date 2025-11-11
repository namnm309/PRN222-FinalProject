using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface IVehicleService
    {
        Task<IEnumerable<Vehicle>> GetVehiclesByUserAsync(Guid userId);
        Task<Vehicle?> GetVehicleByIdAsync(Guid vehicleId);
        Task<Vehicle> CreateVehicleAsync(Guid userId, Vehicle vehicle, bool isPrimary, string? nickname, string? chargePortLocation);
        Task<Vehicle?> UpdateVehicleAsync(Guid vehicleId, Vehicle vehicle, bool isPrimary, string? nickname, string? chargePortLocation);
        Task<bool> DeleteVehicleAsync(Guid vehicleId);
        Task SetPrimaryVehicleAsync(Guid userId, Guid vehicleId);
    }
}

