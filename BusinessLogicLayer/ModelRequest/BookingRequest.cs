using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateBookingRequest
    {
        public int DecorServiceId { get; set; }
        public int AddressId { get; set; }
    }

    /// <summary>
    /// Request để thêm tracking tiến độ thi công vào booking
    /// </summary>
    public class TrackingRequest
    {
        [Required]
        [StringLength(255)]
        public string Stage { get; set; }

        public DateTime? PlannedDate { get; set; }

        public DateTime? ActualDate { get; set; }

        public string ImageUrls { get; set; }
    }

    public class CreateQuotationRequest
    {
        public int BookingId { get; set; }
        public decimal MaterialCost { get; set; }
        public decimal LaborCost { get; set; }
    }
}
