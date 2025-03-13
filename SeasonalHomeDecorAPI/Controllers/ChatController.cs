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
        [HttpGet("history/{receiverId}")]
        public async Task<IActionResult> GetChatHistory(int receiverId)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var response = await _chatService.GetChatHistoryAsync(senderId, receiverId);
            return Ok(response);
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

        // Đánh dấu tin nhắn đã đọc
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

        //[HttpGet("getall")]
        //public async Task<IActionResult> GetAllUserChats()
        //{
        //    try
        //    {
        //        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        //        if (userId <= 0)
        //            return BadRequest(new { message = "Invalid user ID" });

        //        var contacts = await _chatService.GetAllUserChatAsync(userId);

        //        if (contacts == null || !contacts.Any())
        //            return Ok(new List<ChatMessageResponse>()); // Trả về danh sách rỗng thay vì lỗi

        //        return Ok(contacts);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        //[HttpPost("add-to-chat-list")]
        //public async Task<IActionResult> AddToChatList([FromBody] CreateChatRequest request)
        //{
        //    var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        //    await _chatService.AddToChatListAsync(senderId, request.ReceiverId);

        //    return Ok(new { message = "User added to chat list." });
        //}
    }
}
