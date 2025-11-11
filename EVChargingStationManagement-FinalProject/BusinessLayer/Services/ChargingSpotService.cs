using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ChargingSpotService : IChargingSpotService
    {
        private readonly EVDbContext _context;

        public ChargingSpotService(EVDbContext context)
        {
            _context = context;
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
            var now = DateTime.UtcNow;
            
            // Lấy các cổng trống: Status = Available, không có session đang chạy, không có reservation active, và station phải Active
            return await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .Where(s => s.ChargingStationId == stationId &&
                           s.Status == SpotStatus.Available &&
                           s.ChargingStation != null &&
                           s.ChargingStation.Status == StationStatus.Active &&
                           !_context.ChargingSessions.Any(cs => cs.ChargingSpotId == s.Id && cs.Status == ChargingSessionStatus.InProgress) &&
                           !_context.Reservations.Any(r => r.ChargingSpotId == s.Id &&
                                                          (r.Status == ReservationStatus.Pending ||
                                                           r.Status == ReservationStatus.Confirmed ||
                                                           r.Status == ReservationStatus.CheckedIn) &&
                                                          r.ScheduledEndTime > now))
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

            existingSpot.SpotNumber = spot.SpotNumber;
            existingSpot.Status = spot.Status;
            existingSpot.ConnectorType = spot.ConnectorType;
            existingSpot.PowerOutput = spot.PowerOutput;
            existingSpot.PricePerKwh = spot.PricePerKwh;
            existingSpot.Description = spot.Description;
            existingSpot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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

