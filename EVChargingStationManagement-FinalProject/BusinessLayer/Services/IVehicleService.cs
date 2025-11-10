using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleDTO>> GetVehiclesByUserIdAsync(Guid userId);
        Task<VehicleDTO?> GetVehicleByIdAsync(Guid id);
        Task<VehicleDTO> CreateVehicleAsync(Guid userId, CreateVehicleRequest request);
        Task<bool> VehicleExistsAsync(Guid id, Guid userId);
    }
}

