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
        public int Rate { get; set; }
        public string Comment { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public bool IsUpdated { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int? BookingId { get; set; }
        public Booking Booking { get; set; }

        public int? ServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public int? OrderId { get; set; }
        public Order Order { get; set; }

        public int? ProductId { get; set; }
        public Product Product { get; set; }

        public virtual ICollection<ReviewImage> ReviewImages { get; set; }
    }
}
