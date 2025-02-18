using System.Security.Claims;
using BusinessLogicLayer;
using BusinessLogicLayer.Interfaces;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static DataAccessObject.Models.Notification;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // Lấy danh sách thông báo của người dùng
        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetNotifications(int accountId)
        {
            var notifications = await _notificationService.GetNotificationsByAccountIdAsync(accountId);
            return Ok(notifications);
        }

        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var notifications = await _notificationService.GetNotificationsByAccountIdAsync(userId);
            return Ok(notifications);
        }
    }
}
