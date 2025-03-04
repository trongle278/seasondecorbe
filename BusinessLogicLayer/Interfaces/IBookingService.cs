using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IBookingService
    {
        Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId);
        Task<BaseResponse> ConfirmBookingAsync(ConfirmBookingRequest request);
        Task<BaseResponse> UpdateBookingStatusAsync(UpdateBookingStatusRequest request);
        Task<BaseResponse> MakePaymentAsync(MakePaymentRequest request, int accountId);
        Task<BaseResponse> GetBookingAsync(int bookingId);
    }
}
