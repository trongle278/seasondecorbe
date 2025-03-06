using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelRequest.Product
{
    public class CreateProductRequest
    {
        [Required]
        public string ProductName { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Product price cannot be negative.")]
        public double ProductPrice { get; set; }
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Product quantity cannot be negative.")]
        public int? Quantity { get; set; }
        public string? MadeIn { get; set; }
        public string? ShipFrom { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public int ProviderId { get; set; }
        public List<IFormFile> Images { get; set; }
    }

    public class UpdateProductRequest
    {
        [Required]
        public string ProductName { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Product price cannot be negative.")]
        public double ProductPrice { get; set; }
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Product quantity cannot be negative.")]
        public int? Quantity { get; set; }
        public string? MadeIn { get; set; }
        public string? ShipFrom { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public List<IFormFile> Images { get; set; }
    }

    public class ProductListRequest
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public double Rate { get; set; }
        public double ProductPrice { get; set; }
        public int TotalSold { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class ProductDetailRequest
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