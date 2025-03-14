using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
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

        public ChatHub(IUnitOfWork unitOfWork, IChatService chatService)
        {
            _chatService = chatService;
            _unitOfWork = unitOfWork;
        }

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

        // Gửi tin nhắn + file (base64)
        public async Task SendMessage(int receiverId, string message)
        {
            if (Context.User != null)
            {
                var senderIdClaim = Context.User.FindFirst("nameid");
                if (senderIdClaim != null && int.TryParse(senderIdClaim.Value, out var senderId))
                {
                    // Tạo entity Chat
                    var chat = new Chat
                    {
                        SenderId = senderId,
                        ReceiverId = receiverId,
                        Message = message,
                        SentTime = DateTime.UtcNow,
                        IsRead = false
                    };

                    // Lưu tin nhắn vào database
                    await _unitOfWork.ChatRepository.InsertAsync(chat);
                    await _unitOfWork.CommitAsync();

                    // Gửi tin nhắn real-time đến receiver (thằng B)
                    if (_userConnections.TryGetValue(receiverId, out var receiverConn))
                    {
                        await Clients.Client(receiverConn).SendAsync("ReceiveMessage", new ChatMessageResponse
                        {
                            Id = chat.Id,
                            SenderId = chat.SenderId,
                            ReceiverId = chat.ReceiverId,
                            Message = chat.Message,
                            SentTime = chat.SentTime,
                            IsRead = chat.IsRead
                        });
                    }

                    // Gửi phản hồi cho sender (thằng A)
                    await Clients.Caller.SendAsync("MessageSent", new ChatMessageResponse
                    {
                        Id = chat.Id,
                        SenderId = chat.SenderId,
                        ReceiverId = chat.ReceiverId,
                        Message = chat.Message,
                        SentTime = chat.SentTime,
                        IsRead = chat.IsRead
                    });
                }
                else
                {
                    throw new InvalidOperationException("Sender ID not found in token.");
                }
            }
            else
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
        }


        // Đánh dấu tin nhắn đã đọc
        public async Task MarkAsRead(int senderId)
        {
            if (Context.User != null)
            {
                var receiverIdClaim = Context.User.FindFirst("nameid"); // Sử dụng claim "nameid"
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
