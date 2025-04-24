using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest.Pagination
{
    public class ProductFilterRequest
    {
        public int? UserId { get; set; }
        public string? ProductName { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
    }

    public class FilterByCategoryRequest
    {
        [Required]
        public int CategoryId { get; set; }
        public int? UserId { get; set; }
        public string? ProductName { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
    }

    public class FilterByProviderRequest
    {
        [Required]
        public string Slug { get; set; }
        public int? UserId { get; set; }
        public string? ProductName { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
    }

    public class PagingRelatedProductRequest
    {
        [Required]
        public string QuotationCode { get; set; }
        public int UserId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
    }
}
