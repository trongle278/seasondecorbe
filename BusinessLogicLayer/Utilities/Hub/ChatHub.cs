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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Utilities.Hub
{
    public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly Dictionary<int, string> _userConnections = new();
        private readonly IChatService _chatService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public ChatHub(IUnitOfWork unitOfWork, IChatService chatService, ICloudinaryService cloudinaryService)
        {
            _chatService = chatService;
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
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

        public async Task SendMessage(int receiverId, string message, IEnumerable<IFormFile> files)
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
                Message = message
            };

            // Gọi ChatService để xử lý gửi tin nhắn
            var chatMessage = await _chatService.SendMessageAsync(senderId, chatRequest, files);

            // Gửi tin nhắn real-time đến receiver nếu họ đang online
            if (_userConnections.TryGetValue(receiverId, out var receiverConn))
            {
                await Clients.Client(receiverConn).SendAsync("ReceiveMessage", chatMessage);
            }

            // Gửi phản hồi cho sender
            await Clients.Caller.SendAsync("MessageSent", chatMessage);
        }

        // Đánh dấu tin nhắn đã đọc
        public async Task MarkAsRead(int senderId)
        {
            if (Context.User != null)
            {
                var receiverIdClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (receiverIdClaim != null && int.TryParse(receiverIdClaim.Value, out var receiverId))
                {
                    await _chatService.MarkMessagesAsReadAsync(receiverId, senderId);

                    // Thông báo realtime cho sender
                    if (_userConnections.TryGetValue(senderId, out var senderConn))
                    {
                        await Clients.Client(senderConn).SendAsync("MessagesRead", receiverId);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Receiver ID not found in token.");
                }
            }
            else
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
        }
    }
}
