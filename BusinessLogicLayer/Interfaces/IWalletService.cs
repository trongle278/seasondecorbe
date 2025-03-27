using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface IWalletService
    {
        Task<Boolean> CreateWallet(int accountId);
        Task<Boolean> UpdateWallet(int walletId, decimal amount);

        Task<BaseResponse<List<WalletTransactionResponse>>> GetWalletTransactions(int walletId);
        Task<BaseResponse<List<WalletTransactionResponse>>> GetTransactionsByType(int walletId, PaymentTransaction.EnumTransactionType type);
        Task<BaseResponse<List<WalletTransactionResponse>>> GetTransactionsByStatus(int walletId, PaymentTransaction.EnumTransactionStatus status);
        Task<BaseResponse<List<WalletTransactionResponse>>> GetTransactionsByDateRange(int walletId, DateTime startDate, DateTime endDate);
        Task<BaseResponse<WalletTransactionResponse>> GetTransactionDetail(int transactionId);
    }
}
