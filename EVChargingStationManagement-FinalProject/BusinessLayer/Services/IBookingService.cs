using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
	public interface IBookingService
	{
		Task<BookingDTO?> GetByIdAsync(Guid id);
		Task<IEnumerable<BookingDTO>> GetByUserAsync(Guid userId);
		Task<BookingDTO> CreateAsync(Guid userId, CreateBookingRequest request);
		Task<BookingDTO?> UpdateStatusAsync(Guid bookingId, string status, string? notes = null);
		Task<bool> CancelAsync(Guid bookingId, Guid userId);
		Task<Guid> StartSessionAsync(Guid bookingId, string? qr = null);
		Task<bool> EndSessionAsync(Guid sessionId, decimal energyKwh);
		Task<Guid> PayAsync(Guid sessionId, string paymentMethod, string? referenceCode = null);
		Task<ChargingSessionDTO?> GetSessionAsync(Guid sessionId);
		Task<bool> RecordVnPayResultAsync(Guid bookingId, bool isSuccess, long amount, string? bankCode, string? vnpTxnRef, string? vnpTransactionNo);
	}
}


