using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
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
        [HttpGet("history/{receiverId}")]
        public async Task<IActionResult> GetChatHistory(int receiverId)
        {
            try
            {
                var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Giờ hàm trả về List<ChatMessageResponse>
                var history = await _chatService.GetChatHistoryAsync(senderId, receiverId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Gửi tin nhắn kèm file (qua multipart/form-data)
        [HttpPost("send-with-files")]
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

        // Đánh dấu tin nhắn đã đọc
        [HttpPost("mark-as-read/{senderId}")]
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
    }
}
