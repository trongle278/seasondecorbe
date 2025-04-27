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
        public string? Note { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        //public int RequestChangeCount { get; set; }// số lần đã đổi yêu cầu
        //public bool IsAdditionalFeeCharged { get; set; } // có bị tính phí phát sinh không
        //public decimal? AdditionalCost { get; set; }   // Chi phí phát sinh riêng
        //public string ExpectedCompletion { get; set; }
        public DateTime? ConstructionDate { get; set; }
        public DateTime CreateAt { get; set; }
        public enum BookingStatus
        {
            Pending,            // Khi khách hàng tạo booking0
            Planning,           // Provider đã xác nhận và sắp xếp khảo sát1
            Quoting,            // Provider báo giá2
            Contracting,        // Provider soạn hợp đồng3
            Confirm,            // Khi customer đồng ý các điều khoản và chốt hợp đồng4
            DepositPaid,        // Đã thanh toán đặt cọc5
            Preparing,          // Chuẩn bị nguyên liệu6
            InTransit,          // Nguyên liệu được chuyển đến chỗ khách hàng7
            Progressing,        // Đang tiến hành thi công (theo dạng Tracking service)8
            AllDone,           // Đã thi công xong 9
            FinalPaid,          // Thanh toán thi công 10 
            Completed,          // Dự án hoàn thành11
            PendingCancel,      // Chờ provider duyệt hủy 12
            Canceled,           // Booking bị hủy 13
            Rejected            // Booking bị từ chối 14
        }

        public BookingStatus Status { get; set; }

        public bool? IsBooked { get; set; }
        public bool? IsTracked { get; set; }
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
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; }
        public virtual ICollection<Support> Supports { get; set; } = new List<Support>();
        public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
        public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
        public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();

        public string? CancelReason { get; set; }
        public string? RejectReason { get; set; } // Lưu lý do reject
    }
}
