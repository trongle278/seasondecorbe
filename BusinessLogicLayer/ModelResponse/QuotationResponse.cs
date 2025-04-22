using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse.Product;
using DataAccessObject.Models;
using static DataAccessObject.Models.Quotation;

namespace BusinessLogicLayer.ModelResponse
{
    public class QuotationDetailResponse
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string QuotationCode { get; set; }
        public string Style { get; set; }
        public decimal MaterialCost { get; set; }
        public decimal ConstructionCost { get; set; }
        public decimal? ProductCost { get; set; }
        public decimal DepositPercentage { get; set; }
        public string? QuotationFilePath { get; set; }
        public int Status { get; set; }
        public decimal TotalCost => MaterialCost + ConstructionCost + (ProductCost ?? 0);
        public bool IsQuoteExisted { get; set; }
        public bool? IsContractExisted { get; set; }
        public bool IsSigned { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<MaterialDetailResponse> Materials { get; set; }
        public List<ConstructionDetailResponse> ConstructionTasks { get; set; }
        public List<ProductsDetailResponse> ProductDetails { get; set; }
    }

    public class MaterialDetailResponse
    {
        public int Id { get; set; }
        public string MaterialName { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal TotalCost => Quantity * Cost;
        //public MaterialDetail.MaterialCategory Category { get; set; }
    }

    public class ConstructionDetailResponse
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public decimal Cost { get; set; }
        public string Unit { get; set; }
        public decimal? Area { get; set; }
    }

    public class ProductsDetailResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class QuotationResponse
    {
        public int Id { get; set; }
        public string QuotationCode { get; set; }
        public string Style { get; set; }
        public decimal MaterialCost { get; set; }
        public decimal ConstructionCost { get; set; }
        public decimal? ProductCost { get; set; }
        public decimal DepositPercentage { get; set; }
        public bool IsQuoteExisted { get; set; }
        public bool? IsContractExisted { get; set; }
        public bool IsSigned { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
        public string FilePath { get; set; }
        public List<MaterialDetailResponse> MaterialDetails { get; set; }
        public List<ConstructionDetailResponse> ConstructionDetails { get; set; }
        public List<ProductsDetailResponse> ProductDetails { get; set; }
    }

    public class QuotationResponseForCustomer : QuotationResponse 
    {
        public ProviderResponse Provider { get; set; }
    }

    public class QuotationDetailResponseForCustomer : QuotationDetailResponse
    {
        public ProviderResponse Provider { get; set; }
    }

    public class QuotationResponseForProvider : QuotationResponse
    {
        public CustomerResponse Customer { get; set; }
    }
}
