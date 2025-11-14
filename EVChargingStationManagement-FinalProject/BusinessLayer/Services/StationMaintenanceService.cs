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

        public async Task<IEnumerable<StationMaintenanceDTO>> GetAllMaintenancesAsync()
        {
            var maintenances = await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            
            return maintenances.Select(MapToDTO);
        }

        public async Task<StationMaintenanceDTO?> GetMaintenanceByIdAsync(Guid id)
        {
            var maintenance = await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            return maintenance == null ? null : MapToDTO(maintenance);
        }

        public async Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesByStationIdAsync(Guid stationId)
        {
            var maintenances = await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .Where(m => m.ChargingStationId == stationId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            
            return maintenances.Select(MapToDTO);
        }

        public async Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesBySpotIdAsync(Guid spotId)
        {
            var maintenances = await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .Where(m => m.ChargingSpotId == spotId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            
            return maintenances.Select(MapToDTO);
        }

        public async Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesByStatusAsync(MaintenanceStatus status)
        {
            var maintenances = await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .Where(m => m.Status == status)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            
            return maintenances.Select(MapToDTO);
        }

        public async Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesByStatusStringAsync(string status)
        {
            if (!Enum.TryParse<MaintenanceStatus>(status, true, out var maintenanceStatus))
            {
                throw new ArgumentException($"Invalid status value: {status}", nameof(status));
            }

            return await GetMaintenancesByStatusAsync(maintenanceStatus);
        }

        public async Task<IEnumerable<StationMaintenanceDTO>> GetMaintenancesByUserIdAsync(Guid userId)
        {
            var maintenances = await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .Where(m => m.ReportedByUserId == userId || m.AssignedToUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            
            return maintenances.Select(MapToDTO);
        }

        public async Task<StationMaintenanceDTO> CreateMaintenanceAsync(CreateStationMaintenanceRequest request)
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

            // Reload với navigation properties
            var createdMaintenance = await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == maintenance.Id);
            
            return MapToDTO(createdMaintenance!);
        }

        public async Task<StationMaintenanceDTO?> UpdateMaintenanceAsync(Guid id, UpdateStationMaintenanceRequest request)
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

            // Reload với navigation properties
            var updatedMaintenance = await _context.StationMaintenances
                .Include(m => m.ChargingStation)
                .Include(m => m.ChargingSpot)
                .Include(m => m.ReportedByUser)
                .Include(m => m.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            return updatedMaintenance == null ? null : MapToDTO(updatedMaintenance);
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

        private StationMaintenanceDTO MapToDTO(StationMaintenance maintenance)
        {
            return new StationMaintenanceDTO
            {
                Id = maintenance.Id,
                ChargingStationId = maintenance.ChargingStationId,
                ChargingStationName = maintenance.ChargingStation?.Name,
                ChargingSpotId = maintenance.ChargingSpotId,
                ChargingSpotNumber = maintenance.ChargingSpot?.SpotNumber,
                ReportedByUserId = maintenance.ReportedByUserId,
                ReportedByUserName = maintenance.ReportedByUser?.FullName,
                AssignedToUserId = maintenance.AssignedToUserId,
                AssignedToUserName = maintenance.AssignedToUser?.FullName,
                ScheduledDate = maintenance.ScheduledDate,
                StartDate = maintenance.StartDate,
                EndDate = maintenance.EndDate,
                Status = maintenance.Status,
                Title = maintenance.Title,
                Description = maintenance.Description,
                Notes = maintenance.Notes,
                CreatedAt = maintenance.CreatedAt,
                UpdatedAt = maintenance.UpdatedAt
            };
        }
    }
}

