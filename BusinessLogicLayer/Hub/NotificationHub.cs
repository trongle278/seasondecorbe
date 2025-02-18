using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Hub
{
    public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly Dictionary<int, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            // Lấy userId từ token/claim
            var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _userConnections[userId] = Context.ConnectionId;

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _userConnections.Remove(userId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
