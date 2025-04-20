using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse.Payment;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IPaymentService
    {
        Task<bool> TopUp(int accountId, decimal amount);
        Task<bool> Deposit(int customerId, int providerId, decimal amount, int bookingId);
        Task<bool> FinalPay(int accountId, decimal remainBookingAmount, int providerId, int bookingId, decimal commissionRate);
        Task<bool> OrderPay(int customerId, int providerId, int orderId, decimal amount, decimal commissionRate);
        Task<bool> Refund(int accountId, decimal amount, int bookingId, int adminId);

        Task<BaseResponse<DepositPaymentResponse>> GetDepositPaymentAsync(string contractCode);
        Task<BaseResponse<FinalPaymentResponse>> GetFinalPaymentAsync(string contractCode);
    }
}
