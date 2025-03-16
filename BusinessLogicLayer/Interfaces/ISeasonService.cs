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
    public interface ISeasonService
    {
        Task<BaseResponse<IEnumerable<Season>>> GetAllSeasonsAsync();
        Task<BaseResponse<Season>> GetSeasonByIdAsync(int id);
        Task<BaseResponse> CreateSeasonAsync(SeasonRequest request);
        Task<BaseResponse> UpdateSeasonAsync(int id, SeasonRequest request);
        Task<BaseResponse> DeleteSeasonAsync(int id);
    }
}
