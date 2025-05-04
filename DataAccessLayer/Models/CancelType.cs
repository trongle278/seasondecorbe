using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class CancelType
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
    }
}
