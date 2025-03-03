using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateBookingRequest
    {
        public double TotalPrice { get; set; }
        public int AccountId { get; set; }
        public int DecorServiceId { get; set; }
        public int? VoucherId { get; set; }
    }

    public class ConfirmBookingRequest
    {
        public int BookingId { get; set; }
        public List<PaymentPhaseRequest> PaymentPhases { get; set; }
    }

    public class UpdateBookingStatusRequest
    {
        public int BookingId { get; set; }
        public int NewStatus { get; set; } // 0=Surveying,1=Pending,..., 
                                           // map sang Booking.BookingStatus
    }
}
