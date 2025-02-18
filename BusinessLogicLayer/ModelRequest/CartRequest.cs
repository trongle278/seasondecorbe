using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class CartRequest
    {
        public int TotalItem { get; set; }
        public double TotalPrice { get; set; }
        public int AccountId { get; set; }
    }
}
