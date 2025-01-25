using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using DataAccessObject.Models;
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

        public ChatService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

    }
}
