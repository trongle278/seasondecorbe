using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Booking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string BookingCode { get; set; }
        public double TotalPrice { get; set; }
        public DateTime CreateAt { get; set; }
        public enum BookingStatus
        {
            Pending,     // Khi khách hàng tạo booking
            Confirmed,   // Provider xác nhận booking
            Surveying,   // Provider bắt đầu khảo sát (gặp trực tiếp với customer)
            Procuring,   // Sau khảo sát, customer đặt cọc để chuẩn bị nguyên liệu
            Progressing, // Khi thi công đang diễn ra
            Completed,   // Khi thanh toán cuối cùng xong
            Cancelled    // Booking bị hủy
        }
        public BookingStatus Status { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int DecorServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public int? VoucherId { get; set; }
        public Voucher Voucher { get; set; }

        public Review Review { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<PaymentPhase> PaymentPhases { get; set; }
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; }
    }
}
