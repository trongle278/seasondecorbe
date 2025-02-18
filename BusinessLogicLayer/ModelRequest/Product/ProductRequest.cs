using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest.Product
{
    public class ProductRequest
    {
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public string ProductImg { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Product price cannot be negative.")]
        public double ProductPrice { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Product quantity cannot be negative.")]
        public int? Quantity { get; set; }
        public int CategoryId { get; set; }
    }
}