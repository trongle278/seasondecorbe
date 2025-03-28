using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Payment;

namespace BusinessLogicLayer.Interfaces
{
    public interface ITransactionsService
    {
        Task<BaseResponse<List<TransactionsResponse>>> GetTransactions(int walletId);
    }
}
