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
        Task<BaseResponse<List<Booking>>> GetBookingsByUserAsync(int accountId);
        Task<BaseResponse<Booking>> GetBookingDetailsAsync(int bookingId);
        Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId);
        Task<BaseResponse<bool>> ChangeBookingStatusAsync(int bookingId);
        Task<BaseResponse<bool>> CancelBookingAsync(int bookingId);
        Task<BaseResponse> ProcessDepositAsync(int bookingId);
        Task<BaseResponse> ProcessConstructionPaymentAsync(int bookingId);
    }
}
