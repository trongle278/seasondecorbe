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

namespace BusinessLogicLayer.Hub
{
    public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly Dictionary<int, string> _userConnections = new();
        private readonly IChatService _chatService;

        public ChatHub(IUnitOfWork unitOfWork, IChatService chatService)
        {
            _chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            // Lấy userId từ token/claim
            var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _userConnections[userId] = Context.ConnectionId;

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _userConnections.Remove(userId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        // Gửi tin nhắn + file (base64)
        public async Task SendMessageWithFiles(ChatMessageRequest request)
        {
            // request chứa {ReceiverId, Message, List<Base64FileDto> Files}
            var senderId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Chuyển Base64 -> IFormFile
            var formFiles = new List<IFormFile>();
            foreach (var base64File in request.Files)
            {
                byte[] fileBytes = System.Convert.FromBase64String(base64File.Base64Content);
                var stream = new MemoryStream(fileBytes);

                var formFile = new FormFile(stream, 0, fileBytes.Length, base64File.FileName, base64File.FileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = base64File.ContentType ?? "application/octet-stream"
                };
                formFiles.Add(formFile);
            }

            // Gọi service lưu tin nhắn + upload file
            ChatMessageResponse savedMsg = await _chatService.SendMessageWithFilesAsync(senderId, request, formFiles);

            // Bắn realtime cho receiver
            if (_userConnections.TryGetValue(savedMsg.ReceiverId, out var receiverConn))
            {
                await Clients.Client(receiverConn).SendAsync("ReceiveMessage", savedMsg);
            }

            // Optionally, trả kết quả cho chính sender (nếu muốn)
            await Clients.Caller.SendAsync("MessageSent", savedMsg);
        }


        // Đánh dấu tin nhắn đã đọc
        public async Task MarkAsRead(int senderId)
        {
            var receiverId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _chatService.MarkMessagesAsReadAsync(receiverId, senderId);

            // Thông báo realtime cho sender
            if (_userConnections.TryGetValue(senderId, out var senderConn))
            {
                await Clients.Client(senderConn).SendAsync("MessagesRead", receiverId);
            }
        }
    }
}
