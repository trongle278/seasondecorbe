using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Common.Enums;

namespace BusinessLogicLayer.Interfaces
{
    public interface IDecoratorService
    {
        Task<BaseResponse> SendDecoratorInvitationEmailAsync(string email);
        Task<BaseResponse> CreateDecoratorProfileAsync(int accountId, BecomeDecoratorRequest request);
        Task<BaseResponse> UpdateDecoratorStatusAsync(int decoratorId, DecoratorApplicationStatus newStatus);
        Task<DecoratorResponse> GetDecoratorProfileAsync(int accountId);
    }
}
