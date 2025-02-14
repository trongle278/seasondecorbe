using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
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

        public async Task<IEnumerable<Chat>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            return await _unitOfWork.ChatRepository.GetChatHistoryAsync(senderId, receiverId);
        }

        public async Task<Chat> SendMessageAsync(int senderId, ChatMessageRequest request)
        {
            var chat = new Chat
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Message = request.Message,
                SentTime = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.ChatRepository.InsertAsync(chat);
            await _unitOfWork.CommitAsync();

            return chat;
        }

        public async Task MarkMessagesAsReadAsync(int receiverId, int senderId)
        {
            var unreadMessages = await _unitOfWork.ChatRepository.GetUnreadMessagesAsync(receiverId, senderId);

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                _unitOfWork.ChatRepository.Update(message);  // Bỏ await vì Update không phải async
            }

            await _unitOfWork.CommitAsync();
        }

        public async Task<Chat> SendMessageWithFilesAsync(int senderId, ChatMessageRequest request, IEnumerable<IFormFile> files)
        {
            var chat = new Chat
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Message = request.Message,
                SentTime = DateTime.UtcNow,
                IsRead = false
            };

            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                var fileName = file.FileName;
                // Implement file upload logic here
                var fileUrl = await _cloudinaryService.UploadFileAsync(stream, fileName);

                var chatFile = new ChatFile
                {
                    FileName = fileName,
                    FileUrl = fileUrl
                };

                chat.ChatFiles.Add(chatFile);
            }

            await _unitOfWork.ChatRepository.InsertAsync(chat);
            await _unitOfWork.CommitAsync();

            return chat;
        }
    }
}
