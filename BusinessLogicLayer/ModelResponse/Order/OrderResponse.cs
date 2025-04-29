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
        public string OrderCode { get; set; }
        public int AddressId { get; set; }
        public OrderAddressResponse Address { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public enum OrderStatus
        {
            Pending,
            Paid,
            Cancelled
        }
        public OrderStatus Status { get; set; }
        public int AccountId { get; set; }
        public ICollection<OrderDetailResponse> OrderDetails { get; set; }
    }
}
