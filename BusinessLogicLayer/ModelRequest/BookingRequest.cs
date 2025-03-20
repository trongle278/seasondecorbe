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

    public class ConfirmBookingRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Deposit amount must be greater than 0")]
        public decimal DepositAmount { get; set; }
    }

    public class BookingDetailRequest
    {
        [Required]
        [StringLength(255)]
        public string ServiceItem { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than 0")]
        public decimal Cost { get; set; }

        [Required]
        public DateTime EstimatedCompletion { get; set; }
    }
}
