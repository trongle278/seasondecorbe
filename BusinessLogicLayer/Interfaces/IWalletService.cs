using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Payment;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface IWalletService
    {
        Task<Boolean> CreateWallet(int accountId);
        Task<Boolean> UpdateWallet(int walletId, decimal amount);
        Task<BaseResponse<WalletResponse>> GetWalletByAccountId(int accountId);
        Task<BaseResponse<List<TransactionsResponse>>> GetAllTransactionsByAccountId(int accountId);
    }
}
