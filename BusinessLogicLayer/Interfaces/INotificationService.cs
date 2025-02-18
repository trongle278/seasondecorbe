using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(Notification notification);
        Task<IEnumerable<Notification>> GetNotificationsByAccountIdAsync(int accountId);
    }
}
