using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Utilities.Hub
{
    public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly Dictionary<int, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            if (Context.User != null)
            {
                var userIdClaim = Context.User.FindFirst("nameid"); // Sử dụng claim "nameid"
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    _userConnections[userId] = Context.ConnectionId;
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Context.User != null)
            {
                var userIdClaim = Context.User.FindFirst("nameid"); // Sử dụng claim "nameid"
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    _userConnections.Remove(userId, out _);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
