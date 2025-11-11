using Microsoft.AspNetCore.SignalR;

namespace PresentationLayer.Hubs
{
    public class StationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeStation(Guid stationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{stationId}");
        }

        public async Task UnsubscribeStation(Guid stationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station-{stationId}");
        }

        public async Task SubscribeSession(Guid sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        }

        public async Task UnsubscribeSession(Guid sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        }
    }
}

