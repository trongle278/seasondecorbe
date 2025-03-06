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
        Task<BaseResponse> AdvanceBookingPhaseAsync(int bookingId);
        Task<BaseResponse> ApproveSurveyAndDepositAsync(int bookingId, double depositAmount);
        Task<BaseResponse> CompleteBookingAsync(int bookingId, double finalAmount);       
        Task<BaseResponse> GetBookingHistoryAsync(int accountId);
        Task<BaseResponse> CancelBookingAsync(int bookingId);
    }
}
