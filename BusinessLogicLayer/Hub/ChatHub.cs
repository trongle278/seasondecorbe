using BusinessLogicLayer.Interfaces;
using DataAccessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Hub
{
    public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Dictionary<int, string> _userConnections = new();
        private readonly IChatService _chatService;

        public ChatHub(IUnitOfWork unitOfWork, IChatService chatService)
        {
            _unitOfWork = unitOfWork;
            _chatService = chatService;
        }

        public async Task ConnectUser(int userId)
        {
            _userConnections[userId] = Context.ConnectionId;
        }

        public async Task SendMessage(int receiverId, string message)
        {
            var senderId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var chat = new Chat
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                SentTime = DateTime.UtcNow
            };

            await _unitOfWork.ChatRepository.InsertAsync(chat);
            await _unitOfWork.CommitAsync();

            if (_userConnections.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message);
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userId = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId != 0)
            {
                _userConnections.Remove(userId);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task MarkAsRead(int senderId)
        {
            var receiverId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);

            await _chatService.MarkMessagesAsReadAsync(receiverId, senderId);

            // Notify sender that messages were read
            if (_userConnections.TryGetValue(senderId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("MessagesRead", receiverId);
            }
        }
    }
}
