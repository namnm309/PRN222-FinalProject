using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BusinessLayer.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time station and spot monitoring
    /// Allows staff to monitor charging stations and spots status in real-time
    /// </summary>
    public class StationMonitoringHub : Hub
    {
        // Track connected users and their subscribed stations
        private static readonly ConcurrentDictionary<string, HashSet<Guid>> _userStationSubscriptions = new();
        
        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var userId = Context.User?.Identity?.Name;
            
            Console.WriteLine($"[StationMonitoring] User {userId} connected with connection ID: {connectionId}");
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            _userStationSubscriptions.TryRemove(connectionId, out _);
            
            Console.WriteLine($"[StationMonitoring] Connection {connectionId} disconnected");
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to real-time updates for a specific station
        /// </summary>
        public async Task SubscribeToStation(string stationId)
        {
            if (!Guid.TryParse(stationId, out var stationGuid))
            {
                await Clients.Caller.SendAsync("Error", "Invalid station ID");
                return;
            }

            var connectionId = Context.ConnectionId;
            var groupName = $"Station_{stationId}";
            
            // Add to SignalR group
            await Groups.AddToGroupAsync(connectionId, groupName);
            
            // Track subscription
            _userStationSubscriptions.AddOrUpdate(
                connectionId,
                new HashSet<Guid> { stationGuid },
                (key, existing) =>
                {
                    existing.Add(stationGuid);
                    return existing;
                });

            Console.WriteLine($"[StationMonitoring] Connection {connectionId} subscribed to station {stationId}");
            
            await Clients.Caller.SendAsync("SubscriptionConfirmed", stationId);
        }

        /// <summary>
        /// Unsubscribe from station updates
        /// </summary>
        public async Task UnsubscribeFromStation(string stationId)
        {
            if (!Guid.TryParse(stationId, out var stationGuid))
                return;

            var connectionId = Context.ConnectionId;
            var groupName = $"Station_{stationId}";
            
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
            
            if (_userStationSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.Remove(stationGuid);
            }

            Console.WriteLine($"[StationMonitoring] Connection {connectionId} unsubscribed from station {stationId}");
        }

        /// <summary>
        /// Subscribe to all stations updates
        /// </summary>
        public async Task SubscribeToAllStations()
        {
            var connectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(connectionId, "AllStations");
            
            Console.WriteLine($"[StationMonitoring] Connection {connectionId} subscribed to all stations");
            
            await Clients.Caller.SendAsync("SubscriptionConfirmed", "all");
        }

        /// <summary>
        /// Get current subscription status
        /// </summary>
        public async Task GetSubscriptions()
        {
            var connectionId = Context.ConnectionId;
            
            if (_userStationSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                await Clients.Caller.SendAsync("CurrentSubscriptions", subscriptions.Select(s => s.ToString()).ToList());
            }
            else
            {
                await Clients.Caller.SendAsync("CurrentSubscriptions", new List<string>());
            }
        }

        /// <summary>
        /// Send heartbeat to keep connection alive
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }
    }
}

