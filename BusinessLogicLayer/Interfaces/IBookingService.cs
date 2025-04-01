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
        Task<BaseResponse<BookingResponse>> GetBookingDetailsAsync(int bookingId);
        Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId);
        Task<BaseResponse<bool>> ChangeBookingStatusAsync(int bookingId);
        Task<BaseResponse> RequestCancellationAsync(int bookingId, int accountId, int cancelTypeId, string? cancelReason);
        Task<BaseResponse> ApproveCancellationAsync(int bookingId, int providerId);
        Task<BaseResponse> RevokeCancellationRequestAsync(int bookingId, int accountId);
        Task<BaseResponse> ProcessDepositAsync(int bookingId);
        Task<BaseResponse> ProcessFinalPaymentAsync(int bookingId);
        Task<BaseResponse> RejectBookingAsync(int bookingId, int accountId, string reason);
        Task<BaseResponse<PageResult<BookingResponse>>> GetPaginatedBookingsForCustomerAsync(BookingFilterRequest request, int accountId);
        Task<BaseResponse<PageResult<BookingResponseForProvider>>> GetPaginatedBookingsForProviderAsync(BookingFilterRequest request, int providerId);
    }
}
