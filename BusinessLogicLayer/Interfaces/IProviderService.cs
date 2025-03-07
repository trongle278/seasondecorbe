using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IProviderService
    {
        Task<BaseResponse> GetAllProvidersAsync();
        Task<BaseResponse> GetProviderProfileByAccountIdAsync(int accountId);
        Task<BaseResponse> GetProviderProfileBySlugAsync(string slug);
        Task<BaseResponse> SendProviderInvitationEmailAsync(string email);
        Task<BaseResponse> CreateProviderProfileAsync(int accountId, BecomeProviderRequest request);
        Task<BaseResponse> UpdateProviderProfileByAccountIdAsync(int accountId, UpdateProviderRequest request);
        Task<BaseResponse> ChangeProviderStatusByAccountIdAsync(int accountId, bool isProvider);
    }
}
