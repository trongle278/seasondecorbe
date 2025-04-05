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
            Pending,            // Khi khách hàng tạo booking0
            Planning,             // Provider đã xác nhận và sắp xếp khảo sát1
            Confirm,            // Khi customer đồng ý các điều khoản và chốt hợp đồng2
            DepositPaid,        // Đã thanh toán đặt cọc3
            Preparing,          // Chuẩn bị nguyên liệu4
            InTransit,          // Nguyên liệu được chuyển đến chỗ khách hàng5
            Progressing,        // Đang tiến hành thi công (theo dạng Tracking service)6
            ConstructionPayment,// Thanh toán thi công7
            Completed,          // Dự án hoàn thành8
            PendingCancellation, // Chờ provider duyệt hủy9
            Canceled,          // Booking bị hủy10
            Rejected           // Booking bị từ chối11
        }

        public BookingStatus Status { get; set; }


        public int? CancelTypeId { get; set; }
        public virtual CancelType CancelType { get; set; }  // Navigation property

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
        public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();

        public int QuotationId { get; set; }
        public Quotation Quotation { get; set; }

        public string? CancelReason { get; set; }
        public string? RejectReason { get; set; } // Lưu lý do reject

        //public int ContractId { get; set; }
        //public Contract Contract { get; set; }
    }
}
