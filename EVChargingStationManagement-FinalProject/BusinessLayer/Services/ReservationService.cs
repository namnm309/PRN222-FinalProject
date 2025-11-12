using System.Linq;
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

        public async Task<Reservation> CreateReservationAsync(Guid userId, Reservation reservation)
        {
            // Validate spot availability
            var spot = await _context.ChargingSpots
                .Include(s => s.Reservations)
                .Include(s => s.ChargingStation)
                .FirstOrDefaultAsync(s => s.Id == reservation.ChargingSpotId);

            if (spot == null)
            {
                throw new InvalidOperationException("Charging spot not found.");
            }

            // Kiểm tra station status - chỉ cho phép đặt lịch khi station Active
            if (spot.ChargingStation == null || spot.ChargingStation.Status != StationStatus.Active)
            {
                throw new InvalidOperationException("Trạm sạc hiện không khả dụng để đặt lịch.");
            }

            // Tính ScheduledEndTime tự động nếu không được cung cấp (mặc định +2 giờ)
            if (reservation.ScheduledEndTime == default(DateTime) || reservation.ScheduledEndTime == DateTime.MinValue)
            {
                reservation.ScheduledEndTime = reservation.ScheduledStartTime.AddHours(2);
            }

            if (!IsTimeslotAvailable(spot, reservation.ScheduledStartTime, reservation.ScheduledEndTime))
            {
                throw new InvalidOperationException("Timeslot is not available.");
            }

            reservation.Id = Guid.NewGuid();
            reservation.UserId = userId;
            reservation.Status = ReservationStatus.Pending;
            reservation.ConfirmationCode = $"RSV-{reservation.Id.ToString()[..8]}";
            reservation.CreatedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;

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

