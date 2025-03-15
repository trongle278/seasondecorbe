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
        Task<BaseResponse> GetChatHistoryAsync(int senderId, int receiverId);
        Task<BaseResponse> MarkMessagesAsReadAsync(int receiverId, int senderId);
        Task<ChatMessageResponse> SendMessageAsync(int senderId, ChatMessageRequest request, IEnumerable<IFormFile> formFiles);
        Task<BaseResponse> GetUnreadMessagesAsync(int userId);
    }
}
