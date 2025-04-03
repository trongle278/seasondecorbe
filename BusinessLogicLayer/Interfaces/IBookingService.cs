using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using DataAccessObject.Models;
using static DataAccessObject.Models.Booking;

namespace BusinessLogicLayer.Interfaces
{
    public interface IBookingService
    {
        Task<BaseResponse<List<BookingResponse>>> GetBookingsByUserAsync(int accountId);
        Task<BaseResponse<List<BookingResponse>>> GetPendingCancellationBookingsForProviderAsync(int providerId);
        //Task<BaseResponse<BookingResponseForProvider>> GetBookingDetailsForProviderAsync(string bookingCode, int providerId);
        Task<BaseResponse<BookingDetailForProviderResponse>> GetBookingDetailForProviderAsync(string bookingCode, int accountId);
        Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId);
        Task<BaseResponse<bool>> ChangeBookingStatusAsync(string bookingCode);
        Task<BaseResponse> RequestCancellationAsync(string bookingCode, int accountId, int cancelTypeId, string? cancelReason);
        Task<BaseResponse> ApproveCancellationAsync(string bookingCode, int providerId);
        Task<BaseResponse> RevokeCancellationRequestAsync(string bookingCode, int accountId);
        Task<BaseResponse> RejectBookingAsync(string bookingCode, int accountId, string reason);
        Task<BaseResponse> ProcessDepositAsync(string bookingCode);
        Task<BaseResponse> ProcessFinalPaymentAsync(string bookingCode);
        Task<BaseResponse<PageResult<BookingResponse>>> GetPaginatedBookingsForCustomerAsync(BookingFilterRequest request, int accountId);
        Task<BaseResponse<PageResult<BookingResponseForProvider>>> GetPaginatedBookingsForProviderAsync(BookingFilterRequest request, int providerId);
    }
}
