using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse.Product;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse
{
    public class BookingResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string QuotationCode { get; set; }
        public decimal TotalPrice { get; set; }
        public int Status { get; set; }
        public bool? CancelDisable { get; set; }
        public bool? IsCommitDepositPaid { get; set; }
        public bool IsQuoteExisted { get; set; }
        public bool IsContractSigned { get; set; }
        public bool IsTracked { get; set; }
        public bool IsReviewed { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ServiceItems { get; set; }
        public decimal Cost { get; set; }
        //public DateTime? EstimatedCompletion { get; set; }
        public DecorServiceDTO DecorService { get; set; } // Dịch vụ decor
        public List<ThemeColorResponse> ThemeColors { get; set; }
        public DesignResponse? Design { get; set; }
        public ProviderResponse Provider { get; set; } // Thông tin nhà cung cấp (Provider)    
        public string CancelType { get; set; }
        public string? CancelReason { get; set; }
        public BookingFormResponse? BookingForm { get; set; }
        public List<RelatedProductItemResponse>? RelatedProductItems { get; set; }
    }

    public class BookingDetailForProviderResponse
    {
        public string BookingCode { get; set; }
        public decimal TotalPrice { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal DepositAmount { get; set; }
        public string CancelType { get; set; }
        public string CancelReason { get; set; }
        public string RejectReason { get; set; }
        public List<BookingDetailResponse> BookingDetails { get; set; } = new List<BookingDetailResponse>();
        public DateTime? SurveyDate { get; set; }
        public string Address { get; set; }
        public DecorServiceDTO DecorService { get; set; }
        public CustomerResponse Customer { get; set; }

        public string DesignName { get; set; }
        public List<ThemeColorResponse> ThemeColors { get; set; } = new();

        public BookingFormResponse? BookingForm { get; set; }

        public List<RelatedProductItemResponse>? RelatedProductItems { get; set; }
    }

    public class BookingDetailResponse
    {
        public int Id { get; set; }
        public string ServiceItem { get; set; }
        public decimal Cost { get; set; }
        //public DateTime EstimatedCompletion { get; set; }
    }

    public class BookingResponseForProvider
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public decimal TotalPrice { get; set; }
        public int Status { get; set; }
        public bool? CancelDisable { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ServiceItems { get; set; }
        public decimal Cost { get; set; }
        //public DateTime? EstimatedCompletion { get; set; }
        public bool? IsCommitDepositPaid { get; set; }
        public bool IsQuoteExisted { get; set; }
        public bool IsTracked { get; set; }
        public bool IsReviewed { get; set; }
        public DecorServiceDTO DecorService { get; set; }
        public List<ThemeColorResponse> ThemeColors { get; set; }
        public DesignResponse? Design { get; set; }
        public CustomerResponse Customer { get; set; }
        //public List<BookingDetailResponse> BookingDetails { get; set; } = new List<BookingDetailResponse>();
        public BookingFormResponse? BookingForm { get; set; }
        public List<RelatedProductItemResponse>? RelatedProductItems { get; set; }
    }

    public class CustomerResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Avatar { get; set; }
        public string Slug { get; set; }
    }

    public class CancelBookingListResponse
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }  // Thêm tên khách hàng

        public string Status { get; set; }  // Trạng thái booking (e.g., PendingCancellation)
        public string CancelReason { get; set; }
        public string CustomCancelReason { get; set; }  // Lý do hủy nếu là "Other"
        public DateTime CreatedDate { get; set; }  // Ngày tạo booking
        public DateTime? RequestedCancellationDate { get; set; }  // Ngày yêu cầu hủy
    }

    public class PendingCancelBookingDetailForProviderResponse
    {
        public string BookingCode { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Avatar { get; set; }
        public string Style { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CancelTypeId { get; set; }
        public string CancelTypeName { get; set; }
        public string CancelReason { get; set; }
        public DateTime? SurveyDate { get; set; }
        public string Address { get; set; }   
    }

    public class BookingFormResponse
    {
        public int Id { get; set; }
        public string? SpaceStyle { get; set; }
        public double? RoomSize { get; set; }
        public string? Style { get; set; }
        public string? ThemeColor { get; set; }
        public string? PrimaryUser { get; set; }
        public int AccountId { get; set; }
        public List<FormImageResponse>? Images { get; set; }
        public List<ScopeOfWorkResponse>? ScopeOfWorks { get; set; }
    }

    public class FormImageResponse
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ScopeOfWorkResponse
    {
        public int Id { get; set; }
        public string WorkType { get; set; }
    }
}
