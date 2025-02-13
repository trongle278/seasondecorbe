using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DataAccessObject.Models
{
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int TotalItem { get; set; }
        public double TotalPrice { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int? VoucherId { get; set; }
        public Voucher Voucher { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; }
    }
}
