using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class ZoomMeetingRequest
    {
        public string Topic { get; set; }
        public string TimeZone { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
    }

    public class CreateMeetingRequest
    {
        public DateTime StartTime { get; set; }
        public int CustomerId { get; set; }
    }
}
