using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.POS
{
    public class CreatePaymentLinkRequest
    {
        public int bookingId { get; set; }
        public string returnUrl { get; set; }
        public string cancelUrl { get; set; }

        public int OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
        public List<PaymentItemRequest> Items { get; set; }
    }

    public class PaymentItemRequest
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
    }
}
