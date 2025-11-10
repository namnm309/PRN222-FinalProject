using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BusinessLayer.Services
{
	public class BookingService : IBookingService
	{
		private readonly EVDbContext _context;
		private readonly IConfiguration _config;

		public BookingService(EVDbContext context, IConfiguration config)
		{
			_context = context;
			_config = config;
		}

		public async Task<ChargingSessionDTO?> GetSessionAsync(Guid sessionId)
		{
			var s = await _context.ChargingSessions.FirstOrDefaultAsync(x => x.Id == sessionId);
			if (s == null) return null;
			return new ChargingSessionDTO
			{
				Id = s.Id,
				BookingId = s.BookingId,
				ChargingStationId = s.ChargingStationId,
				ChargingSpotId = s.ChargingSpotId,
				StartedAt = s.StartedAt,
				EndedAt = s.EndedAt,
				EnergyKwh = s.EnergyKwh,
				PricePerKwh = s.PricePerKwh,
				TotalAmount = s.TotalAmount,
				Status = s.Status
			};
		}

		public async Task<BookingDTO?> GetByIdAsync(Guid id)
		{
			var entity = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
			return entity?.ToDTO();
		}

		public async Task<IEnumerable<BookingDTO>> GetByUserAsync(Guid userId)
		{
			var list = await _context.Bookings
				.Where(b => b.UserId == userId)
				.OrderByDescending(b => b.StartTime)
				.ToListAsync();
			return list.Select(b => b.ToDTO());
		}

		public async Task<BookingDTO> CreateAsync(Guid userId, CreateBookingRequest request)
		{
			// Validate station & spot
			var spot = await _context.ChargingSpots
				.Include(s => s.ChargingStation)
				.FirstOrDefaultAsync(s => s.Id == request.ChargingSpotId && s.ChargingStationId == request.ChargingStationId);
			if (spot == null)
				throw new InvalidOperationException("Charging spot not found in the specified station");

			// Validate vehicle ownership
			var vehicleExists = await _context.Vehicles.AnyAsync(v => v.Id == request.VehicleId && v.UserId == userId);
			if (!vehicleExists)
				throw new InvalidOperationException("Vehicle does not belong to the user or not found");

			// Validate time
			if (request.EndTime <= request.StartTime)
				throw new InvalidOperationException("EndTime must be after StartTime");

			// Normalize to UTC
			var startUtc = request.StartTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc) : request.StartTime.ToUniversalTime();
			var endUtc = request.EndTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc) : request.EndTime.ToUniversalTime();

			// Check for overlapping bookings on the same spot
			var overlaps = await _context.Bookings.AnyAsync(b =>
				b.ChargingSpotId == request.ChargingSpotId &&
				b.Status != "Cancelled" &&
				((startUtc < b.EndTime) && (endUtc > b.StartTime)));
			if (overlaps)
				throw new InvalidOperationException("Time slot overlaps with an existing booking for this spot");

			// Optional: ensure spot is available
			if (spot.Status == SpotStatus.Occupied || spot.Status == SpotStatus.Maintenance)
				throw new InvalidOperationException("Charging spot is not available");

			var entity = new Booking
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				VehicleId = request.VehicleId,
				ChargingStationId = request.ChargingStationId,
				ChargingSpotId = request.ChargingSpotId,
				StartTime = startUtc,
				EndTime = endUtc,
				Status = "Pending",
				Notes = request.Notes,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_context.Bookings.Add(entity);
			await _context.SaveChangesAsync();

			return entity.ToDTO();
		}

		public async Task<BookingDTO?> UpdateStatusAsync(Guid bookingId, string status, string? notes = null)
		{
			var entity = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
			if (entity == null) return null;

			entity.Status = status;
			if (!string.IsNullOrWhiteSpace(notes)) entity.Notes = notes;
			entity.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return entity.ToDTO();
		}

		public async Task<bool> CancelAsync(Guid bookingId, Guid userId)
		{
			var entity = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
			if (entity == null) return false;
			if (entity.Status == "Cancelled") return true;
			entity.Status = "Cancelled";
			entity.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<Guid> StartSessionAsync(Guid bookingId, string? qr = null)
		{
			var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
			if (booking == null) throw new InvalidOperationException("Booking not found");
			if (booking.Status == "Cancelled") throw new InvalidOperationException("Booking has been cancelled");
			// Require prepay if configured
			var requirePrepay = _config.GetValue<bool>("Payments:RequirePrepay", false);
			if (requirePrepay)
			{
				var paid = await _context.BookingPayments.AnyAsync(p => p.BookingId == bookingId && p.Status == "Succeeded");
				if (!paid) throw new InvalidOperationException("Booking has not been paid yet");
			}

			// Optional: validate QR maps to spotId or stationId; for now we trust the booking's spot
			var spot = await _context.ChargingSpots.FirstOrDefaultAsync(s => s.Id == booking.ChargingSpotId);
			if (spot == null) throw new InvalidOperationException("Charging spot not found");

			// Mark spot occupied
			spot.Status = SpotStatus.Occupied;
			spot.UpdatedAt = DateTime.UtcNow;

			var session = new ChargingSession
			{
				Id = Guid.NewGuid(),
				BookingId = booking.Id,
				ChargingStationId = booking.ChargingStationId,
				ChargingSpotId = booking.ChargingSpotId,
				StartedAt = DateTime.UtcNow,
				Status = "InProgress",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			_context.ChargingSessions.Add(session);
			booking.Status = "Confirmed";
			booking.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return session.Id;
		}

		public async Task<bool> EndSessionAsync(Guid sessionId, decimal energyKwh)
		{
			var session = await _context.ChargingSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) return false;
			var spot = await _context.ChargingSpots.FirstOrDefaultAsync(s => s.Id == session.ChargingSpotId);
			if (spot == null) return false;

			session.EndedAt = DateTime.UtcNow;
			session.EnergyKwh = energyKwh;
			session.PricePerKwh = spot.PricePerKwh ?? 0m;
			session.TotalAmount = Math.Round((session.PricePerKwh ?? 0m) * energyKwh, 2);
			session.Status = "Completed";
			session.UpdatedAt = DateTime.UtcNow;

			// Free the spot
			spot.Status = SpotStatus.Available;
			spot.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<Guid> PayAsync(Guid sessionId, string paymentMethod, string? referenceCode = null)
		{
			var session = await _context.ChargingSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
			if (session == null) throw new InvalidOperationException("Session not found");
			if (session.TotalAmount == null) throw new InvalidOperationException("Session has no amount to pay");

			var trx = new Transaction
			{
				Id = Guid.NewGuid(),
				ChargingSessionId = sessionId,
				PaymentMethod = paymentMethod,
				Status = "Succeeded",
				Amount = session.TotalAmount.Value,
				PaidAt = DateTime.UtcNow,
				ReferenceCode = referenceCode,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			_context.Transactions.Add(trx);
			await _context.SaveChangesAsync();
			return trx.Id;
		}

		public async Task<bool> RecordVnPayResultAsync(Guid bookingId, bool isSuccess, long amount, string? bankCode, string? vnpTxnRef, string? vnpTransactionNo)
		{
			// Idempotent: update if exists by VnpTxnRef or BookingId with same TxnRef
			var existing = await _context.BookingPayments
				.FirstOrDefaultAsync(p => (vnpTxnRef != null && p.VnpTxnRef == vnpTxnRef) || p.BookingId == bookingId);

			if (existing == null)
			{
				existing = new BookingPayment
				{
					Id = Guid.NewGuid(),
					BookingId = bookingId,
					Amount = amount,
					Provider = "VNPay",
					VnpTxnRef = vnpTxnRef,
					VnpTransactionNo = vnpTransactionNo,
					BankCode = bankCode,
					Status = isSuccess ? "Succeeded" : "Failed",
					PaidAt = isSuccess ? DateTime.UtcNow : null,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};
				_context.BookingPayments.Add(existing);
			}
			else
			{
				existing.Amount = amount;
				existing.BankCode = bankCode ?? existing.BankCode;
				existing.VnpTxnRef = vnpTxnRef ?? existing.VnpTxnRef;
				existing.VnpTransactionNo = vnpTransactionNo ?? existing.VnpTransactionNo;
				existing.Status = isSuccess ? "Succeeded" : "Failed";
				existing.PaidAt = isSuccess ? (existing.PaidAt ?? DateTime.UtcNow) : existing.PaidAt;
				existing.UpdatedAt = DateTime.UtcNow;
			}
			await _context.SaveChangesAsync();
			return true;
		}
	}
}


