using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;
using static DataAccessObject.Models.Booking;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateBookingRequest
    {
        [DefaultValue("1")]
        public int DecorServiceId { get; set; }
        [DefaultValue("1")]
        public int AddressId { get; set; }
        [DefaultValue("2025-04-30")]
        public DateTime SurveyDate { get; set; } // Ngày khảo sát
        public string? Note { get; set; }
        public int? DecorationStyleId { get; set; } // Chọn 1 style
        public List<int> ThemeColorIds { get; set; } = new(); // Chọn nhiều màu
        public string? SpaceStyle { get; set; }
        public double? RoomSize { get; set; }
        public string? Style { get; set; }
        public string? ThemeColor { get; set; }
        public string? PrimaryUser { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public List<int>? ScopeOfWorkId { get; set; }
        public List<IFormFile>? Images { get; set; }
    }

    public class UpdateBookingRequest
    {
        public int? AddressId { get; set; }
        public DateTime? SurveyDate { get; set; }
        public string? Note { get; set; }

        public int? DecorationStyleId { get; set; }         // chọn 1
        public List<int>? ThemeColorIds { get; set; }       // chọn nhiều
    }

    public class CreateQuotationRequest
    {
        [Required(ErrorMessage = "Materials list is required")]
        [MinLength(1, ErrorMessage = "At least one material item is required")]
        public List<MaterialItemRequest> Materials { get; set; }
        [Required(ErrorMessage = "Construction tasks list is required")]
        [MinLength(1, ErrorMessage = "At least one construction task is required")]
        public List<ConstructionItemRequest> ConstructionTasks { get; set; }
        public decimal DepositPercentage { get; set; }
    }
            
    public class UploadQuotationFile
    {
        [Required(ErrorMessage = "QuotationFile is required")]
        public IFormFile QuotationFile { get; set; }
    }

    public class MaterialItemRequest
    {
        public string MaterialName { get; set; }
        public int Quantity { get; set; }       
        public decimal Cost { get; set; }
        public string Note { get; set; }
        //public MaterialDetail.MaterialCategory Category { get; set; }
    }

    public class ConstructionItemRequest
    {
        public string TaskName { get; set; }
        public decimal Cost { get; set; }
        public string Unit { get; set; }
        // Chỉ áp dụng khi đơn vị là "m²"
        public decimal? Area { get; set; }
        public string Note { get; set; }
    }

    public class ProductItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CancelBookingRequest
    {
        [Required(ErrorMessage = "CancelTypeId is required.")]
        public int CancelTypeId { get; set; }
        public string? CancelReason { get; set; }
    }

    public class RejectBookingRequest
    {
        public string Reason { get; set; }
    }

    public class BookingFilterRequest
    {
        public BookingStatus? Status { get; set; }
        public int? DecorServiceId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool Descending { get; set; } = false;      
    }

    public class BookingFormRequest
    {
        public string? SpaceStyle { get; set; }
        public double? RoomSize { get; set; }
        public string? Style { get; set; }
        public string? ThemeColor { get; set; }
        public string? PrimaryUser { get; set; }
        public List<int>? ScopeOfWorkId { get; set; }
        public List<IFormFile>? Images { get; set; }
    }
}
