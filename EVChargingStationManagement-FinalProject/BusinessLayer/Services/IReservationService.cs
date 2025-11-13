using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IReservationService
    {
        Task<IEnumerable<Reservation>> GetReservationsForUserAsync(Guid userId, DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<Reservation>> GetReservationsForStationAsync(Guid stationId, ReservationStatus? status = null);
        Task<Reservation?> GetReservationByIdAsync(Guid id);
        Task<Reservation> CreateReservationAsync(Guid userId, CreateReservationRequest request);
        Task<Reservation?> UpdateReservationStatusAsync(Guid reservationId, ReservationStatus status, string? notes = null);
        Task<bool> CancelReservationAsync(Guid reservationId, Guid userId, string? reason = null);
        Task<IEnumerable<Reservation>> GetUpcomingReservationsAsync(Guid userId);
        Task<IEnumerable<Reservation>> GetAllReservationsForStaffAsync(DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<ChargingSpotDTO>> GetAvailableSpotsWithReservationInfoAsync(Guid stationId);
    }
}

