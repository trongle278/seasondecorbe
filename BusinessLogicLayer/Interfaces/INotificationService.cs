using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface INotificationService
    {
        //Task<NotificationResponse> SendNotificationAsync(Notification notification);
        //Task<IEnumerable<NotificationResponse>> GetNotificationsByAccountIdAsync(int accountId);
        Task<BaseResponse<Notification>> CreateNotificationAsync(NotificationCreateRequest request);
        Task<BaseResponse<List<NotificationResponse>>> GetAllNotificationsAsync(int accountId);
        Task<BaseResponse<List<NotificationResponse>>> GetUnreadNotificationsAsync(int accountId);
        Task<BaseResponse<bool>> MarkNotificationAsReadAsync(int notificationId);
        Task<BaseResponse<bool>> MarkAllNotificationsAsReadAsync(int accountId);
    }
}
