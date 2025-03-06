using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class PaymentPhase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public PaymentPhaseType Phase { get; set; }
        public enum PaymentPhaseType
        {
            Deposit,             // Đợt đặt cọc    0        
            FinalPayment         // Đợt thanh toán cuối   1
        }

        public double ScheduledAmount { get; set; }
        public long OrderCode { get; set; }
        public string Description { get; set; }

        //public DateTime DueDate { get; set; }

        //public PaymentPhaseStatus Status { get; set; }
        //public enum PaymentPhaseStatus
        //{
        //    Pending,
        //    Completed,
        //    Cancelled
        //}

        public DateTime? PaymentDate { get; set; }

        // Liên kết với các giao dịch thanh toán thực tế cho giai đoạn này
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
