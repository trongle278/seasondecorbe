using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelRequest.Cart
{
    public class CartItemRequest
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        [Range(1, int.MaxValue, ErrorMessage  = "Item in cart quantity must exists.")]
        public int Quantity { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative.")]
        public double UnitPrice { get; set; }
    }
}
