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

namespace BusinessLogicLayer
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public ChatService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<List<ChatMessageResponse>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            // Lấy danh sách Chat entity
            var chats = await _unitOfWork.ChatRepository.GetChatHistoryAsync(senderId, receiverId);

            // Map sang List<ChatMessageResponse>
            var response = chats.Select(chat => new ChatMessageResponse
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
            }).ToList();

            return response;
        }

        public async Task<ChatMessageResponse> SendMessageWithFilesAsync(int senderId, ChatMessageRequest request, IEnumerable<IFormFile> formFiles)
        {
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

            // Lưu DB
            await _unitOfWork.ChatRepository.InsertAsync(chat);
            await _unitOfWork.CommitAsync();

            // Map sang response
            var response = new ChatMessageResponse
            {
                Id = chat.Id,
                SenderId = chat.SenderId,
                // SenderName = "Load từ DB user (tuỳ bạn)"
                ReceiverId = chat.ReceiverId,
                // ReceiverName = "Load từ DB user (tuỳ bạn)",
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
