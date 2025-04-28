using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataAccessObject.Models.Notification;

namespace BusinessLogicLayer.ModelResponse
{
    public class NotificationResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime NotifiedAt { get; set; }
        public bool IsRead { get; set; }
        public NotificationType Type { get; set; }
    }
}
