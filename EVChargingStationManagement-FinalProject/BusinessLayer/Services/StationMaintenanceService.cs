using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class StationMaintenanceService : IStationMaintenanceService
    {
        private readonly EVDbContext _context;

        public StationMaintenanceService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StationMaintenance>> GetAllMaintenancesAsync()
        {
            return await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<StationMaintenance?> GetMaintenanceByIdAsync(Guid id)
        {
            return await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<StationMaintenance>> GetMaintenancesByStationIdAsync(Guid stationId)
        {
            return await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .Where(m => m.ChargingStationId == stationId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<StationMaintenance>> GetMaintenancesBySpotIdAsync(Guid spotId)
        {
            return await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .Where(m => m.ChargingSpotId == spotId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<StationMaintenance>> GetMaintenancesByStatusAsync(MaintenanceStatus status)
        {
            return await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .Where(m => m.Status == status)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<StationMaintenance>> GetMaintenancesByUserIdAsync(Guid userId)
        {
            return await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .Where(m => m.ReportedByUserId == userId || m.AssignedToUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<StationMaintenance> CreateMaintenanceAsync(CreateStationMaintenanceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Kiểm tra station tồn tại
            var stationExists = await _context.ChargingStations.AnyAsync(s => s.Id == request.ChargingStationId);
            if (!stationExists)
                throw new InvalidOperationException("Charging station does not exist");

            // Kiểm tra spot tồn tại (nếu có)
            if (request.ChargingSpotId.HasValue)
            {
                var spotExists = await _context.ChargingSpots.AnyAsync(s => s.Id == request.ChargingSpotId.Value);
                if (!spotExists)
                    throw new InvalidOperationException("Charging spot does not exist");
            }

            var maintenance = new StationMaintenance
            {
                Id = Guid.NewGuid(),
                ChargingStationId = request.ChargingStationId,
                ChargingSpotId = request.ChargingSpotId,
                ReportedByUserId = request.ReportedByUserId,
                AssignedToUserId = request.AssignedToUserId,
                ScheduledDate = request.ScheduledDate,
                Status = request.Status,
                Title = request.Title,
                Description = request.Description,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.StationMaintenances.Add(maintenance);
            await _context.SaveChangesAsync();

            return maintenance;
        }

        public async Task<StationMaintenance?> UpdateMaintenanceAsync(Guid id, UpdateStationMaintenanceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingMaintenance = await _context.StationMaintenances.FindAsync(id);
            if (existingMaintenance == null)
                return null;

            if (request.ChargingSpotId.HasValue)
            {
                existingMaintenance.ChargingSpotId = request.ChargingSpotId;
            }
            if (request.AssignedToUserId.HasValue)
            {
                existingMaintenance.AssignedToUserId = request.AssignedToUserId;
            }
            if (request.ScheduledDate.HasValue)
            {
                existingMaintenance.ScheduledDate = request.ScheduledDate.Value;
            }
            if (request.StartDate.HasValue)
            {
                existingMaintenance.StartDate = request.StartDate;
            }
            if (request.EndDate.HasValue)
            {
                existingMaintenance.EndDate = request.EndDate;
            }
            existingMaintenance.Status = request.Status;
            existingMaintenance.Title = request.Title;
            existingMaintenance.Description = request.Description;
            if (request.Notes != null)
            {
                existingMaintenance.Notes = request.Notes;
            }
            existingMaintenance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existingMaintenance;
        }

        public async Task<bool> DeleteMaintenanceAsync(Guid id)
        {
            var maintenance = await _context.StationMaintenances.FindAsync(id);
            if (maintenance == null)
                return false;

            _context.StationMaintenances.Remove(maintenance);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MaintenanceExistsAsync(Guid id)
        {
            return await _context.StationMaintenances.AnyAsync(m => m.Id == id);
        }
    }
}

