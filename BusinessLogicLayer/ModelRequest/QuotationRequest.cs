using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.ModelRequest
{
    public class QuotationFilterRequest
    {
        public string? SortBy { get; set; } = "CreateAt";
        public bool Descending { get; set; } = false;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
