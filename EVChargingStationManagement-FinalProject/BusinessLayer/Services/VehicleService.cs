using System.Linq;
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

        public async Task<IEnumerable<Vehicle>> GetVehiclesByUserAsync(Guid userId)
        {
            var userVehicles = await _context.UserVehicles
                .Where(uv => uv.UserId == userId)
                .Include(uv => uv.Vehicle)
                .ToListAsync();

            foreach (var userVehicle in userVehicles)
            {
                if (userVehicle.Vehicle != null)
                {
                    userVehicle.Vehicle.UserVehicles = new List<UserVehicle> { userVehicle };
                }
            }

            return userVehicles
                .Where(uv => uv.Vehicle != null)
                .Select(uv => uv.Vehicle!)
                .ToList();
        }

        public async Task<Vehicle?> GetVehicleByIdAsync(Guid vehicleId)
        {
            return await _context.Vehicles
                .Include(v => v.UserVehicles)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);
        }

        public async Task<Vehicle> CreateVehicleAsync(Guid userId, Vehicle vehicle, bool isPrimary, string? nickname, string? chargePortLocation)
        {
            vehicle.Id = Guid.NewGuid();
            vehicle.CreatedAt = DateTime.UtcNow;
            vehicle.UpdatedAt = DateTime.UtcNow;

            var userVehicle = new UserVehicle
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = vehicle.Id,
                IsPrimary = isPrimary,
                Nickname = nickname,
                ChargePortLocation = chargePortLocation,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            _context.UserVehicles.Add(userVehicle);

            if (isPrimary)
            {
                await ResetPrimaryVehicleAsync(userId, vehicle.Id);
            }

            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task<Vehicle?> UpdateVehicleAsync(Guid vehicleId, Vehicle vehicle, bool isPrimary, string? nickname, string? chargePortLocation)
        {
            var existingVehicle = await _context.Vehicles
                .Include(v => v.UserVehicles)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);

            if (existingVehicle == null)
            {
                return null;
            }

            existingVehicle.Make = vehicle.Make;
            existingVehicle.Model = vehicle.Model;
            existingVehicle.ModelYear = vehicle.ModelYear;
            existingVehicle.LicensePlate = vehicle.LicensePlate;
            existingVehicle.VehicleType = vehicle.VehicleType;
            existingVehicle.BatteryCapacityKwh = vehicle.BatteryCapacityKwh;
            existingVehicle.MaxChargingPowerKw = vehicle.MaxChargingPowerKw;
            existingVehicle.Color = vehicle.Color;
            existingVehicle.Notes = vehicle.Notes;
            existingVehicle.UpdatedAt = DateTime.UtcNow;

            var userVehicle = existingVehicle.UserVehicles.FirstOrDefault();
            if (userVehicle != null)
            {
                userVehicle.IsPrimary = isPrimary;
                userVehicle.Nickname = nickname;
                userVehicle.ChargePortLocation = chargePortLocation;
                userVehicle.UpdatedAt = DateTime.UtcNow;

                if (isPrimary)
                {
                    await ResetPrimaryVehicleAsync(userVehicle.UserId, existingVehicle.Id);
                }
            }

            await _context.SaveChangesAsync();
            return existingVehicle;
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
    }
}

