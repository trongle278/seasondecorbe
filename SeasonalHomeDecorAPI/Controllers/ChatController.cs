using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Utilities.Hub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;

        }

        // Lấy lịch sử giữa senderId (lấy từ token) và receiverId
        [HttpGet("chat-history/{userId}")]
        public async Task<IActionResult> GetChatHistory(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (currentUserId == 0)
            {
                return Unauthorized(new { message = "Invalid token or user ID" });
            }

            var response = await _chatService.GetChatHistoryAsync(currentUserId, userId);
            return Ok(response); // ✅ Trả về nguyên BaseResponse, không chỉnh sửa
        }

        

        [HttpPost("markasread/{senderId}")]
        public async Task<IActionResult> MarkAsRead(int senderId)
        {
            try
            {
                var receiverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                await _chatService.MarkMessagesAsReadAsync(receiverId, senderId);
                return Ok(new { message = "Messages marked as read" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("unread-messages")]
        public async Task<IActionResult> GetUnreadMessages()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var response = await _chatService.GetUnreadMessagesAsync(userId);
            return Ok(response);
        }
    }
}
