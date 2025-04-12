using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class LaborDetail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Quotation")]
        public int QuotationId { get; set; }
        public virtual Quotation Quotation { get; set; }

        public string TaskName { get; set; } // Ví dụ: "Sơn tường", "Lắp đặt đèn"
        public decimal Cost { get; set; } // Chi phí nhân công cho hạng mục này
        public string Unit { get; set; } // Đơn vị tính (m2, cái, ...)
        public decimal? Area { get; set; }
    }
}
