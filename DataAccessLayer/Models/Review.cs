using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string Image { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public int ProductId { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }
    }
}
