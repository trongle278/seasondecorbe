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
        Task<BaseResponse> SendProviderInvitationEmailAsync(string email);
        Task<BaseResponse> CreateProviderProfileAsync(int accountId, BecomeProviderRequest request);
    }
}
