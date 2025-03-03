using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Services
{
    public class PayosService : IPayosService
    {
        public async Task<PayosResult> PayViaPayOSAsync(double amount, int orderId, int accountId)
        {
            // Gọi API PayOS thật (HTTP request / SDK) => tuỳ logic
            // Dưới đây mình mô phỏng 1s chờ + random success/fail
            await Task.Delay(1000);
            bool isPaid = new Random().Next(0, 2) == 1; // 50% success

            if (!isPaid)
            {
                return new PayosResult
                {
                    IsSuccess = false,
                    ErrorMessage = "PayOS gateway refused the payment."
                };
            }
            return new PayosResult
            {
                IsSuccess = true
            };
        }
    }
}
