using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class BookingDetail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Booking")]
        public int BookingId { get; set; }

        // Navigation property
        public virtual Booking Booking { get; set; }

        [Required]
        [StringLength(255)]
        public string ServiceItem { get; set; }

        [Required]
        public decimal Cost { get; set; }

        [Required]
        public DateTime EstimatedCompletion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
