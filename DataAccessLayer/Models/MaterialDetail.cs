using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class MaterialDetail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Quotation")]
        public int QuotationId { get; set; }
        public virtual Quotation Quotation { get; set; }

        public string MaterialName { get; set; } // Ví dụ: "Sơn Dulux", "Ghế sofa"

        public int Quantity { get; set; } // Số lượng nguyên liệu

        public decimal Cost { get; set; } // Giá tiền của nguyên liệu đó (đơn giá)
    }
}
