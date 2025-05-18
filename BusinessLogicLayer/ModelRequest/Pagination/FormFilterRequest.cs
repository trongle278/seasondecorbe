using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest.Pagination
{
    public class FormFilterRequest
    {
        public string? SpaceStyle { get; set; }
        public double? MinSize { get; set; }
        public double? MaxSize { get; set; }
        public string? Style { get; set; }
        public string? ThemeColor { get; set; }
        public string? PrimaryUser { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
    }
}
