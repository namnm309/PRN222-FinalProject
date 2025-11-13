using BusinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface IRealtimeNotifier
    {
        Task NotifyReservationChangedAsync(Reservation reservation);
        Task NotifySessionChangedAsync(ChargingSession session);
        Task NotifySpotStatusChangedAsync(ChargingSpot spot);
        Task NotifyNotificationReceivedAsync(Notification notification);
        Task NotifyStationAvailabilityChangedAsync(Guid stationId, int totalSpots, int availableSpots);
        Task NotifyChargingProgressUpdatedAsync(Guid sessionId, ChargingProgressDTO progress);
        Task NotifySpotsListUpdatedAsync(Guid stationId);
        Task NotifyStationStatusChangedAsync(Guid stationId, string status, string stationName);
    }
}

