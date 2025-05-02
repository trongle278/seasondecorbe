using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BusinessLogicLayer.Utilities.Hub
{
    public class CallHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();
        private readonly IHubContext<CallHub> _hubContext;

        public CallHub(IHubContext<CallHub> hubContext)
        {
            _hubContext = hubContext;  // Gán giá trị cho _hubContext
        }
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null || httpContext.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new Exception("User is not authenticated!");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new Exception("User ID not found in claims!");
            }

            var userId = int.Parse(userIdClaim.Value);
            _userConnections[userId] = Context.ConnectionId;

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null || httpContext.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new Exception("User is not authenticated!");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new Exception("User ID not found in claims!");
            }

            var userId = int.Parse(userIdClaim.Value);
            _userConnections.Remove(userId, out _);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task SendOffer(string receiverConnectionId, string offer)

        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceiveOffer", Context.ConnectionId, offer);
        }

        public async Task SendAnswer(string receiverConnectionId, string answer)
        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, answer);
        }

        public async Task SendIceCandidate(string receiverConnectionId, string candidate)
        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
        }
    }
}
