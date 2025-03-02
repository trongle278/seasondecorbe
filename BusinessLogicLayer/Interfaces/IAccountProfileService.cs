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
    public interface IAccountProfileService
    {
        Task<AccountResponse> GetAccountByIdAsync(int accountId);
        Task<AccountListResponse> GetAllAccountsAsync();
        Task<BaseResponse> UpdateAccountAsync(int accountId, UpdateAccountRequest request);
        Task<BaseResponse> UpdateAvatarAsync(int accountId, Stream fileStream, string fileName);
    }
}
