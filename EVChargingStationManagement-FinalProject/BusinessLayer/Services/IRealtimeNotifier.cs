using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface IRealtimeNotifier
    {
        Task NotifyReservationChangedAsync(ReservationDTO reservation);
        Task NotifySessionChangedAsync(ChargingSessionDTO session);
        Task NotifySpotStatusChangedAsync(ChargingSpotDTO spot);
        Task NotifyNotificationReceivedAsync(NotificationDTO notification);
        Task NotifyStationAvailabilityChangedAsync(Guid stationId, int totalSpots, int availableSpots);
        Task NotifyChargingProgressUpdatedAsync(Guid sessionId, ChargingProgressDTO progress);
        Task NotifySpotsListUpdatedAsync(Guid stationId);
        Task NotifyStationStatusChangedAsync(Guid stationId, string status, string stationName);
    }
}

