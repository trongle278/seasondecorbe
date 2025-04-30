using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using DataAccessObject.Models;
using Microsoft.AspNetCore.SignalR;
using Repository.Interfaces;
using BusinessLogicLayer.ModelResponse;
using AutoMapper;
using BusinessLogicLayer.Utilities.Hub;
using BusinessLogicLayer.ModelRequest;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly FcmService _fcmService;
        private readonly IDeviceTokenRepository _deviceTokenRepository; // Repository cho device tokens

        public NotificationService(IUnitOfWork unitOfWork,
                                   IHubContext<NotificationHub> hubContext,
                                   FcmService fcmService,
                                   IDeviceTokenRepository deviceTokenRepository)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _fcmService = fcmService;
            _deviceTokenRepository = deviceTokenRepository;
        }

        public async Task<BaseResponse<Notification>> CreateNotificationAsync(NotificationCreateRequest request)
        {
            // Tạo đối tượng notification từ request
            var notification = new Notification
            {
                AccountId = request.AccountId,
                Title = request.Title,
                Content = request.Content,
                Url = request.Url,
                NotifiedAt = DateTime.Now,
                IsRead = false
            };

            // Lưu vào DB
            await _unitOfWork.NotificationRepository.InsertAsync(notification);
            await _unitOfWork.CommitAsync();

            // Trả về thông báo vừa lưu
            return new BaseResponse<Notification>
            {
                Success = true,
                Message = "Notification created successfully.",
                Data = notification
            };
        }

        public async Task<BaseResponse<List<NotificationResponse>>> GetAllNotificationsAsync(int accountId)
        {
            var notifications = await _unitOfWork.NotificationRepository
                .Queryable()
                .Where(n => n.AccountId == accountId)
                .OrderByDescending(n => n.NotifiedAt)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    NotifiedAt = n.NotifiedAt,
                    Url = n.Url,
                    IsRead = n.IsRead
                })
                .ToListAsync();

            return new BaseResponse<List<NotificationResponse>>
            {
                Success = true,
                Message = "Get notifications successfully.",
                Data = notifications
            };
        }

        public async Task<BaseResponse<List<NotificationResponse>>> GetUnreadNotificationsAsync(int accountId)
        {
            var unreadNotifications = await _unitOfWork.NotificationRepository
                .Queryable()
                .Where(n => n.AccountId == accountId && !n.IsRead) // Lọc thông báo chưa đọc
                .OrderByDescending(n => n.NotifiedAt)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    Url = n.Url,
                    NotifiedAt = n.NotifiedAt,
                    IsRead = n.IsRead
                })
                .ToListAsync();

            return new BaseResponse<List<NotificationResponse>>
            {
                Success = true,
                Message = "Fetched unread notifications successfully.",
                Data = unreadNotifications
            };
        }

        public async Task<BaseResponse<bool>> MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _unitOfWork.NotificationRepository
                .Queryable()
                .FirstOrDefaultAsync(n => n.Id == notificationId); // Tìm thông báo theo ID

            if (notification == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Notification not found." // Nếu không tìm thấy thông báo
                };
            }

            notification.IsRead = true; // Đánh dấu là đã đọc
            await _unitOfWork.CommitAsync(); // Lưu lại thay đổi trong DB

            return new BaseResponse<bool>
            {
                Success = true,
                Message = "Notification marked as read successfully.",
                Data = true
            };
        }

        public async Task<BaseResponse<bool>> MarkAllNotificationsAsReadAsync(int accountId)
        {
            // Lấy tất cả thông báo chưa đọc của account
            var unreadNotifications = await _unitOfWork.NotificationRepository
                .Queryable()
                .Where(n => n.AccountId == accountId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any())
            {
                return new BaseResponse<bool>
                {
                    Success = true,
                    Message = "No unread notifications to mark.",
                    Data = true
                };
            }

            // Đánh dấu tất cả là đã đọc
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _unitOfWork.CommitAsync();

            return new BaseResponse<bool>
            {
                Success = true,
                Message = "All notifications marked as read successfully.",
                Data = true
            };
        }
    }
}
