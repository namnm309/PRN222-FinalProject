using System.Collections.Generic;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;

namespace PresentationLayer.Services
{
    public class StationHubNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<StationHub> _hubContext;
        private readonly IChargingStationService _stationService;

        public StationHubNotifier(IHubContext<StationHub> hubContext, IChargingStationService stationService)
        {
            _hubContext = hubContext;
            _stationService = stationService;
        }

        public Task NotifyReservationChangedAsync(ReservationDTO reservation)
        {
            var stationId = reservation.ChargingStationId;
            var spotId = reservation.ChargingSpotId;
            var tasks = new List<Task>
            {
                _hubContext.Clients.Group($"user-{reservation.UserId}")
                    .SendAsync("ReservationUpdated", reservation.Id),
            };

            if (stationId != Guid.Empty)
            {
                tasks.Add(_hubContext.Clients.Group($"station-{stationId}")
                    .SendAsync("ReservationUpdated", reservation.Id));
                
                // Notify that a spot has been reserved (for real-time locking)
                if (spotId != Guid.Empty)
                {
                    tasks.Add(_hubContext.Clients.Group($"station-{stationId}")
                        .SendAsync("SpotReserved", spotId, reservation.Status.ToString()));
                }
            }

            return Task.WhenAll(tasks);
        }

        public Task NotifySessionChangedAsync(ChargingSessionDTO session)
        {
            var stationId = session.ChargingStationId;
            var tasks = new List<Task>
            {
                _hubContext.Clients.Group($"user-{session.UserId}")
                    .SendAsync("SessionUpdated", session.Id),
            };

            if (stationId != Guid.Empty)
            {
                tasks.Add(_hubContext.Clients.Group($"station-{stationId}")
                    .SendAsync("SessionUpdated", session.Id));
            }

            return Task.WhenAll(tasks);
        }

        public Task NotifySpotStatusChangedAsync(ChargingSpotDTO spot)
        {
            var stationId = spot.ChargingStationId;
            return _hubContext.Clients.Group($"station-{stationId}")
                .SendAsync("SpotStatusUpdated", spot.Id, spot.Status.ToString());
        }

        public Task NotifyNotificationReceivedAsync(NotificationDTO notification)
        {
            return _hubContext.Clients.Group($"user-{notification.UserId}")
                .SendAsync("NotificationReceived", notification.Id);
        }

        public async Task NotifyStationAvailabilityChangedAsync(Guid stationId, int totalSpots, int availableSpots)
        {
            await _hubContext.Clients.Group($"station-{stationId}")
                .SendAsync("StationAvailabilityUpdated", stationId, totalSpots, availableSpots);
        }

        public async Task NotifyChargingProgressUpdatedAsync(Guid sessionId, ChargingProgressDTO progress)
        {
            await _hubContext.Clients.Group($"session-{sessionId}")
                .SendAsync("ChargingProgressUpdated", sessionId, progress);
        }

        public async Task NotifySpotsListUpdatedAsync(Guid stationId)
        {
            await _hubContext.Clients.Group($"station-{stationId}")
                .SendAsync("SpotsListUpdated", stationId);
        }

        public async Task NotifyStationStatusChangedAsync(Guid stationId, string status, string stationName)
        {
            // Broadcast cho tất cả clients (vì status change là thông tin quan trọng)
            // và cũng gửi cho group station cụ thể để đảm bảo
            await Task.WhenAll(
                _hubContext.Clients.All.SendAsync("StationStatusUpdated", stationId, status, stationName),
                _hubContext.Clients.Group($"station-{stationId}").SendAsync("StationStatusUpdated", stationId, status, stationName)
            );
        }
    }
}

