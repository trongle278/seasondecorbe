using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse.Order
{
    public class ProductOrderResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public double? UnitPrice { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
    }
}
