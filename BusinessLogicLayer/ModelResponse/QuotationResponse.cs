using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse
{
    public class QuotationDetailResponse
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal MaterialCost { get; set; }
        public decimal ConstructionCost { get; set; }
        public decimal TotalCost => MaterialCost + ConstructionCost;
        public DateTime CreatedAt { get; set; }

        public List<MaterialDetailResponse> Materials { get; set; }
        public List<ConstructionDetailResponse> ConstructionTasks { get; set; }
    }

    public class MaterialDetailResponse
    {
        public int Id { get; set; }
        public string MaterialName { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal TotalCost => Quantity * Cost;
        public MaterialDetail.MaterialCategory Category { get; set; }
    }

    public class ConstructionDetailResponse
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public decimal Cost { get; set; }
        public string Unit { get; set; }
    }

    public class QuotationResponse
    {
        public int Id { get; set; }
        public string QuotationCode { get; set; }
        public decimal MaterialCost { get; set; }
        public decimal ConstructionCost { get; set; }
        public decimal DepositPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FilePath { get; set; }
        public List<MaterialDetailResponse> MaterialDetails { get; set; }
        public List<ConstructionDetailResponse> ConstructionDetails { get; set; }
        //public BookingInfoResponse BookingInfo { get; set; }
    }

    public class QuotationResponseForProvider : QuotationResponse
    {
        public CustomerResponse Customer { get; set; }
    }
}
