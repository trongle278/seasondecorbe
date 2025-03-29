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
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public DateTime CreateAt { get; set; }
        public enum BookingStatus
        {
            Pending,            // Khi khách hàng tạo booking
            Survey,             // Provider đã xác nhận và sắp xếp khảo sát
            Confirm,            // Khi customer đồng ý các điều khoản và chốt hợp đồng
            DepositPaid,        // Đã thanh toán đặt cọc
            Preparing,          // Chuẩn bị nguyên liệu
            InTransit,          // Nguyên liệu được chuyển đến chỗ khách hàng
            Progressing,        // Đang tiến hành thi công (theo dạng Tracking service)
            ConstructionPayment,// Thanh toán thi công
            Completed,          // Dự án hoàn thành
            Cancelled           // Booking bị hủy
        }

        public BookingStatus Status { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int DecorServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public int AddressId { get; set; }
        public Address Address { get; set; }

        public Review Review { get; set; }

        // Chi tiết báo giá dùng entity BookingDetail
        public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
        public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; }
        public virtual ICollection<Support> Supports { get; set; } = new List<Support>();

        public int QuotationId { get; set; }
        public Quotation Quotation { get; set; }

        //public int ContractId { get; set; }
        //public Contract Contract { get; set; }
    }
}
