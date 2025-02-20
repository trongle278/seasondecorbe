using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationResponse> SendNotificationAsync(Notification notification);
        Task<IEnumerable<NotificationResponse>> GetNotificationsByAccountIdAsync(int accountId);
    }
}
