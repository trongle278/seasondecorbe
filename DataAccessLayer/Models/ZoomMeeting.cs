using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class ZoomMeeting
    {
        public int id { get; set; }
        public string Topic { get; set; }
        public DateTime StartTime { get; set; }
        public int? Duration { get; set; }
        public string? ZoomUrl { get; set; }
        public enum MeetingStatus
        {
            Requested,
            Scheduled,
            Started,
            Ended,
            Rejected
        }
        public MeetingStatus Status { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? ResponseAt { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }
    }
}
