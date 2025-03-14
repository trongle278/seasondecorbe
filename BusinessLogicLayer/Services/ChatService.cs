using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

        public async Task<BaseResponse> GetChatHistoryAsync(int senderId, int receiverId)
        {
            var chats = await _unitOfWork.ChatRepository.GetChatHistoryAsync(senderId, receiverId);

            var chatMessages = chats.Select(chat => new ChatMessageResponse
            {
                Id = chat.Id,
                SenderId = chat.SenderId,
                SenderName = chat.Sender != null ? $"{chat.Sender.FirstName} {chat.Sender.LastName}" : null,
                ReceiverId = chat.ReceiverId,
                ReceiverName = chat.Receiver != null ? $"{chat.Receiver.FirstName} {chat.Receiver.LastName}" : null,
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

            return new BaseResponse
            {
                Success = true,
                Message = "Chat history retrieved successfully",
                Errors = new List<string>(),
                Data = chatMessages
            };
        }

        public async Task<BaseResponse> MarkMessagesAsReadAsync(int receiverId, int senderId)
        {
            await _unitOfWork.ChatRepository.MarkMessagesAsReadAsync(receiverId, senderId);

            return new BaseResponse
            {
                Success = true,
                Message = "Messages marked as read successfully",
                Errors = new List<string>(),
                Data = new List<object>() // Dữ liệu rỗng nhưng vẫn là []
            };
        }

        public async Task<BaseResponse> GetUnreadMessagesAsync(int userId)
        {
            var unreadMessages = await _unitOfWork.ChatRepository.GetUnreadMessagesAsync(userId);

            var messageList = unreadMessages.Select(chat => new ChatMessageResponse
            {
                Id = chat.Id,
                SenderId = chat.SenderId,
                SenderName = chat.Sender != null ? $"{chat.Sender.FirstName} {chat.Sender.LastName}" : null,
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
            }).ToList();

            return new BaseResponse
            {
                Success = true,
                Message = "Unread messages retrieved successfully.",
                Errors = new List<string>(),
                Data = messageList
            };
        }
    }
}
