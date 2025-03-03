using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Code { get; set; }
        //public string PaymentMethod { get; set; }
        public DateTime Date { get; set; }
        public double Total { get; set; }
        public enum PaymentStatus
        {
            Pending,
            Completed,
            Failed,
            Refunded,
            Cancelled,
            Expired
        }
        public PaymentStatus Status { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        // Liên kết với bảng PaymentPhase để biết giao dịch thuộc giai đoạn nào
        public int PaymentPhaseId { get; set; }
        public PaymentPhase PaymentPhase { get; set; }
    }
}
