using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ChargingStationService : IChargingStationService
    {
        private readonly EVDbContext _context;

        public ChargingStationService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChargingStation>> GetAllStationsAsync()
        {
            return await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<ChargingStation?> GetStationByIdAsync(Guid id)
        {
            return await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<ChargingStation>> GetStationsByStatusAsync(StationStatus status)
        {
            return await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .Where(s => s.Status == status)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<ChargingStation> CreateStationAsync(ChargingStation station)
        {
            if (station == null)
                throw new ArgumentNullException(nameof(station));

            station.Id = Guid.NewGuid();
            station.CreatedAt = DateTime.UtcNow;
            station.UpdatedAt = DateTime.UtcNow;

            _context.ChargingStations.Add(station);
            await _context.SaveChangesAsync();

            return station;
        }

        public async Task<ChargingStation?> UpdateStationAsync(Guid id, ChargingStation station)
        {
            if (station == null)
                throw new ArgumentNullException(nameof(station));

            var existingStation = await _context.ChargingStations.FindAsync(id);
            if (existingStation == null)
                return null;

            existingStation.Name = station.Name;
            existingStation.Address = station.Address;
            existingStation.City = station.City;
            existingStation.Province = station.Province;
            existingStation.PostalCode = station.PostalCode;
            existingStation.Latitude = station.Latitude;
            existingStation.Longitude = station.Longitude;
            existingStation.Phone = station.Phone;
            existingStation.Email = station.Email;
            existingStation.Status = station.Status;
            existingStation.Description = station.Description;
            existingStation.OpeningTime = station.OpeningTime;
            existingStation.ClosingTime = station.ClosingTime;
            existingStation.Is24Hours = station.Is24Hours;
            existingStation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existingStation;
        }

        public async Task<bool> DeleteStationAsync(Guid id)
        {
            var station = await _context.ChargingStations.FindAsync(id);
            if (station == null)
                return false;

            _context.ChargingStations.Remove(station);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> StationExistsAsync(Guid id)
        {
            return await _context.ChargingStations.AnyAsync(s => s.Id == id);
        }
    }
}

