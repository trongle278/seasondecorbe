using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelResponse.Product
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public double ProductPrice { get; set; }
        public int? Quantity { get; set; }
        public string? MadeIn { get; set; }
        public string? ShipFrom { get; set; }
        public int CategoryId { get; set; }
        public List<IFormFile> Images { get; set; }
    }

    public class ProductListResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public double Rate { get; set; }
        public double ProductPrice { get; set; }
        public int TotalSold { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class ProductDetailResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public double Rate { get; set; }
        public int TotalRate { get; set; }
        public int TotalSold { get; set; }
        public string? Description { get; set; }
        public double ProductPrice { get; set; }
        public int? Quantity { get; set; }
        public string? MadeIn { get; set; }
        public string? ShipFrom { get; set; }
        public int CategoryId { get; set; }
        public List<string>? ImageUrls { get; set; }
    }
}
