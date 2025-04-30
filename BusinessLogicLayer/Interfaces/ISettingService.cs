using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;

namespace BusinessLogicLayer.Interfaces
{
    public interface ISettingService
    {
        Task<BaseResponse<PageResult<SettingResponse>>> GetSettingAsync(SettingFilterRequest request);
        Task<BaseResponse> GetSettingByIdAsync(int id);
        Task<BaseResponse> UpdateSettingAsync(int id, SettingRequest request);
    }
}
