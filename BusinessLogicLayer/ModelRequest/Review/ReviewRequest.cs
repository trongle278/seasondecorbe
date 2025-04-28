using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelRequest.Review
{
    public class ReviewRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rate must between 1-5")]
        public int Rate { get; set; }
        [Required]
        public string Comment { get; set; }
        [Required]
        public int AccountId { get; set; }
        public List<IFormFile> Images { get; set; }
    }

    public class ReviewOrderRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rate must between 1-5")]
        public int Rate { get; set; }
        [Required]
        public string Comment { get; set; }
        [Required]
        public int OrderId { get; set; }
        [Required]
        public int ProductId { get; set; }
        public List<IFormFile> Images { get; set; }
    }

    public class ReviewBookingRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rate must between 1-5")]
        public int Rate { get; set; }
        [Required]
        public string Comment { get; set; }
        [Required]
        public int BookingId { get; set; }
        [Required]
        public int ServiceId { get; set; }
        public List<IFormFile> Images { get; set; }
    }

    public class UpdateOrderReviewRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rate must between 1-5")]
        public int Rate { get; set; }
        [Required]
        public string Comment { get; set; }
        public List<IFormFile> Images { get; set; }
    }

    public class UpdateBookingReviewRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rate must between 1-5")]
        public int Rate { get; set; }
        [Required]
        public string Comment { get; set; }
        public List<IFormFile> Images { get; set; }
    }
}
