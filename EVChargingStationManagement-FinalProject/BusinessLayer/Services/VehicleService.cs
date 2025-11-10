using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly EVDbContext _context;

        public VehicleService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VehicleDTO>> GetVehiclesByUserIdAsync(Guid userId)
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();

            return vehicles.Select(v => new VehicleDTO
            {
                Id = v.Id,
                LicensePlate = v.LicensePlate,
                Make = v.Make,
                Model = v.Model,
                Year = v.Year,
                Vin = v.Vin,
                ConnectorType = v.ConnectorType,
                UserId = v.UserId,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            });
        }

        public async Task<VehicleDTO?> GetVehicleByIdAsync(Guid id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return null;

            return new VehicleDTO
            {
                Id = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Vin = vehicle.Vin,
                ConnectorType = vehicle.ConnectorType,
                UserId = vehicle.UserId,
                CreatedAt = vehicle.CreatedAt,
                UpdatedAt = vehicle.UpdatedAt
            };
        }

        public async Task<VehicleDTO> CreateVehicleAsync(Guid userId, CreateVehicleRequest request)
        {
            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                LicensePlate = request.LicensePlate,
                Make = request.Make,
                Model = request.Model,
                Year = request.Year,
                Vin = request.Vin,
                ConnectorType = request.ConnectorType,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return new VehicleDTO
            {
                Id = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Vin = vehicle.Vin,
                ConnectorType = vehicle.ConnectorType,
                UserId = vehicle.UserId,
                CreatedAt = vehicle.CreatedAt,
                UpdatedAt = vehicle.UpdatedAt
            };
        }

        public async Task<bool> VehicleExistsAsync(Guid id, Guid userId)
        {
            return await _context.Vehicles.AnyAsync(v => v.Id == id && v.UserId == userId);
        }
    }
}

