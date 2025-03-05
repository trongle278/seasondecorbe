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
        Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountid);
        Task<BaseResponse> ConfirmBookingAsync(int bookingId);
        Task<BaseResponse> StartSurveyAsync(int bookingId);
        Task<BaseResponse> ApproveSurveyAndDepositAsync(int bookingId, Payment depositPayment);
        Task<BaseResponse> StartProgressAsync(int bookingId);
        Task<BaseResponse> CompleteBookingAsync(int bookingId, Payment finalPayment);
        Task<BaseResponse> CancelBookingAsync(int bookingId);
    }
}
