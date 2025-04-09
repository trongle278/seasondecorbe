using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using static DataAccessObject.Models.Quotation;

namespace BusinessLogicLayer.ModelRequest
{
    public class QuotationFilterRequest
    {
        public string? QuotationCode { get; set; }
        public QuotationStatus? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool Descending { get; set; } = false; 
    }
}
