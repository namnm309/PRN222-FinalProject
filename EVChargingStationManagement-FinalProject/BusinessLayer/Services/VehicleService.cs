using System.Linq;
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

        public async Task<IEnumerable<VehicleDTO>> GetVehiclesByUserAsync(Guid userId)
        {
            var userVehicles = await _context.UserVehicles
                .Where(uv => uv.UserId == userId)
                .Include(uv => uv.Vehicle)
                .ToListAsync();

            return userVehicles
                .Where(uv => uv.Vehicle != null)
                .Select(uv => MapToDTO(uv.Vehicle!, uv))
                .ToList();
        }

        public async Task<VehicleDTO?> GetVehicleByIdAsync(Guid vehicleId)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.UserVehicles)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);
            
            if (vehicle == null)
                return null;

            var userVehicle = vehicle.UserVehicles.FirstOrDefault();
            return MapToDTO(vehicle, userVehicle);
        }

        public async Task<VehicleDTO> CreateVehicleAsync(Guid userId, CreateVehicleRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Make = request.Make,
                Model = request.Model,
                ModelYear = request.ModelYear,
                LicensePlate = request.LicensePlate,
                VehicleType = request.VehicleType,
                BatteryCapacityKwh = request.BatteryCapacityKwh,
                MaxChargingPowerKw = request.MaxChargingPowerKw,
                Color = request.Color,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var userVehicle = new UserVehicle
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = vehicle.Id,
                IsPrimary = request.IsPrimary,
                Nickname = request.Nickname,
                ChargePortLocation = request.ChargePortLocation,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            _context.UserVehicles.Add(userVehicle);

            if (request.IsPrimary)
            {
                await ResetPrimaryVehicleAsync(userId, vehicle.Id);
            }

            await _context.SaveChangesAsync();
            
            // Reload với navigation properties
            var createdVehicle = await _context.Vehicles
                .Include(v => v.UserVehicles)
                .FirstOrDefaultAsync(v => v.Id == vehicle.Id);
            
            var createdUserVehicle = createdVehicle?.UserVehicles.FirstOrDefault();
            return MapToDTO(createdVehicle!, createdUserVehicle);
        }

        public async Task<VehicleDTO?> UpdateVehicleAsync(Guid vehicleId, UpdateVehicleRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingVehicle = await _context.Vehicles
                .Include(v => v.UserVehicles)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);

            if (existingVehicle == null)
            {
                return null;
            }

            existingVehicle.Make = request.Make;
            existingVehicle.Model = request.Model;
            existingVehicle.ModelYear = request.ModelYear;
            existingVehicle.LicensePlate = request.LicensePlate;
            existingVehicle.VehicleType = request.VehicleType;
            existingVehicle.BatteryCapacityKwh = request.BatteryCapacityKwh;
            existingVehicle.MaxChargingPowerKw = request.MaxChargingPowerKw;
            existingVehicle.Color = request.Color;
            existingVehicle.Notes = request.Notes;
            existingVehicle.UpdatedAt = DateTime.UtcNow;

            var userVehicle = existingVehicle.UserVehicles.FirstOrDefault();
            if (userVehicle != null)
            {
                userVehicle.IsPrimary = request.IsPrimary;
                userVehicle.Nickname = request.Nickname;
                userVehicle.ChargePortLocation = request.ChargePortLocation;
                userVehicle.UpdatedAt = DateTime.UtcNow;

                if (request.IsPrimary)
                {
                    await ResetPrimaryVehicleAsync(userVehicle.UserId, existingVehicle.Id);
                }
            }

            await _context.SaveChangesAsync();
            
            // Reload với navigation properties
            var updatedVehicle = await _context.Vehicles
                .Include(v => v.UserVehicles)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);
            
            if (updatedVehicle == null)
                return null;

            var updatedUserVehicle = updatedVehicle.UserVehicles.FirstOrDefault();
            return MapToDTO(updatedVehicle, updatedUserVehicle);
        }

        public async Task<bool> DeleteVehicleAsync(Guid vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
            {
                return false;
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SetPrimaryVehicleAsync(Guid userId, Guid vehicleId)
        {
            await ResetPrimaryVehicleAsync(userId, vehicleId);
            await _context.SaveChangesAsync();
        }

        private async Task ResetPrimaryVehicleAsync(Guid userId, Guid newPrimaryVehicleId)
        {
            var userVehicles = await _context.UserVehicles
                .Where(uv => uv.UserId == userId)
                .ToListAsync();

            foreach (var uv in userVehicles)
            {
                uv.IsPrimary = uv.VehicleId == newPrimaryVehicleId;
                uv.UpdatedAt = DateTime.UtcNow;
            }
        }

        private VehicleDTO MapToDTO(Vehicle vehicle, UserVehicle? userVehicle)
        {
            return new VehicleDTO
            {
                Id = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                ModelYear = vehicle.ModelYear,
                LicensePlate = vehicle.LicensePlate,
                VehicleType = vehicle.VehicleType,
                BatteryCapacityKwh = vehicle.BatteryCapacityKwh,
                MaxChargingPowerKw = vehicle.MaxChargingPowerKw,
                Color = vehicle.Color,
                Notes = vehicle.Notes,
                IsPrimary = userVehicle?.IsPrimary ?? false,
                Nickname = userVehicle?.Nickname,
                ChargePortLocation = userVehicle?.ChargePortLocation
            };
        }
    }
}

