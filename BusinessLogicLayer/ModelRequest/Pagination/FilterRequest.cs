using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest.Pagination
{
    public class FilterRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
    }
}
