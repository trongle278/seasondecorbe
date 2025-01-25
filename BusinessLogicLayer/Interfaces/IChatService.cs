using BusinessLogicLayer.ModelRequest;
using DataAccessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Interfaces
{
    public interface IChatService
    {
        Task<IEnumerable<Chat>> GetChatHistoryAsync(int senderId, int receiverId);
        Task<Chat> SendMessageAsync(int senderId, ChatMessageRequest request);
        Task MarkMessagesAsReadAsync(int receiverId, int senderId);
    }
}
