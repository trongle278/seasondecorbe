using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Repository.UnitOfWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Utilities.Hub
{
    public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();
        private readonly IChatService _chatService;
        private readonly IContactService _contactService;

        public ChatHub(IUnitOfWork unitOfWork, IChatService chatService, ICloudinaryService cloudinaryService, IContactService contactService)
        {
            _chatService = chatService;
            _contactService = contactService;
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

        public async Task SendMessage(int receiverId, string message, List<Base64FileDto> files)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext?.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new HubException("User is not authenticated.");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var senderId))
            {
                throw new HubException("Invalid sender ID.");
            }

            var chatRequest = new ChatMessageRequest
            {
                ReceiverId = receiverId,
                Message = message,
                Files = files
            };

            var chatMessage = await _chatService.SendMessageAsync(senderId, chatRequest);

            if (_userConnections.TryGetValue(receiverId, out var receiverConn))
            {
                await Clients.Client(receiverConn).SendAsync("ReceiveMessage", chatMessage);
            }

            await Clients.Caller.SendAsync("MessageSent", chatMessage);
        }

        public async Task MarkAsRead(int senderId)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext?.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new HubException("User is not authenticated.");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var receiverId))
            {
                throw new HubException("Invalid receiver ID.");
            }

            var response = await _chatService.MarkMessagesAsReadAsync(receiverId, senderId);

            if (response.Success && _userConnections.TryGetValue(senderId, out var senderConn))
            {
                await Clients.Client(senderConn).SendAsync("MessagesRead", receiverId);
            }
        }

        public async Task UpdateContacts()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext?.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new HubException("User is not authenticated.");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                throw new HubException("Invalid user ID.");
            }

            var response = await _contactService.GetAllContactsAsync(userId);

            if (response.Success)
            {
                await Clients.Caller.SendAsync("ContactsUpdated", response.Data);
            }
        }
    }
}
