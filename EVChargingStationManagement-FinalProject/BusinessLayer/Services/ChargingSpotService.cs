using BusinessLayer.DTOs;
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

        public async Task<IEnumerable<ChargingSpotDTO>> GetAllSpotsAsync()
        {
            var spots = await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .OrderBy(s => s.ChargingStation!.Name)
                .ThenBy(s => s.SpotNumber)
                .ToListAsync();
            
            return spots.Select(MapToDTO);
        }

        public async Task<ChargingSpotDTO?> GetSpotByIdAsync(Guid id)
        {
            var spot = await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            return spot == null ? null : MapToDTO(spot);
        }

        public async Task<IEnumerable<ChargingSpotDTO>> GetSpotsByStationIdAsync(Guid stationId)
        {
            var spots = await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .Where(s => s.ChargingStationId == stationId)
                .OrderBy(s => s.SpotNumber)
                .ToListAsync();
            
            return spots.Select(MapToDTO);
        }

        public async Task<IEnumerable<ChargingSpotDTO>> GetSpotsByStatusAsync(SpotStatus status)
        {
            var spots = await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .Where(s => s.Status == status)
                .OrderBy(s => s.ChargingStation!.Name)
                .ThenBy(s => s.SpotNumber)
                .ToListAsync();
            
            return spots.Select(MapToDTO);
        }

        public async Task<IEnumerable<ChargingSpotDTO>> GetAvailableSpotsByStationIdAsync(Guid stationId)
        {
            var now = DateTime.UtcNow;
            
            // Lấy các cổng trống: Status = Available, không có session đang chạy, không có reservation active, và station phải Active
            var spots = await _context.ChargingSpots
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
            
            return spots.Select(MapToDTO);
        }

        public async Task<ChargingSpotDTO> CreateSpotAsync(CreateChargingSpotRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Kiểm tra station tồn tại
            var stationExists = await _context.ChargingStations.AnyAsync(s => s.Id == request.ChargingStationId);
            if (!stationExists)
                throw new InvalidOperationException("Charging station does not exist");

            // Kiểm tra spot number đã tồn tại trong station chưa
            var spotExists = await SpotNumberExistsInStationAsync(request.ChargingStationId, request.SpotNumber);
            if (spotExists)
                throw new InvalidOperationException($"Spot number {request.SpotNumber} already exists in this station");

            var spot = new ChargingSpot
            {
                Id = Guid.NewGuid(),
                SpotNumber = request.SpotNumber,
                ChargingStationId = request.ChargingStationId,
                Status = request.Status,
                ConnectorType = request.ConnectorType,
                PowerOutput = request.PowerOutput,
                PricePerKwh = request.PricePerKwh,
                Description = request.Description,
                IsOnline = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChargingSpots.Add(spot);
            await _context.SaveChangesAsync();

            // Reload với navigation properties
            var createdSpot = await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .FirstOrDefaultAsync(s => s.Id == spot.Id);
            
            return MapToDTO(createdSpot!);
        }

        public async Task<ChargingSpotDTO?> UpdateSpotAsync(Guid id, UpdateChargingSpotRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingSpot = await _context.ChargingSpots.FindAsync(id);
            if (existingSpot == null)
                return null;

            // Kiểm tra spot number đã tồn tại trong station chưa (nếu thay đổi)
            if (existingSpot.SpotNumber != request.SpotNumber)
            {
                var spotExists = await SpotNumberExistsInStationAsync(existingSpot.ChargingStationId, request.SpotNumber, id);
                if (spotExists)
                    throw new InvalidOperationException($"Spot number {request.SpotNumber} already exists in this station");
            }

            existingSpot.SpotNumber = request.SpotNumber;
            existingSpot.Status = request.Status;
            existingSpot.ConnectorType = request.ConnectorType;
            existingSpot.PowerOutput = request.PowerOutput;
            existingSpot.PricePerKwh = request.PricePerKwh;
            existingSpot.Description = request.Description;
            existingSpot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload với navigation properties
            var updatedSpot = await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            return updatedSpot == null ? null : MapToDTO(updatedSpot);
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

        private ChargingSpotDTO MapToDTO(ChargingSpot spot)
        {
            return new ChargingSpotDTO
            {
                Id = spot.Id,
                SpotNumber = spot.SpotNumber,
                ChargingStationId = spot.ChargingStationId,
                ChargingStationName = spot.ChargingStation?.Name,
                Status = spot.Status,
                ConnectorType = spot.ConnectorType,
                PowerOutput = spot.PowerOutput,
                PricePerKwh = spot.PricePerKwh,
                Description = spot.Description,
                CreatedAt = spot.CreatedAt,
                UpdatedAt = spot.UpdatedAt,
                IsReserved = false, // Will be set by caller if needed
                IsAvailable = false, // Will be set by caller if needed
                IsOnline = spot.IsOnline
            };
        }
    }
}

