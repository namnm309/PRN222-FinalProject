using System.Linq;
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

        public async Task<IEnumerable<ChargingSession>> GetSessionsForUserAsync(Guid userId, int limit = 20)
        {
            return await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Where(cs => cs.UserId == userId)
                .OrderByDescending(cs => cs.SessionStartTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingSession>> GetActiveSessionsAsync(Guid? stationId = null)
        {
            var query = _context.ChargingSessions
                .Include(cs => cs.User)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Where(cs => cs.Status == ChargingSessionStatus.InProgress);

            if (stationId.HasValue)
            {
                query = query.Where(cs => cs.ChargingSpot != null && cs.ChargingSpot.ChargingStationId == stationId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<ChargingSession?> GetActiveSessionForUserAsync(Guid userId)
        {
            return await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Where(cs => cs.UserId == userId && cs.Status == ChargingSessionStatus.InProgress)
                .OrderByDescending(cs => cs.SessionStartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<ChargingSession?> GetSessionByIdAsync(Guid id)
        {
            return await _context.ChargingSessions
                .Include(cs => cs.User)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .FirstOrDefaultAsync(cs => cs.Id == id);
        }

        public async Task<ChargingSession> StartSessionAsync(Guid userId, ChargingSession session)
        {
            var spot = await _context.ChargingSpots
                .Include(s => s.ChargingSessions.Where(cs => cs.Status == ChargingSessionStatus.InProgress))
                .Include(s => s.ChargingStation)
                .FirstOrDefaultAsync(s => s.Id == session.ChargingSpotId);

            if (spot == null)
            {
                throw new InvalidOperationException("Charging spot not found.");
            }

            // Kiểm tra station status - chỉ cho phép bắt đầu sạc khi station Active
            if (spot.ChargingStation == null || spot.ChargingStation.Status != StationStatus.Active)
            {
                throw new InvalidOperationException("Trạm sạc hiện không khả dụng để bắt đầu sạc.");
            }

            if (spot.Status != SpotStatus.Available)
            {
                throw new InvalidOperationException("Charging spot is not available.");
            }

            if (spot.ChargingSessions.Any())
            {
                throw new InvalidOperationException("Charging spot already has an active session.");
            }

            session.Id = Guid.NewGuid();
            session.UserId = userId;
            session.Status = ChargingSessionStatus.InProgress;
            session.SessionStartTime = DateTime.UtcNow;
            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            // Lưu pricePerKwh từ spot nếu chưa có
            if (!session.PricePerKwh.HasValue && spot.PricePerKwh.HasValue)
            {
                session.PricePerKwh = spot.PricePerKwh.Value;
            }
            
            // Set initial SOC nếu chưa có
            if (!session.InitialSocPercentage.HasValue)
            {
                session.InitialSocPercentage = 0; // Mặc định 0%
            }
            
            // Set target SOC nếu chưa có
            if (!session.TargetSocPercentage.HasValue)
            {
                session.TargetSocPercentage = 100; // Mặc định 100%
            }

            _context.ChargingSessions.Add(session);
            spot.Status = SpotStatus.Occupied;
            spot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<ChargingSession?> CompleteSessionAsync(Guid sessionId, decimal energyDeliveredKwh, decimal cost, decimal? pricePerKwh, string? notes)
        {
            var session = await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)
                .Include(cs => cs.Reservation)
                .FirstOrDefaultAsync(cs => cs.Id == sessionId);

            if (session == null)
            {
                return null;
            }

            session.EnergyDeliveredKwh = energyDeliveredKwh;
            session.PricePerKwh = pricePerKwh ?? session.PricePerKwh;
            
            // Đảm bảo cost luôn có base fee 10k
            const decimal BASE_FEE = 10000;
            if (cost < BASE_FEE)
            {
                cost = BASE_FEE; // Ít nhất là base fee
            }
            session.Cost = cost;
            
            session.SessionEndTime = DateTime.UtcNow;
            session.Status = ChargingSessionStatus.Completed;
            session.Notes = notes ?? session.Notes;
            session.UpdatedAt = DateTime.UtcNow;

            // Cập nhật reservation status nếu có (sẽ được cập nhật thành Completed khi thanh toán thành công)
            if (session.Reservation != null)
            {
                // Chỉ cập nhật nếu chưa completed (có thể đã được cập nhật khi thanh toán)
                if (session.Reservation.Status != ReservationStatus.Completed)
                {
                    // Đánh dấu là đã check-in nếu chưa thanh toán
                    if (session.Reservation.Status == ReservationStatus.Pending || session.Reservation.Status == ReservationStatus.Confirmed)
                    {
                        session.Reservation.Status = ReservationStatus.CheckedIn;
                        session.Reservation.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            // Trả spot về Available ngay khi ngưng sạc
            if (session.ChargingSpot != null)
            {
                session.ChargingSpot.Status = SpotStatus.Available;
                session.ChargingSpot.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<ChargingSession?> UpdateSessionStatusAsync(Guid sessionId, ChargingSessionStatus status, string? notes = null)
        {
            var session = await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)
                .Include(cs => cs.User)
                .FirstOrDefaultAsync(cs => cs.Id == sessionId);

            if (session == null)
            {
                return null;
            }

            session.Status = status;
            session.Notes = notes ?? session.Notes;
            session.UpdatedAt = DateTime.UtcNow;

            // Set SessionEndTime when status changes to Completed, Cancelled, or Failed
            if (status is ChargingSessionStatus.Completed or ChargingSessionStatus.Cancelled or ChargingSessionStatus.Failed)
            {
                if (!session.SessionEndTime.HasValue)
                {
                    session.SessionEndTime = DateTime.UtcNow;
                }
                
                // Trả spot về Available
                if (session.ChargingSpot != null)
                {
                    session.ChargingSpot.Status = SpotStatus.Available;
                    session.ChargingSpot.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return session;
        }
    }
}

