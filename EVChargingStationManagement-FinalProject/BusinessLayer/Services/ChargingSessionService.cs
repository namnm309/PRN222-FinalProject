using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ChargingSessionService : IChargingSessionService
    {
        private readonly EVDbContext _context;

        public ChargingSessionService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChargingSession>> GetAllSessionsAsync()
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<ChargingSession?> GetSessionByIdAsync(Guid id)
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<ChargingSession>> GetSessionsByUserIdAsync(Guid userId)
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingSession>> GetSessionsByStationIdAsync(Guid stationId)
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .Where(s => s.ChargingStationId == stationId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingSession>> GetSessionsBySpotIdAsync(Guid spotId)
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .Where(s => s.ChargingSpotId == spotId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingSession>> GetSessionsByStatusAsync(SessionStatus status)
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .Where(s => s.Status == status)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingSession>> GetActiveSessionsAsync()
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .Where(s => s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<ChargingSession?> GetActiveSessionBySpotIdAsync(Guid spotId)
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .FirstOrDefaultAsync(s => s.ChargingSpotId == spotId && 
                    (s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused));
        }

        public async Task<ChargingSession?> GetActiveSessionByUserIdAsync(Guid userId)
        {
            return await _context.ChargingSessions
                .Include(s => s.User)
                .Include(s => s.ChargingStation)
                .Include(s => s.ChargingSpot)
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                    (s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused));
        }

        public async Task<ChargingSession> CreateSessionAsync(ChargingSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            // Kiểm tra user tồn tại
            var userExists = await _context.Users.AnyAsync(u => u.Id == session.UserId);
            if (!userExists)
                throw new InvalidOperationException("User does not exist");

            // Kiểm tra station tồn tại
            var stationExists = await _context.ChargingStations.AnyAsync(s => s.Id == session.ChargingStationId);
            if (!stationExists)
                throw new InvalidOperationException("Charging station does not exist");

            // Kiểm tra spot tồn tại
            var spot = await _context.ChargingSpots.FindAsync(session.ChargingSpotId);
            if (spot == null)
                throw new InvalidOperationException("Charging spot does not exist");

            // Kiểm tra spot có đang được sử dụng không
            var activeSession = await GetActiveSessionBySpotIdAsync(session.ChargingSpotId);
            if (activeSession != null)
                throw new InvalidOperationException("This charging spot is already in use");

            // Kiểm tra user có phiên sạc đang active không
            var userActiveSession = await GetActiveSessionByUserIdAsync(session.UserId);
            if (userActiveSession != null)
                throw new InvalidOperationException("User already has an active charging session");

            // Cập nhật trạng thái spot
            spot.Status = SpotStatus.Occupied;
            spot.UpdatedAt = DateTime.UtcNow;

            session.Id = Guid.NewGuid();
            session.StartTime = DateTime.UtcNow;
            session.Status = SessionStatus.Active;
            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            _context.ChargingSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<ChargingSession?> UpdateSessionAsync(Guid id, ChargingSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var existingSession = await _context.ChargingSessions.FindAsync(id);
            if (existingSession == null)
                return null;

            existingSession.Status = session.Status;
            existingSession.EnergyConsumed = session.EnergyConsumed;
            existingSession.TotalCost = session.TotalCost;
            existingSession.CurrentSoC = session.CurrentSoC;
            existingSession.PowerOutput = session.PowerOutput;
            existingSession.PaymentMethod = session.PaymentMethod;
            existingSession.TransactionId = session.TransactionId;
            existingSession.Notes = session.Notes;
            existingSession.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existingSession;
        }

        public async Task<ChargingSession?> StopSessionAsync(Guid id, decimal energyConsumed, decimal totalCost, string? paymentMethod, string? notes)
        {
            var session = await _context.ChargingSessions
                .Include(s => s.ChargingSpot)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return null;

            if (session.Status != SessionStatus.Active && session.Status != SessionStatus.Paused)
                throw new InvalidOperationException("Only active or paused sessions can be stopped");

            session.EndTime = DateTime.UtcNow;
            session.Status = SessionStatus.Completed;
            session.EnergyConsumed = energyConsumed;
            session.TotalCost = totalCost;
            session.PaymentMethod = paymentMethod;
            session.Notes = notes;
            session.UpdatedAt = DateTime.UtcNow;

            // Cập nhật trạng thái spot về Available
            if (session.ChargingSpot != null)
            {
                session.ChargingSpot.Status = SpotStatus.Available;
                session.ChargingSpot.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<ChargingSession?> PauseSessionAsync(Guid id, string? notes)
        {
            var session = await _context.ChargingSessions.FindAsync(id);
            if (session == null)
                return null;

            if (session.Status != SessionStatus.Active)
                throw new InvalidOperationException("Only active sessions can be paused");

            session.Status = SessionStatus.Paused;
            session.Notes = notes ?? session.Notes;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<ChargingSession?> ResumeSessionAsync(Guid id, string? notes)
        {
            var session = await _context.ChargingSessions.FindAsync(id);
            if (session == null)
                return null;

            if (session.Status != SessionStatus.Paused)
                throw new InvalidOperationException("Only paused sessions can be resumed");

            session.Status = SessionStatus.Active;
            session.Notes = notes ?? session.Notes;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<ChargingSession?> CancelSessionAsync(Guid id, string? reason)
        {
            var session = await _context.ChargingSessions
                .Include(s => s.ChargingSpot)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return null;

            if (session.Status == SessionStatus.Completed)
                throw new InvalidOperationException("Completed sessions cannot be cancelled");

            session.Status = SessionStatus.Cancelled;
            session.EndTime = DateTime.UtcNow;
            session.Notes = reason ?? session.Notes;
            session.UpdatedAt = DateTime.UtcNow;

            // Cập nhật trạng thái spot về Available
            if (session.ChargingSpot != null)
            {
                session.ChargingSpot.Status = SpotStatus.Available;
                session.ChargingSpot.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<bool> DeleteSessionAsync(Guid id)
        {
            var session = await _context.ChargingSessions.FindAsync(id);
            if (session == null)
                return false;

            _context.ChargingSessions.Remove(session);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SessionExistsAsync(Guid id)
        {
            return await _context.ChargingSessions.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> CanStartSessionAsync(Guid spotId)
        {
            var activeSession = await GetActiveSessionBySpotIdAsync(spotId);
            return activeSession == null;
        }
    }
}

