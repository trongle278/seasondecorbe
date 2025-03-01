using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IAccountProfileService
    {
        Task<BaseResponse> UpdateSlug(int accountId, UpdateSlugRequest request);
        Task<BaseResponse> UpdateAvatarAsync(int accountId, Stream fileStream, string fileName);
    }
}
