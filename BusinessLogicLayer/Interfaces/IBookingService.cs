using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface IBookingService
    {
        Task<BaseResponse> GetBookingsByUserAsync(int accountId, int page = 1, int pageSize = 10);
        Task<BaseResponse> GetBookingDetailsAsync(int bookingId, int accountId);
        Task<BaseResponse> AddBookingDetailAsync(int bookingId, BookingDetailRequest request);
        Task<BaseResponse> AddTrackingAsync(int bookingId, TrackingRequest request);
        Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId);
        Task<BaseResponse> SurveyBookingAsync(int bookingId);
        Task<BaseResponse> ConfirmBookingAsync(int bookingId, decimal depositAmount);
        Task<BaseResponse> MarkDepositPaidAsync(int bookingId);
        Task<BaseResponse> MarkPreparingAsync(int bookingId);
        Task<BaseResponse> MarkInTransitAsync(int bookingId);
        Task<BaseResponse> MarkProgressingAsync(int bookingId);
        Task<BaseResponse> MarkConstructionPaymentAsync(int bookingId);
        Task<BaseResponse> CompleteBookingAsync(int bookingId);
        Task<BaseResponse> CancelBookingAsync(int bookingId);
    }
}
