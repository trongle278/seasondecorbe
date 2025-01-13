using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class BookingDetail
    {
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; }
    }
}
