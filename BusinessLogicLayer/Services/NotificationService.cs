using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Hub;
using BusinessLogicLayer.Interfaces;
using DataAccessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repository.Interfaces;

namespace BusinessLogicLayer.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(INotificationRepository notificationRepository,
                                   IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(Notification notification)
        {
            // Lưu thông báo vào DB
            await _notificationRepository.InsertAsync(notification);
            await _notificationRepository.SaveAsync();

            // Gửi realtime thông báo cho người dùng có AccountId tương ứng
            await _hubContext.Clients.User(notification.AccountId.ToString())
                             .SendAsync("ReceiveNotification", notification);
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByAccountIdAsync(int accountId)
        {
            return await _notificationRepository.GetNotificationsByAccountIdAsync(accountId);
        }
    }
}
