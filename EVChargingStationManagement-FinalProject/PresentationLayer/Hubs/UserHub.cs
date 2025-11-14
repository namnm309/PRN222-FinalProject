using Microsoft.AspNetCore.SignalR;

namespace PresentationLayer.Hubs
{
    public class UserHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }

            // Thêm admin vào group admin để nhận thông báo
            var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role == "Admin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin-group");
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

            var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role == "Admin")
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin-group");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}

