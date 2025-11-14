using System.Linq;
using BusinessLayer.DTOs;
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

        public async Task<IEnumerable<ChargingSessionDTO>> GetSessionsForUserAsync(Guid userId, int limit = 20)
        {
            var sessions = await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Include(cs => cs.User)
                .Where(cs => cs.UserId == userId)
                .OrderByDescending(cs => cs.SessionStartTime)
                .Take(limit)
                .ToListAsync();
            
            return sessions.Select(MapToDTO);
        }

        public async Task<IEnumerable<ChargingSessionDTO>> GetActiveSessionsAsync(Guid? stationId = null)
        {
            var query = _context.ChargingSessions
                .Include(cs => cs.User)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(cs => cs.Reservation)
                .Where(cs => cs.Status == ChargingSessionStatus.InProgress);

            if (stationId.HasValue)
            {
                query = query.Where(cs => cs.ChargingSpot != null && cs.ChargingSpot.ChargingStationId == stationId.Value);
            }

            var sessions = await query.ToListAsync();
            return sessions.Select(MapToDTO);
        }

        public async Task<ChargingSessionDTO?> GetActiveSessionForUserAsync(Guid userId)
        {
            var session = await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Include(cs => cs.User)
                .Where(cs => cs.UserId == userId && cs.Status == ChargingSessionStatus.InProgress)
                .OrderByDescending(cs => cs.SessionStartTime)
                .FirstOrDefaultAsync();
            
            return session == null ? null : MapToDTO(session);
        }

        public async Task<ChargingSessionDTO?> GetSessionByIdAsync(Guid id)
        {
            var session = await _context.ChargingSessions
                .Include(cs => cs.User)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .FirstOrDefaultAsync(cs => cs.Id == id);
            
            return session == null ? null : MapToDTO(session);
        }

        public async Task<ChargingSessionDTO> StartSessionAsync(Guid userId, StartChargingSessionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var spot = await _context.ChargingSpots
                .Include(s => s.ChargingSessions.Where(cs => cs.Status == ChargingSessionStatus.InProgress))
                .Include(s => s.ChargingStation)
                .FirstOrDefaultAsync(s => s.Id == request.ChargingSpotId);

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

            var session = new ChargingSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ChargingSpotId = request.ChargingSpotId,
                ReservationId = request.ReservationId,
                VehicleId = request.VehicleId,
                EnergyRequestedKwh = request.EnergyRequestedKwh,
                TargetSocPercentage = request.TargetSocPercentage,
                QrCodeScanned = request.QrCode,
                Notes = request.Notes,
                Status = ChargingSessionStatus.InProgress,
                SessionStartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Lưu pricePerKwh từ request hoặc từ spot
            if (request.PricePerKwh.HasValue)
            {
                session.PricePerKwh = request.PricePerKwh.Value;
            }
            else if (spot.PricePerKwh.HasValue)
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
            
            // Reload với navigation properties
            var createdSession = await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Include(cs => cs.User)
                .FirstOrDefaultAsync(cs => cs.Id == session.Id);
            
            return MapToDTO(createdSession!);
        }

        public async Task<ChargingSessionDTO?> CompleteSessionAsync(Guid sessionId, decimal energyDeliveredKwh, decimal cost, decimal? pricePerKwh, string? notes)
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
            
            // Reload với navigation properties
            var completedSession = await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Include(cs => cs.User)
                .FirstOrDefaultAsync(cs => cs.Id == sessionId);
            
            return completedSession == null ? null : MapToDTO(completedSession);
        }

        public async Task<ChargingSessionDTO?> UpdateSessionStatusAsync(Guid sessionId, ChargingSessionStatus status, string? notes = null)
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
            
            // Reload với navigation properties
            var updatedSession = await _context.ChargingSessions
                .Include(cs => cs.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(cs => cs.Vehicle)
                .Include(cs => cs.Reservation)
                .Include(cs => cs.User)
                .FirstOrDefaultAsync(cs => cs.Id == sessionId);
            
            return updatedSession == null ? null : MapToDTO(updatedSession);
        }

        private ChargingSessionDTO MapToDTO(ChargingSession session)
        {
            return new ChargingSessionDTO
            {
                Id = session.Id,
                ChargingSpotId = session.ChargingSpotId,
                ChargingSpotNumber = session.ChargingSpot?.SpotNumber,
                ChargingStationId = session.ChargingSpot?.ChargingStationId ?? Guid.Empty,
                ChargingStationName = session.ChargingSpot?.ChargingStation?.Name,
                UserId = session.UserId,
                UserName = session.User != null ? (session.User.FullName ?? session.User.Email) : null,
                VehicleId = session.VehicleId,
                VehicleName = session.Vehicle != null ? $"{session.Vehicle.Make} {session.Vehicle.Model}" : null,
                ReservationId = session.ReservationId,
                ScheduledStartTime = session.Reservation?.ScheduledStartTime,
                ScheduledEndTime = session.Reservation?.ScheduledEndTime,
                Status = session.Status,
                SessionStartTime = session.SessionStartTime,
                SessionEndTime = session.SessionEndTime,
                EnergyDeliveredKwh = session.EnergyDeliveredKwh,
                EnergyRequestedKwh = session.EnergyRequestedKwh,
                Cost = session.Cost,
                PricePerKwh = session.PricePerKwh,
                ChargingSpotPower = session.ChargingSpot?.PowerOutput,
                ExternalSessionId = session.ExternalSessionId,
                Notes = session.Notes
            };
        }
    }
}

