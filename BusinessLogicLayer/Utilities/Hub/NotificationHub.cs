using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.SignalR;

namespace BusinessLogicLayer.Utilities.Hub
{
    public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHub(INotificationService notificationService, IHubContext<NotificationHub> hubContext)
        {
            _notificationService = notificationService;
            _hubContext = hubContext;  // Gán giá trị cho _hubContext
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null || httpContext.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new Exception("User is not authenticated!");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new Exception("User ID not found in claims!");
            }

            var userId = int.Parse(userIdClaim.Value);
            _userConnections[userId] = Context.ConnectionId;

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null || httpContext.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new Exception("User is not authenticated!");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new Exception("User ID not found in claims!");
            }

            var userId = int.Parse(userIdClaim.Value);
            _userConnections.Remove(userId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotification(NotificationCreateRequest request)
        {
            // Gọi service để lưu thông báo vào DB
            var notificationResponse = await _notificationService.CreateNotificationAsync(request);

            if (!notificationResponse.Success || notificationResponse.Data == null)
            {
                throw new HubException(notificationResponse.Message ?? "Failed to create notification.");
            }

            var notification = notificationResponse.Data;

            // Sau khi thông báo được lưu vào DB, gửi qua WebSocket
            await _hubContext.Clients.User(notification.AccountId.ToString())
                              .SendAsync("ReceiveNotification", new
                              {
                                  notification.Title,
                                  notification.Content,
                                  notification.NotifiedAt,
                                  notification.Url
                              });
        }

        public async Task MarkAsRead(int notificationId)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext?.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new HubException("User is not authenticated.");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                throw new HubException("Invalid user ID.");
            }

            // Gọi service để đánh dấu thông báo đã đọc
            var response = await _notificationService.MarkNotificationAsReadAsync(notificationId);

            if (!response.Success)
            {
                throw new HubException(response.Message ?? "Failed to mark notification as read.");
            }

            // Gửi thông báo cập nhật trạng thái đọc cho client
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);

            // Nếu cần thông báo cho các client khác của cùng user
            if (_userConnections.TryGetValue(userId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("NotificationMarkedAsRead", notificationId);
            }
        }

        public async Task MarkAllAsRead()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext?.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new HubException("User is not authenticated.");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                throw new HubException("Invalid user ID.");
            }

            var response = await _notificationService.MarkAllNotificationsAsReadAsync(userId);

            if (!response.Success)
            {
                throw new HubException(response.Message ?? "Failed to mark all notifications as read.");
            }

            // Gửi thông báo cập nhật cho client
            await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead");
        }
    }
}
