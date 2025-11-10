using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ChargingSpotService : IChargingSpotService
    {
        private readonly EVDbContext _context;
        private readonly IStationMonitoringService _monitoringService;

        public ChargingSpotService(EVDbContext context, IStationMonitoringService monitoringService)
        {
            _context = context;
            _monitoringService = monitoringService;
        }

        public async Task<IEnumerable<ChargingSpot>> GetAllSpotsAsync()
        {
            return await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .OrderBy(s => s.ChargingStation!.Name)
                .ThenBy(s => s.SpotNumber)
                .ToListAsync();
        }

        public async Task<ChargingSpot?> GetSpotByIdAsync(Guid id)
        {
            return await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<ChargingSpot>> GetSpotsByStationIdAsync(Guid stationId)
        {
            return await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .Where(s => s.ChargingStationId == stationId)
                .OrderBy(s => s.SpotNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingSpot>> GetSpotsByStatusAsync(SpotStatus status)
        {
            return await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .Where(s => s.Status == status)
                .OrderBy(s => s.ChargingStation!.Name)
                .ThenBy(s => s.SpotNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingSpot>> GetAvailableSpotsByStationIdAsync(Guid stationId)
        {
            return await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .Where(s => s.ChargingStationId == stationId && s.Status == SpotStatus.Available)
                .OrderBy(s => s.SpotNumber)
                .ToListAsync();
        }

        public async Task<ChargingSpot> CreateSpotAsync(ChargingSpot spot)
        {
            if (spot == null)
                throw new ArgumentNullException(nameof(spot));

            // Kiểm tra station tồn tại
            var stationExists = await _context.ChargingStations.AnyAsync(s => s.Id == spot.ChargingStationId);
            if (!stationExists)
                throw new InvalidOperationException("Charging station does not exist");

            // Kiểm tra spot number đã tồn tại trong station chưa
            var spotExists = await SpotNumberExistsInStationAsync(spot.ChargingStationId, spot.SpotNumber);
            if (spotExists)
                throw new InvalidOperationException($"Spot number {spot.SpotNumber} already exists in this station");

            spot.Id = Guid.NewGuid();
            spot.CreatedAt = DateTime.UtcNow;
            spot.UpdatedAt = DateTime.UtcNow;

            _context.ChargingSpots.Add(spot);
            await _context.SaveChangesAsync();

            return spot;
        }

        public async Task<ChargingSpot?> UpdateSpotAsync(Guid id, ChargingSpot spot)
        {
            if (spot == null)
                throw new ArgumentNullException(nameof(spot));

            var existingSpot = await _context.ChargingSpots.FindAsync(id);
            if (existingSpot == null)
                return null;

            // Kiểm tra spot number đã tồn tại trong station chưa (nếu thay đổi)
            if (existingSpot.SpotNumber != spot.SpotNumber)
            {
                var spotExists = await SpotNumberExistsInStationAsync(existingSpot.ChargingStationId, spot.SpotNumber, id);
                if (spotExists)
                    throw new InvalidOperationException($"Spot number {spot.SpotNumber} already exists in this station");
            }

            // Track status change for broadcasting
            var oldStatus = existingSpot.Status;
            var statusChanged = oldStatus != spot.Status;

            existingSpot.SpotNumber = spot.SpotNumber;
            existingSpot.Status = spot.Status;
            existingSpot.ConnectorType = spot.ConnectorType;
            existingSpot.PowerOutput = spot.PowerOutput;
            existingSpot.PricePerKwh = spot.PricePerKwh;
            existingSpot.Description = spot.Description;
            existingSpot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Broadcast status change if changed
            if (statusChanged)
            {
                try
                {
                    await _monitoringService.BroadcastSpotStatusChange(
                        existingSpot.Id,
                        existingSpot.ChargingStationId,
                        (int)existingSpot.Status,
                        existingSpot.SpotNumber
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChargingSpotService] Failed to broadcast status change: {ex.Message}");
                }
            }

            return existingSpot;
        }

        public async Task<bool> DeleteSpotAsync(Guid id)
        {
            var spot = await _context.ChargingSpots.FindAsync(id);
            if (spot == null)
                return false;

            _context.ChargingSpots.Remove(spot);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SpotExistsAsync(Guid id)
        {
            return await _context.ChargingSpots.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> SpotNumberExistsInStationAsync(Guid stationId, string spotNumber, Guid? excludeId = null)
        {
            var query = _context.ChargingSpots
                .Where(s => s.ChargingStationId == stationId && s.SpotNumber == spotNumber);

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}

