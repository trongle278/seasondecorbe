using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface IBookingService
    {
        Task<BaseResponse<List<BookingResponse>>> GetBookingsByUserAsync(int accountId);
        Task<BaseResponse<BookingResponse>> GetBookingDetailsAsync(int bookingId);
        Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId);
        Task<BaseResponse<bool>> ChangeBookingStatusAsync(int bookingId);
        Task<BaseResponse<bool>> CancelBookingAsync(int bookingId);
        Task<BaseResponse> ProcessDepositAsync(int bookingId);
        Task<BaseResponse> ProcessFinalPaymentAsync(int bookingId);
        Task<BaseResponse> RejectBookingAsync(int bookingId, int accountId, string reason);
        Task<BaseResponse<PageResult<BookingResponse>>> GetPaginatedBookingsForCustomerAsync(BookingFilterRequest request, int accountId);
        Task<BaseResponse<PageResult<BookingResponseForProvider>>> GetPaginatedBookingsForProviderAsync(BookingFilterRequest request, int providerId);
    }
}
