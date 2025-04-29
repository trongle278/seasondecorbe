using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataAccessObject.Models.Notification;

namespace BusinessLogicLayer.ModelRequest
{
    public class NotificationCreateRequest
    {
        public int AccountId { get; set; }          // Người nhận
        public string Title { get; set; }
        public string Content { get; set; }
        public int? SenderId { get; set; }           // Người gửi (nếu có)
        public NotificationType Type { get; set; }   // System, Event
    }
}
