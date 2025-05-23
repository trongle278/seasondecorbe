using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse.Review;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelResponse.Product
{
    public class CreateProductResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public decimal ProductPrice { get; set; }
        public int? Quantity { get; set; }
        public string? MadeIn { get; set; }
        public string? ShipFrom { get; set; }
        public int CategoryId { get; set; }
        public int AccountId { get; set; }
        public DateTime CreateAt { get; set; }
        public List<IFormFile> Images { get; set; }
    }
    public class UpdateProductResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public decimal ProductPrice { get; set; }
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
        public int? Quantity { get; set; }
        public double Rate { get; set; }
        public decimal ProductPrice { get; set; }
        public int TotalSold { get; set; }
        public string Status { get; set; }
        public ProductCategoryResponse ProductCategory { get; set; }
        public List<string>? ImageUrls { get; set; }
        public List<string> Seasons { get; set; }
    }

    public class ProductDetailResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public double Rate { get; set; }
        public int TotalRate { get; set; }
        public int TotalSold { get; set; }
        public int FavoriteCount { get; set; }
        public string? Description { get; set; }
        public decimal ProductPrice { get; set; }
        public int? Quantity { get; set; }
        public string Status { get; set; }
        public string? MadeIn { get; set; }
        public string? ShipFrom { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<string>? ImageUrls { get; set; }
        public List<string> Seasons { get; set; }
        public ProductProviderResponse? Provider { get; set; }
        public List<ReviewResponse>? Reviews { get; set; }
    }

    public class RelatedProductResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public double Rate { get; set; }
        public decimal ProductPrice { get; set; }
        public int TotalSold { get; set; }
        public int? Quantity { get; set; }
        public string Status { get; set; }
        public List<string>? ImageUrls { get; set; }
        public string Category { get; set; }
        public List<string> Seasons { get; set; }
    }

    public class ServiceRelatedProductResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? Description {  get; set; }
        public double Rate { get; set; }
        public decimal ProductPrice { get; set; }
        public int TotalSold { get; set; }
        public int? Quantity { get; set; }
        public string Status { get; set; }
        public List<string>? ImageUrls { get; set; }
        public string Category { get; set; }
        public List<string> Seasons { get; set; }
    }

    public class RelatedProductItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class ProductServiceResponse
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public int AccountId { get; set; }
        public int TotalItem { get; set; }
        public decimal TotalPrice { get; set; }
        public List<RelatedProductItemResponse> RelatedProductItems { get; set; }
    }
}
