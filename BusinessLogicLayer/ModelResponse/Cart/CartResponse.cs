using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse.Cart
{
    public class CartResponse
    {
        public int Id { get; set; }
        public int TotalItem { get; set; }
        public double TotalPrice { get; set; }
        public int AccountId { get; set; }
        public int? VoucherId { get; set; }
        public ICollection<CartItemResponse> CartItems { get; set; }
    }
}
