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

        public async Task<StationMaintenance> CreateMaintenanceAsync(StationMaintenance maintenance)
        {
            if (maintenance == null)
                throw new ArgumentNullException(nameof(maintenance));

            // Kiểm tra station tồn tại
            var stationExists = await _context.ChargingStations.AnyAsync(s => s.Id == maintenance.ChargingStationId);
            if (!stationExists)
                throw new InvalidOperationException("Charging station does not exist");

            // Kiểm tra spot tồn tại (nếu có)
            if (maintenance.ChargingSpotId.HasValue)
            {
                var spotExists = await _context.ChargingSpots.AnyAsync(s => s.Id == maintenance.ChargingSpotId.Value);
                if (!spotExists)
                    throw new InvalidOperationException("Charging spot does not exist");
            }

            maintenance.Id = Guid.NewGuid();
            maintenance.CreatedAt = DateTime.UtcNow;
            maintenance.UpdatedAt = DateTime.UtcNow;

            _context.StationMaintenances.Add(maintenance);
            await _context.SaveChangesAsync();

            return maintenance;
        }

        public async Task<StationMaintenance?> UpdateMaintenanceAsync(Guid id, StationMaintenance maintenance)
        {
            if (maintenance == null)
                throw new ArgumentNullException(nameof(maintenance));

            var existingMaintenance = await _context.StationMaintenances.FindAsync(id);
            if (existingMaintenance == null)
                return null;

            existingMaintenance.ChargingSpotId = maintenance.ChargingSpotId;
            existingMaintenance.AssignedToUserId = maintenance.AssignedToUserId;
            existingMaintenance.ScheduledDate = maintenance.ScheduledDate;
            existingMaintenance.StartDate = maintenance.StartDate;
            existingMaintenance.EndDate = maintenance.EndDate;
            existingMaintenance.Status = maintenance.Status;
            existingMaintenance.Title = maintenance.Title;
            existingMaintenance.Description = maintenance.Description;
            existingMaintenance.Notes = maintenance.Notes;
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

