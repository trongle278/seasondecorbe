using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Interfaces
{
    public interface IChatService
    {
        Task<List<ChatMessageResponse>> GetChatHistoryAsync(int senderId, int receiverId);
        Task MarkMessagesAsReadAsync(int receiverId, int senderId);
        Task<ChatMessageResponse> SendMessageWithFilesAsync(int senderId, ChatMessageRequest request, IEnumerable<IFormFile> formFiles);
    }
}
