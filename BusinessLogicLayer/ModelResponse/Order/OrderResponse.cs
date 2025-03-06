using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse.Order
{
    public class OrderResponse
    {
        public int Id { get; set; }
        public int AddressId { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime OrderDate { get; set; }
        public double TotalPrice { get; set; }
        public enum OrderStatus
        {
            Pending,
            Shipping,
            Completed,
            Cancelled
        }
        public OrderStatus Status { get; set; }
        public int AccountId { get; set; }
        public ICollection<ProductOrderResponse> ProductOrders { get; set; }
    }
}
