using BusinessLayer.DTOs;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IReservationService
    {
        Task<IEnumerable<ReservationDTO>> GetReservationsForUserAsync(Guid userId, DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<ReservationDTO>> GetReservationsForStationAsync(Guid stationId, ReservationStatus? status = null);
        Task<ReservationDTO?> GetReservationByIdAsync(Guid id);
        Task<ReservationDTO> CreateReservationAsync(Guid userId, CreateReservationRequest request);
        Task<ReservationDTO?> UpdateReservationStatusAsync(Guid reservationId, ReservationStatus status, string? notes = null);
        Task<bool> CancelReservationAsync(Guid reservationId, Guid userId, string? reason = null);
        Task<IEnumerable<ReservationDTO>> GetUpcomingReservationsAsync(Guid userId);
        Task<IEnumerable<ReservationDTO>> GetAllReservationsForStaffAsync(DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<ChargingSpotDTO>> GetAvailableSpotsWithReservationInfoAsync(Guid stationId);
    }
}

