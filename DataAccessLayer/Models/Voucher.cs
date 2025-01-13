using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Voucher
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string VoucherName { get; set; }
        public string OfferCode { get; set; }
        public int Quantity { get; set; }
        public int Discount { get; set; }
        public string Status { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; }
    }
}
