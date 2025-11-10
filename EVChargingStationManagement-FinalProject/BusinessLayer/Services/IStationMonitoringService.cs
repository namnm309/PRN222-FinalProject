using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    /// <summary>
    /// Service for broadcasting real-time station and spot status updates via SignalR
    /// </summary>
    public interface IStationMonitoringService
    {
        /// <summary>
        /// Broadcast spot status change to all connected clients
        /// </summary>
        Task BroadcastSpotStatusChange(Guid spotId, Guid stationId, int newStatus, string spotNumber);

        /// <summary>
        /// Broadcast station status change to all connected clients
        /// </summary>
        Task BroadcastStationStatusChange(Guid stationId, int newStatus, string stationName);

        /// <summary>
        /// Broadcast new charging session started
        /// </summary>
        Task BroadcastSessionStarted(Guid sessionId, Guid spotId, Guid stationId, string userName);

        /// <summary>
        /// Broadcast charging session ended
        /// </summary>
        Task BroadcastSessionEnded(Guid sessionId, Guid spotId, Guid stationId, decimal energyConsumed);

        /// <summary>
        /// Broadcast station statistics update
        /// </summary>
        Task BroadcastStatsUpdate(int availableSpots, int occupiedSpots, int maintenanceSpots, int offlineSpots);

        /// <summary>
        /// Broadcast new error reported
        /// </summary>
        Task BroadcastErrorReported(Guid errorId, Guid stationId, string errorTitle, string severity);

        /// <summary>
        /// Broadcast maintenance scheduled
        /// </summary>
        Task BroadcastMaintenanceScheduled(Guid maintenanceId, Guid stationId, DateTime scheduledDate, string title);

        /// <summary>
        /// Send real-time alert to specific user group
        /// </summary>
        Task SendAlertToStaff(string message, string severity, Dictionary<string, object>? data = null);
    }
}

