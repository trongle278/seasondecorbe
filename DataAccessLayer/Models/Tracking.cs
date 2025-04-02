using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Tracking
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        [EnumDataType(typeof(Booking.BookingStatus))]
        public Booking.BookingStatus Status { get; set; } // Trạng thái của booking tại thời điểm này
        public string? Note { get; set; } // Ghi chú (nếu có)
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public virtual ICollection<TrackingImage> TrackingImages { get; set; } = new List<TrackingImage>();
    }
}

