using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class PaymentRequest
    {
        public string Code { get; set; }
        public double Total { get; set; }
    }
}
