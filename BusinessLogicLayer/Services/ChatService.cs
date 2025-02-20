using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;
using Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataAccessObject.Models.Notification;

namespace BusinessLogicLayer.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly INotificationService _notificationService;

        public ChatService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _notificationService = notificationService;
        }

        public async Task<List<ChatMessageResponse>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            // Lấy danh sách Chat entity và include ChatFiles, Sender và Receiver
            var chats = await _unitOfWork.ChatRepository.GetChatHistoryAsync(senderId, receiverId);

            var response = chats.Select(chat => new ChatMessageResponse
            {
                Id = chat.Id,
                SenderId = chat.SenderId,
                // Kết hợp FirstName và LastName của Sender (nếu có)
                SenderName = chat.Sender != null
                                ? $"{chat.Sender.FirstName} {chat.Sender.LastName}"
                                : null,
                ReceiverId = chat.ReceiverId,
                // Kết hợp FirstName và LastName của Receiver (nếu có)
                ReceiverName = chat.Receiver != null
                                ? $"{chat.Receiver.FirstName} {chat.Receiver.LastName}"
                                : null,
                Message = chat.Message,
                SentTime = chat.SentTime,
                IsRead = chat.IsRead,
                Files = chat.ChatFiles.Select(cf => new ChatFileResponse
                {
                    FileId = cf.Id,
                    FileName = cf.FileName,
                    FileUrl = cf.FileUrl,
                    UploadedAt = cf.UploadedAt
                }).ToList()
            }).ToList();

            return response;
        }

        public async Task<ChatMessageResponse> SendMessageWithFilesAsync(int senderId, ChatMessageRequest request, IEnumerable<IFormFile> formFiles)
        {
            if (senderId == request.ReceiverId)
            {
                throw new InvalidOperationException("Cannot send a message to yourself.");
            }

            // Tạo entity Chat
            var chat = new Chat
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Message = request.Message,
                SentTime = DateTime.UtcNow,
                IsRead = false
            };

            // Upload và thêm ChatFile
            foreach (var formFile in formFiles)
            {
                using var stream = formFile.OpenReadStream();
                var fileUrl = await _cloudinaryService.UploadFileAsync(stream, formFile.FileName);

                var chatFile = new ChatFile
                {
                    FileName = formFile.FileName,
                    FileUrl = fileUrl
                };
                chat.ChatFiles.Add(chatFile);
            }

            // Lưu chat vào DB
            await _unitOfWork.ChatRepository.InsertAsync(chat);
            await _unitOfWork.CommitAsync();

            // Tạo đối tượng Notification cho tin nhắn mới
            var notification = new Notification
            {
                Title = "New Message",
                Content = chat.Message, // Hoặc bạn có thể định dạng lại nội dung hiển thị thông báo
                NotifiedAt = DateTime.UtcNow,
                AccountId = chat.ReceiverId,
                SenderId = chat.SenderId,
                Type = NotificationType.Chat
            };

            // Gọi NotificationService để lưu và gửi realtime notification
            await _notificationService.SendNotificationAsync(notification);

            // Map sang response
            var response = new ChatMessageResponse
            {
                Id = chat.Id,
                SenderId = chat.SenderId,
                ReceiverId = chat.ReceiverId,
                Message = chat.Message,
                SentTime = chat.SentTime,
                IsRead = chat.IsRead,
                Files = chat.ChatFiles.Select(cf => new ChatFileResponse
                {
                    FileId = cf.Id,
                    FileName = cf.FileName,
                    FileUrl = cf.FileUrl,
                    UploadedAt = cf.UploadedAt
                }).ToList()
            };

            return response;
        }

        public async Task MarkMessagesAsReadAsync(int receiverId, int senderId)
        {
            var unreadMessages = await _unitOfWork.ChatRepository.GetUnreadMessagesAsync(receiverId, senderId);

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                _unitOfWork.ChatRepository.Update(message); // Update ko async => ko await
            }
            await _unitOfWork.CommitAsync();
        }
    }
}
