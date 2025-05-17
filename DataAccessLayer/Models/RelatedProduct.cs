using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class RelatedProduct
    {
        public int Id { get; set; }
        public int TotalItem { get; set; }
        public decimal TotalPrice { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int ServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public Booking Booking { get; set; }

        public virtual ICollection<RelatedProductItem> RelatedProductItems { get; set; }
    }
}
