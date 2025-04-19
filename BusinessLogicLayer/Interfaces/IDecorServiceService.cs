using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Interfaces
{
    public interface IDecorServiceService
    {
        Task<BaseResponse> CreateDecorServiceAsync(CreateDecorServiceRequest request, int accountId);
        //Task<DecorServiceResponse> GetDecorServiceByIdAsync(int id);
        Task<DecorServiceResponse> GetDecorServiceByIdAsync(int id, int accountId);
        Task<DecorServiceListResponse> GetAllDecorServicesAsync();
        Task<DecorServiceResponse> GetDecorServiceBySlugAsync(string slug);
        Task<BaseResponse<PageResult<DecorServiceDTO>>> GetDecorServiceListByProvider(int accountId, DecorServiceFilterRequest request);
        Task<BaseResponse<PageResult<DecorServiceDTO>>> GetDecorServiceListForCustomerAsync(int? providerId, DecorServiceFilterRequest request);
        Task<BaseResponse<PageResult<DecorServiceDTO>>> GetFilterDecorServicesAsync(DecorServiceFilterRequest request);
        Task<BaseResponse> UpdateDecorServiceAsync(int id, UpdateDecorServiceRequest request, int accountId);
        //Task<BaseResponse> UpdateDecorServiceAsyncWithImage(int id, UpdateDecorServiceRequest request, int accountId);
        Task<BaseResponse> DeleteDecorServiceAsync(int id, int accountId);
        Task<DecorServiceListResponse> SearchDecorServices(string keyword);
        Task<DecorServiceListResponse> SearchMultiCriteriaDecorServices(SearchDecorServiceRequest request);
        Task<BaseResponse> ChangeStartDateAsync(int decorServiceId, ChangeStartDateRequest request, int accountId);

    }
}
