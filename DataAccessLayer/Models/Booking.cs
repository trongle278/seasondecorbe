using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Booking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string BookingCode { get; set; }
        public string Status { get; set; }
        public double TotalPrice { get; set; }
        public DateTime CreateAt { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int DecorServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public int VoucherId { get; set; }
        public Voucher Voucher { get; set; }

        public virtual Review Review { get; set; }

        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
