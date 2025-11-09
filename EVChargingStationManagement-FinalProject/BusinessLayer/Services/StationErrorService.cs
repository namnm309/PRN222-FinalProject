using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class StationErrorService : IStationErrorService
    {
        private readonly EVDbContext _context;

        public StationErrorService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StationError>> GetAllErrorsAsync()
        {
            return await _context.StationErrors
                .Include(e => e.ChargingStation)
                .Include(e => e.ChargingSpot)
                .Include(e => e.ReportedByUser)
                .Include(e => e.ResolvedByUser)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<StationError?> GetErrorByIdAsync(Guid id)
        {
            return await _context.StationErrors
                .Include(e => e.ChargingStation)
                .Include(e => e.ChargingSpot)
                .Include(e => e.ReportedByUser)
                .Include(e => e.ResolvedByUser)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<StationError>> GetErrorsByStationIdAsync(Guid stationId)
        {
            return await _context.StationErrors
                .Include(e => e.ChargingStation)
                .Include(e => e.ChargingSpot)
                .Include(e => e.ReportedByUser)
                .Include(e => e.ResolvedByUser)
                .Where(e => e.ChargingStationId == stationId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<StationError>> GetErrorsBySpotIdAsync(Guid spotId)
        {
            return await _context.StationErrors
                .Include(e => e.ChargingStation)
                .Include(e => e.ChargingSpot)
                .Include(e => e.ReportedByUser)
                .Include(e => e.ResolvedByUser)
                .Where(e => e.ChargingSpotId == spotId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<StationError>> GetErrorsByStatusAsync(ErrorStatus status)
        {
            return await _context.StationErrors
                .Include(e => e.ChargingStation)
                .Include(e => e.ChargingSpot)
                .Include(e => e.ReportedByUser)
                .Include(e => e.ResolvedByUser)
                .Where(e => e.Status == status)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<StationError>> GetErrorsByUserIdAsync(Guid userId)
        {
            return await _context.StationErrors
                .Include(e => e.ChargingStation)
                .Include(e => e.ChargingSpot)
                .Include(e => e.ReportedByUser)
                .Include(e => e.ResolvedByUser)
                .Where(e => e.ReportedByUserId == userId || e.ResolvedByUserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<StationError> CreateErrorAsync(StationError error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            // Kiểm tra station tồn tại
            var stationExists = await _context.ChargingStations.AnyAsync(s => s.Id == error.ChargingStationId);
            if (!stationExists)
                throw new InvalidOperationException("Charging station does not exist");

            // Kiểm tra spot tồn tại (nếu có)
            if (error.ChargingSpotId.HasValue)
            {
                var spotExists = await _context.ChargingSpots.AnyAsync(s => s.Id == error.ChargingSpotId.Value);
                if (!spotExists)
                    throw new InvalidOperationException("Charging spot does not exist");
            }

            // Kiểm tra user tồn tại
            var userExists = await _context.Users.AnyAsync(u => u.Id == error.ReportedByUserId);
            if (!userExists)
                throw new InvalidOperationException("User does not exist");

            error.Id = Guid.NewGuid();
            error.CreatedAt = DateTime.UtcNow;
            error.UpdatedAt = DateTime.UtcNow;

            if (!error.ReportedAt.HasValue)
            {
                error.ReportedAt = DateTime.UtcNow;
            }

            _context.StationErrors.Add(error);
            await _context.SaveChangesAsync();

            return error;
        }

        public async Task<StationError?> UpdateErrorAsync(Guid id, StationError error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            var existingError = await _context.StationErrors.FindAsync(id);
            if (existingError == null)
                return null;

            existingError.ResolvedByUserId = error.ResolvedByUserId;
            existingError.Status = error.Status;
            existingError.ResolvedAt = error.ResolvedAt;
            existingError.ResolutionNotes = error.ResolutionNotes;
            existingError.Severity = error.Severity;
            existingError.UpdatedAt = DateTime.UtcNow;

            // Tự động set ResolvedAt khi status là Resolved hoặc Closed
            if ((error.Status == ErrorStatus.Resolved || error.Status == ErrorStatus.Closed) && !existingError.ResolvedAt.HasValue)
            {
                existingError.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return existingError;
        }

        public async Task<bool> DeleteErrorAsync(Guid id)
        {
            var error = await _context.StationErrors.FindAsync(id);
            if (error == null)
                return false;

            _context.StationErrors.Remove(error);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ErrorExistsAsync(Guid id)
        {
            return await _context.StationErrors.AnyAsync(e => e.Id == id);
        }
    }
}

