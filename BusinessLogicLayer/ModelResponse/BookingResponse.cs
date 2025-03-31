using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class BookingResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public decimal TotalPrice { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ServiceItems { get; set; }
        public decimal Cost { get; set; }
        public DateTime? EstimatedCompletion { get; set; }
        public DecorServiceDTO DecorService { get; set; } // Dịch vụ decor
        public ProviderResponse Provider { get; set; } // Thông tin nhà cung cấp (Provider)
        //public List<BookingDetailResponse> BookingDetails { get; set; } = new List<BookingDetailResponse>();
    }
    public class BookingDetailResponse
    {
        public int Id { get; set; }
        public string ServiceItem { get; set; }
        public decimal Cost { get; set; }
        public DateTime EstimatedCompletion { get; set; }
    }

    public class BookingResponseForProvider
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public decimal TotalPrice { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ServiceItems { get; set; }
        public decimal Cost { get; set; }
        public DateTime? EstimatedCompletion { get; set; }
        public DecorServiceDTO DecorService { get; set; }
        public CustomerResponse Customer { get; set; }
        //public List<BookingDetailResponse> BookingDetails { get; set; } = new List<BookingDetailResponse>();
    }

    public class CustomerResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Avatar { get; set; }
    }
}
