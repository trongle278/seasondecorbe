using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime OrderDate { get; set; }
        public double TotalPrice { get; set; }
        public enum OrderStatus
        {
            Pending,
            Processing,
            Shipping,
            Completed,
            Cancelled
        }
        public OrderStatus Status { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int AddressId { get; set; }
        public Address Address { get; set; }

        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<ProductOrder> ProductOrders { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
