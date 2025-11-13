using System.Linq;
using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ReservationService : IReservationService
    {
        private readonly EVDbContext _context;

        public ReservationService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reservation>> GetReservationsForUserAsync(Guid userId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Reservations
                .Include(r => r.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Include(r => r.Vehicle)
                .Where(r => r.UserId == userId);

            if (from.HasValue)
            {
                query = query.Where(r => r.ScheduledEndTime >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(r => r.ScheduledStartTime <= to.Value);
            }

            return await query
                .OrderByDescending(r => r.ScheduledStartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsForStationAsync(Guid stationId, ReservationStatus? status = null)
        {
            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .Include(r => r.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Where(r => r.ChargingSpot != null && r.ChargingSpot.ChargingStationId == stationId);

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            return await query
                .OrderBy(r => r.ScheduledStartTime)
                .ToListAsync();
        }

        public async Task<Reservation?> GetReservationByIdAsync(Guid id)
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .Include(r => r.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reservation> CreateReservationAsync(Guid userId, CreateReservationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate spot availability
            var spot = await _context.ChargingSpots
                .Include(s => s.Reservations)
                .Include(s => s.ChargingStation)
                .FirstOrDefaultAsync(s => s.Id == request.ChargingSpotId);

            if (spot == null)
            {
                throw new InvalidOperationException("Charging spot not found.");
            }

            // Kiểm tra station status - chỉ cho phép đặt lịch khi station Active
            if (spot.ChargingStation == null || spot.ChargingStation.Status != StationStatus.Active)
            {
                throw new InvalidOperationException("Trạm sạc hiện không khả dụng để đặt lịch.");
            }

            var scheduledStartTime = request.ScheduledStartTime.ToUniversalTime();
            var scheduledEndTime = request.ScheduledEndTime.HasValue 
                ? request.ScheduledEndTime.Value.ToUniversalTime() 
                : scheduledStartTime.AddHours(2);

            if (!IsTimeslotAvailable(spot, scheduledStartTime, scheduledEndTime))
            {
                throw new InvalidOperationException("Timeslot is not available.");
            }

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ChargingSpotId = request.ChargingSpotId,
                VehicleId = request.VehicleId,
                ScheduledStartTime = scheduledStartTime,
                ScheduledEndTime = scheduledEndTime,
                EstimatedEnergyKwh = request.EstimatedEnergyKwh,
                EstimatedCost = request.EstimatedCost,
                IsPrepaid = request.IsPrepaid,
                Notes = request.Notes,
                Status = ReservationStatus.Pending,
                ConfirmationCode = $"RSV-{Guid.NewGuid().ToString()[..8]}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<Reservation?> UpdateReservationStatusAsync(Guid reservationId, ReservationStatus status, string? notes = null)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null)
            {
                return null;
            }

            reservation.Status = status;
            reservation.Notes = notes ?? reservation.Notes;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<bool> CancelReservationAsync(Guid reservationId, Guid userId, string? reason = null)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);

            if (reservation == null || reservation.UserId != userId)
            {
                return false;
            }

            if (reservation.Status is ReservationStatus.Completed or ReservationStatus.Cancelled)
            {
                return false;
            }

            reservation.Status = ReservationStatus.Cancelled;
            reservation.Notes = string.IsNullOrWhiteSpace(reason) ? reservation.Notes : $"{reservation.Notes}\nCancelled: {reason}";
            reservation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Reservation>> GetUpcomingReservationsAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            return await _context.Reservations
                .Include(r => r.ChargingSpot)!
                    .ThenInclude(s => s.ChargingStation)
                .Where(r => r.UserId == userId && r.ScheduledStartTime >= now)
                .OrderBy(r => r.ScheduledStartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetAllReservationsForStaffAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.ChargingSpot)
                    .ThenInclude(s => s!.ChargingStation)
                .Include(r => r.Vehicle)
                .AsQueryable();

            // Apply date filters if provided
            if (from.HasValue)
            {
                query = query.Where(r => r.ScheduledStartTime >= from.Value.ToUniversalTime());
            }

            if (to.HasValue)
            {
                query = query.Where(r => r.ScheduledStartTime <= to.Value.ToUniversalTime());
            }

            return await query
                .OrderByDescending(r => r.ScheduledStartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingSpotDTO>> GetAvailableSpotsWithReservationInfoAsync(Guid stationId)
        {
            var now = DateTime.UtcNow;
            var spots = await _context.ChargingSpots
                .Include(s => s.ChargingStation)
                .Where(s => s.ChargingStationId == stationId)
                .ToListAsync();

            var spotIds = spots.Select(s => s.Id).ToList();
            
            // Get all active reservations for these spots
            var activeReservations = await _context.Reservations
                .Where(r => spotIds.Contains(r.ChargingSpotId) &&
                           (r.Status == ReservationStatus.Pending ||
                            r.Status == ReservationStatus.Confirmed ||
                            r.Status == ReservationStatus.CheckedIn) &&
                           r.ScheduledEndTime > now)
                .Select(r => r.ChargingSpotId)
                .Distinct()
                .ToListAsync();
            
            // Get active sessions
            var activeSessions = await _context.ChargingSessions
                .Where(cs => spotIds.Contains(cs.ChargingSpotId) && 
                            cs.Status == ChargingSessionStatus.InProgress)
                .Select(cs => cs.ChargingSpotId)
                .Distinct()
                .ToListAsync();
            
            return spots.Select(s => new ChargingSpotDTO
            {
                Id = s.Id,
                SpotNumber = s.SpotNumber,
                ChargingStationId = s.ChargingStationId,
                ChargingStationName = s.ChargingStation?.Name,
                Status = s.Status,
                ConnectorType = s.ConnectorType,
                PowerOutput = s.PowerOutput,
                PricePerKwh = s.PricePerKwh,
                Description = s.Description,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsReserved = activeReservations.Contains(s.Id),
                IsAvailable = s.Status == SpotStatus.Available && 
                             !activeReservations.Contains(s.Id) && 
                             !activeSessions.Contains(s.Id)
            });
        }

        private static bool IsTimeslotAvailable(ChargingSpot spot, DateTime start, DateTime end)
        {
            return spot.Reservations.All(existing =>
                existing.Status == ReservationStatus.Cancelled ||
                existing.Status == ReservationStatus.Completed ||
                end <= existing.ScheduledStartTime ||
                start >= existing.ScheduledEndTime);
        }
    }
}

