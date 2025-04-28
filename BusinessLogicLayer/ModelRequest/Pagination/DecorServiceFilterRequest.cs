using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataAccessObject.Models.DecorService;

namespace BusinessLogicLayer.ModelRequest.Pagination
{
    public class DecorServiceFilterRequest
    {
        public string? Style { get; set; }
        public string? Sublocation { get; set; }
        public DateTime? StartDate { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public int? DecorCategoryId { get; set; }
        public List<int>? SeasonIds { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
    }

    public class ProviderServiceFilterRequest
    {
        public DecorServiceStatus? Status { get; set; }
        public string? Style { get; set; }
        public string? Sublocation { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public int? DecorCategoryId { get; set; }
        public List<int>? SeasonIds { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
    }
}
