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
    public interface IDecorCategoryService
    {
        Task<DecorCategoryListResponse> GetAllDecorCategoriesAsync();
        Task<DecorCategoryResponse> GetDecorCategoryByIdAsync(int categoryId);
        Task<BaseResponse> CreateDecorCategoryAsync(DecorCategoryRequest request);
        Task<BaseResponse> UpdateDecorCategoryAsync(int categoryId, DecorCategoryRequest request);
        Task<BaseResponse> DeleteDecorCategoryAsync(int categoryId);
    }
}
