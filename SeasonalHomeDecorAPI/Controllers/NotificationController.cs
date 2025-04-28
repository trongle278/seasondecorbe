using System.Security.Claims;
using BusinessLogicLayer;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Services;
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

        [HttpGet("getAllNotifications")]
        public async Task<IActionResult> GetAllNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid token or user ID" });
            }

            var response = await _notificationService.GetAllNotificationsAsync(userId);
            return Ok(response); // ✅ Trả về dữ liệu thông báo
        }

        [HttpGet("getUnreadNotification")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid token or user ID" });
            }

            var response = await _notificationService.GetUnreadNotificationsAsync(userId);
            return Ok(response); // ✅ Trả về dữ liệu thông báo
        }
    }
}
