using BusinessLayer.DTOs;
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

        public async Task<StationError> CreateErrorAsync(CreateStationErrorRequest request)
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

            // Kiểm tra user tồn tại
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.ReportedByUserId);
            if (!userExists)
                throw new InvalidOperationException("User does not exist");

            var error = new StationError
            {
                Id = Guid.NewGuid(),
                ChargingStationId = request.ChargingStationId,
                ChargingSpotId = request.ChargingSpotId,
                ReportedByUserId = request.ReportedByUserId,
                Status = request.Status,
                ErrorCode = request.ErrorCode,
                Title = request.Title,
                Description = request.Description,
                Severity = request.Severity,
                ReportedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.StationErrors.Add(error);
            await _context.SaveChangesAsync();

            return error;
        }

        public async Task<StationError?> UpdateErrorAsync(Guid id, UpdateStationErrorRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingError = await _context.StationErrors.FindAsync(id);
            if (existingError == null)
                return null;

            if (request.ResolvedByUserId.HasValue)
            {
                existingError.ResolvedByUserId = request.ResolvedByUserId;
            }
            existingError.Status = request.Status;
            if (request.ResolvedAt.HasValue)
            {
                existingError.ResolvedAt = request.ResolvedAt;
            }
            if (request.ResolutionNotes != null)
            {
                existingError.ResolutionNotes = request.ResolutionNotes;
            }
            if (request.Severity != null)
            {
                existingError.Severity = request.Severity;
            }
            existingError.UpdatedAt = DateTime.UtcNow;

            // Tự động set ResolvedAt khi status là Resolved hoặc Closed
            if ((request.Status == ErrorStatus.Resolved || request.Status == ErrorStatus.Closed) && !existingError.ResolvedAt.HasValue)
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

