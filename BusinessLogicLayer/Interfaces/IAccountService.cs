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
    public interface IAccountService
    {
        Task<AccountResponse> GetAccountByIdAsync(int accountId);
        Task<AccountListResponse> GetAllAccountsAsync();
        Task<BaseResponse> CreateAccountAsync(CreateAccountRequest request);
        Task<BaseResponse> UpdateAccountAsync(int accountId, UpdateAccountRequest request);
        Task<BaseResponse> DeleteAccountAsync(int accountId);
    }
}
