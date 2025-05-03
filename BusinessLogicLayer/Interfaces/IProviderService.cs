using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;

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
        Task<BaseResponse> ApproveProviderAsync(int accountId);
        Task<BaseResponse> RejectProviderAsync(int accountId, string reason);
        Task<BaseResponse<List<PendingProviderResponse>>> GetPendingProviderApplicationListAsync();
        Task<BaseResponse<PendingProviderResponse>> GetPendingProviderByIdAsync(int accountId);
        Task<BaseResponse<SkillsAndStylesResponse>> GetAllSkillsAndStylesAsync();
        Task<BaseResponse<VerifiedProviderResponse>> GetVerifiedProviderByIdAsync(int accountId);
        Task<BaseResponse<List<VerifiedProviderResponse>>> GetVerifiedProvidersApplicationListAsync();
        Task<BaseResponse<PageResult<VerifiedProviderResponse>>> GetProviderApplicationFilter(ProviderApplicationFilterRequest request);
    }
}
