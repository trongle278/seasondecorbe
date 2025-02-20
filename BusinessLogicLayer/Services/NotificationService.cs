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
using DataAccessObject.Models;
using BusinessLogicLayer.ModelResponse;
using AutoMapper;

namespace BusinessLogicLayer.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly FcmService _fcmService;
        private readonly IMapper _mapper;
        //test
        private readonly IDeviceTokenRepository _deviceTokenRepository; // Repository cho device tokens

        public NotificationService(INotificationRepository notificationRepository,
                                   IHubContext<NotificationHub> hubContext,
                                   IMapper mapper,
                                   FcmService fcmService,
                                   IDeviceTokenRepository deviceTokenRepository)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _mapper = mapper;
            _fcmService = fcmService;
            //test
            _deviceTokenRepository = deviceTokenRepository;
        }

        public async Task<NotificationResponse> SendNotificationAsync(Notification notification)
        {
            // Lưu notification vào DB
            await _notificationRepository.InsertAsync(notification);
            await _notificationRepository.SaveAsync();

            // Map Notification sang NotificationResponse để gửi qua SignalR
            var response = _mapper.Map<NotificationResponse>(notification);

            // Gửi realtime qua SignalR cho web
            await _hubContext.Clients.User(notification.AccountId.ToString())
                             .SendAsync("ReceiveNotification", response);

            // Gửi push notification qua FCM cho từng token
            var deviceTokens = await _deviceTokenRepository.GetTokensByAccountIdAsync(notification.AccountId);
            foreach (var token in deviceTokens)
            {
                var data = new Dictionary<string, string>
        {
            { "type", "chat" },
            { "notificationId", notification.Id.ToString() }
        };

                await _fcmService.SendPushNotificationAsync(token.Token, "Tin nhắn mới", notification.Content, data);
            }

            return response;
        }

        public async Task<IEnumerable<NotificationResponse>> GetNotificationsByAccountIdAsync(int accountId)
        {
            // Lấy danh sách Notification (có Include(n => n.Account))
            var notifications = await _notificationRepository.GetNotificationsByAccountIdAsync(accountId);

            // Map sang NotificationResponse
            var response = _mapper.Map<IEnumerable<NotificationResponse>>(notifications);

            return response;
        }
    }
}
