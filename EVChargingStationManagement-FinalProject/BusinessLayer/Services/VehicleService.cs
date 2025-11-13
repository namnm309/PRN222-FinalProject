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

        public async Task<Vehicle> CreateVehicleAsync(Guid userId, CreateVehicleRequest request)
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
            return vehicle;
        }

        public async Task<Vehicle?> UpdateVehicleAsync(Guid vehicleId, UpdateVehicleRequest request)
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

