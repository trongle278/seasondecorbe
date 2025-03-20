using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Contract
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Liên kết với Booking
        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        // Đường dẫn nơi lưu file PDF hợp đồng
        public string ContractFilePath { get; set; }

        // Ngày hợp đồng được ký (nếu có)
        public DateTime? SignedDate { get; set; }

        // Trạng thái hợp đồng (ví dụ: Pending, Signed, Cancelled)
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
