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

        [HttpGet("history/{receiverId}")]
        public async Task<IActionResult> GetChatHistory(int receiverId)
        {
            try
            {
                var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var chatHistory = await _chatService.GetChatHistoryAsync(senderId, receiverId);
                return Ok(chatHistory);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            try
            {
                var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var chat = await _chatService.SendMessageAsync(senderId, request);
                return Ok(chat);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

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
