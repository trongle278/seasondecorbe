using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class PaymentPhaseRequest
    {
        public int PhaseType { get; set; } // 0=Deposit, 1=MaterialPreparation, 2=FinalPayment
        public double ScheduledAmount { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class MakePaymentRequest
    {
        public int BookingId { get; set; }
        public int PaymentPhaseId { get; set; }
        public double Amount { get; set; }
        public int OrderId { get; set; }
    }
}
