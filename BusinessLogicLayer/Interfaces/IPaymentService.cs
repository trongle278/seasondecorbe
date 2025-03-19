using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Interfaces
{
    public interface IPaymentService
    {
        Task<bool> TopUp(int accountId, decimal amount);
        Task<bool> Deposit(int customerId, int adminId, decimal amount, int bookingId);
        Task<bool> Refund(int accountId, decimal amount, int bookingId, int adminId);
        Task<bool> Pay(int accountId, decimal bookingAmount, int providerId, int bookingId);
    }
}
