using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse.Product
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public string ProductImg { get; set; }
        public double ProductPrice { get; set; }
        public int? Quantity { get; set; }
        public int CategoryId { get; set; }
    }
}
