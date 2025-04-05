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
    }

    /// <summary>
    /// Request để thêm tracking tiến độ thi công vào booking
    /// </summary>
    public class TrackingRequest
    {
        [Required]
        [StringLength(255)]
        public string Stage { get; set; }

        public DateTime? PlannedDate { get; set; }

        public DateTime? ActualDate { get; set; }

        public string ImageUrls { get; set; }
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
    }

    public class ConstructionItemRequest
    {
        public string TaskName { get; set; }
        public decimal Cost { get; set; }
        public string Unit { get; set; }
        // Chỉ áp dụng khi đơn vị là "m²"
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
    }

    public class UpdateTrackingRequest
    {
        [Required]
        public int BookingId { get; set; } // ✅ Nhập BookingId thay vì TrackingId

        public string? Note { get; set; }

        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
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
        public string? Status { get; set; }
        public int? DecorServiceId { get; set; }
        public string? SortBy { get; set; } = "CreateAt";
        public bool Descending { get; set; } = false;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
