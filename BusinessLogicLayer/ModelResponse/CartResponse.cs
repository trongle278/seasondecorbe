using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class CartResponse
    {
        public int Id { get; set; }
        public int TotalItem { get; set; }
        public double TotalPrice { get; set; }

        public int AccountId { get; set; }
    }
}
