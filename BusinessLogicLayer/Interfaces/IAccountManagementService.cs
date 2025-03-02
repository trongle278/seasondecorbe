using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IAccountManagementService
    {
        Task<BaseResponse> GetAllAccountsAsync();
        Task<BaseResponse> GetAccountByIdAsync(int accountId);
        Task<BaseResponse> CreateAccountAsync(CreateAccountRequest request);
        Task<BaseResponse> UpdateAccountAsync(int accountId, UpdateAccountRequest request);
        Task<BaseResponse> BanAccountAsync(int accountId);
        Task<BaseResponse> UnbanAccountAsync(int accountId);
    }
}
