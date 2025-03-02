using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelRequest.Order
{
    public class OrderRequest
    {
        public int AddressId { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime OrderDate { get; set; }
        public double TotalPrice { get; set; }
        public enum Status
        {
            Pending,
            Shipping,
            Completed,
            Cancelled
        }
        public Status OrderStatus { get; set; }
        public int AccountId { get; set; }
        public virtual ICollection<ProductOrderRequest> ProductOrders { get; set; }
    }
}
