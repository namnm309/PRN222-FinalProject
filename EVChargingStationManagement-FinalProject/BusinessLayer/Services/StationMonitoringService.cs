using BusinessLayer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BusinessLayer.Services
{
    /// <summary>
    /// Implementation of station monitoring service using SignalR
    /// Broadcasts real-time updates to connected clients
    /// </summary>
    public class StationMonitoringService : IStationMonitoringService
    {
        private readonly IHubContext<StationMonitoringHub> _hubContext;

        public StationMonitoringService(IHubContext<StationMonitoringHub> hubContext)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public async Task BroadcastSpotStatusChange(Guid spotId, Guid stationId, int newStatus, string spotNumber)
        {
            var update = new
            {
                SpotId = spotId,
                StationId = stationId,
                Status = newStatus,
                SpotNumber = spotNumber,
                StatusText = GetSpotStatusText(newStatus),
                Timestamp = DateTime.UtcNow
            };

            // Send to all clients monitoring this station
            await _hubContext.Clients.Group($"Station_{stationId}")
                .SendAsync("SpotStatusChanged", update);

            // Also send to all clients monitoring all stations
            await _hubContext.Clients.Group("AllStations")
                .SendAsync("SpotStatusChanged", update);

            Console.WriteLine($"[Monitoring] Spot {spotNumber} status changed to {GetSpotStatusText(newStatus)}");
        }

        public async Task BroadcastStationStatusChange(Guid stationId, int newStatus, string stationName)
        {
            var update = new
            {
                StationId = stationId,
                Status = newStatus,
                StationName = stationName,
                StatusText = GetStationStatusText(newStatus),
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"Station_{stationId}")
                .SendAsync("StationStatusChanged", update);

            await _hubContext.Clients.Group("AllStations")
                .SendAsync("StationStatusChanged", update);

            Console.WriteLine($"[Monitoring] Station {stationName} status changed to {GetStationStatusText(newStatus)}");
        }

        public async Task BroadcastSessionStarted(Guid sessionId, Guid spotId, Guid stationId, string userName)
        {
            var notification = new
            {
                SessionId = sessionId,
                SpotId = spotId,
                StationId = stationId,
                UserName = userName,
                Type = "SessionStarted",
                Message = $"Phiên sạc mới bắt đầu bởi {userName}",
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"Station_{stationId}")
                .SendAsync("SessionStarted", notification);

            await _hubContext.Clients.Group("AllStations")
                .SendAsync("SessionStarted", notification);

            Console.WriteLine($"[Monitoring] Session started at station {stationId} by {userName}");
        }

        public async Task BroadcastSessionEnded(Guid sessionId, Guid spotId, Guid stationId, decimal energyConsumed)
        {
            var notification = new
            {
                SessionId = sessionId,
                SpotId = spotId,
                StationId = stationId,
                EnergyConsumed = energyConsumed,
                Type = "SessionEnded",
                Message = $"Phiên sạc kết thúc - {energyConsumed:F2} kWh",
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"Station_{stationId}")
                .SendAsync("SessionEnded", notification);

            await _hubContext.Clients.Group("AllStations")
                .SendAsync("SessionEnded", notification);

            Console.WriteLine($"[Monitoring] Session ended at station {stationId}, energy: {energyConsumed} kWh");
        }

        public async Task BroadcastStatsUpdate(int availableSpots, int occupiedSpots, int maintenanceSpots, int offlineSpots)
        {
            var stats = new
            {
                Available = availableSpots,
                Occupied = occupiedSpots,
                Maintenance = maintenanceSpots,
                Offline = offlineSpots,
                Total = availableSpots + occupiedSpots + maintenanceSpots + offlineSpots,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("AllStations")
                .SendAsync("StatsUpdated", stats);

            Console.WriteLine($"[Monitoring] Stats updated - Available: {availableSpots}, Occupied: {occupiedSpots}");
        }

        public async Task BroadcastErrorReported(Guid errorId, Guid stationId, string errorTitle, string severity)
        {
            var alert = new
            {
                ErrorId = errorId,
                StationId = stationId,
                Title = errorTitle,
                Severity = severity,
                Type = "ErrorReported",
                Message = $"Lỗi mới: {errorTitle}",
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"Station_{stationId}")
                .SendAsync("ErrorReported", alert);

            await _hubContext.Clients.Group("AllStations")
                .SendAsync("ErrorReported", alert);

            // Send urgent alert to all staff if critical
            if (severity?.ToLower() == "critical" || severity?.ToLower() == "high")
            {
                await SendAlertToStaff($"⚠️ LỖI NGHIÊM TRỌNG: {errorTitle}", "critical", new Dictionary<string, object>
                {
                    { "errorId", errorId },
                    { "stationId", stationId },
                    { "severity", severity }
                });
            }

            Console.WriteLine($"[Monitoring] Error reported at station {stationId}: {errorTitle} ({severity})");
        }

        public async Task BroadcastMaintenanceScheduled(Guid maintenanceId, Guid stationId, DateTime scheduledDate, string title)
        {
            var notification = new
            {
                MaintenanceId = maintenanceId,
                StationId = stationId,
                ScheduledDate = scheduledDate,
                Title = title,
                Type = "MaintenanceScheduled",
                Message = $"Bảo trì được lên lịch: {title}",
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"Station_{stationId}")
                .SendAsync("MaintenanceScheduled", notification);

            await _hubContext.Clients.Group("AllStations")
                .SendAsync("MaintenanceScheduled", notification);

            Console.WriteLine($"[Monitoring] Maintenance scheduled at station {stationId}: {title}");
        }

        public async Task SendAlertToStaff(string message, string severity, Dictionary<string, object>? data = null)
        {
            var alert = new
            {
                Message = message,
                Severity = severity,
                Data = data ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };

            // Send to all connected staff members
            await _hubContext.Clients.Group("AllStations")
                .SendAsync("StaffAlert", alert);

            Console.WriteLine($"[Monitoring] Alert sent to staff: {message} ({severity})");
        }

        private static string GetSpotStatusText(int status)
        {
            return status switch
            {
                0 => "Sẵn sàng",
                1 => "Đang sử dụng",
                2 => "Bảo trì",
                3 => "Không hoạt động",
                _ => "Unknown"
            };
        }

        private static string GetStationStatusText(int status)
        {
            return status switch
            {
                0 => "Hoạt động",
                1 => "Không hoạt động",
                2 => "Bảo trì",
                _ => "Unknown"
            };
        }
    }
}

