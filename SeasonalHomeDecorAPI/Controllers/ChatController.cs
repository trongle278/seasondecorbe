using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // Gửi tin nhắn kèm file (qua multipart/form-data)
        [HttpPost("sendmessage")]
        public async Task<IActionResult> SendMessageWithFiles([FromForm] ChatMessageRequest request,
                                                              [FromForm] List<IFormFile> files)
        {
            try
            {
                var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Hàm này trả về ChatMessageResponse
                var response = await _chatService.SendMessageWithFilesAsync(senderId, request, files);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
