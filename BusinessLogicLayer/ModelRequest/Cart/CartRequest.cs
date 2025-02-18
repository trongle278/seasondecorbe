using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace BusinessLogicLayer.ModelRequest.Cart
{
    public class CartRequest
    {
        public int TotalItem { get; set; }
        public double TotalPrice { get; set; }
        public int AccountId { get; set; }
    }

    public class CartDTORequest
    {
        [Range(0, int.MaxValue, ErrorMessage = "Total item cannot be negative.")]
        public int TotalItem { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Total price cannot be negative.")]
        public double TotalPrice { get; set; }
        public int AccountId { get; set; }
        public int? VoucherId { get; set; }
        public ICollection<CartItemRequest> CartItems { get; set; }
    }
}
