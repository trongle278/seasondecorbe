using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateBookingRequest
    {
        public int DecorServiceId { get; set; }
        // Các thông tin khác nếu cần, ví dụ: VoucherId, ghi chú, v.v.
    }

    public class PaymentPhaseRequest
    {
        // Loại giai đoạn thanh toán (Deposit, MaterialPreparation, FinalPayment)
        public PaymentPhase.PaymentPhaseType Phase { get; set; }
        public double ScheduledAmount { get; set; }
        public DateTime DueDate { get; set; }
    }
}
