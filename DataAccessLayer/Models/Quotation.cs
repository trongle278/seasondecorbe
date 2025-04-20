using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Quotation
    {
        [Key]
        public int Id { get; set; }
        public string QuotationCode { get; set; }

        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; }
        public decimal MaterialCost { get; set; } // Chi phí nguyên liệu
        public decimal ConstructionCost { get; set; } // Chi phí thi công
        public decimal? ProductCost { get; set; }
        public decimal DepositPercentage { get; set; }
        public string? QuotationFilePath { get; set; }
        public QuotationStatus Status { get; set; }
        public enum QuotationStatus
        {
            Pending,
            Confirmed,    // Đồng ý bảng báo giá         
            Denied        // Từ chối bảng báo giá        
        }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool isQuoteExisted { get; set; }
        public Contract? Contract { get; set; }
        public virtual ICollection<MaterialDetail> MaterialDetails { get; set; }
        public virtual ICollection<LaborDetail> LaborDetails { get; set; }
        public virtual ICollection<ProductDetail>? ProductDetails { get; set; }
    }
}
