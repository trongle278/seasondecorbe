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
        Task<DecorServiceByIdResponse> GetDecorServiceByIdAsync(int id, int accountId);
        Task<DecorServiceListResponse> GetAllDecorServicesAsync();
        Task<DecorServiceBySlugResponse> GetDecorServiceBySlugAsync(string slug);
        Task<BaseResponse<PageResult<DecorServiceDTO>>> GetDecorServiceListByProvider(int accountId, ProviderServiceFilterRequest request);
        Task<BaseResponse<PageResult<DecorServiceDTO>>> GetDecorServiceListForCustomerAsync(int? providerId, DecorServiceFilterRequest request);
        Task<BaseResponse<PageResult<DecorServiceDTO>>> GetFilterDecorServicesAsync(DecorServiceFilterRequest request);
        Task<BaseResponse> UpdateDecorServiceAsync(int id, UpdateDecorServiceRequest request, int accountId);
        //Task<BaseResponse> UpdateDecorServiceAsyncWithImage(int id, UpdateDecorServiceRequest request, int accountId);
        Task<BaseResponse> DeleteDecorServiceAsync(int id, int accountId);
        Task<DecorServiceListResponse> SearchDecorServices(string keyword);
        Task<DecorServiceListResponse> SearchMultiCriteriaDecorServices(SearchDecorServiceRequest request);
        Task<BaseResponse> ChangeStartDateAsync(int decorServiceId, ChangeStartDateRequest request, int accountId);
        Task<DecorServiceListResponse> GetIncomingDecorServiceListAsync();
        Task<BaseResponse<List<DesignResponse>>> GetStylesByDecorServiceIdAsync(int decorServiceId);
        Task<BaseResponse<List<ThemeColorResponse>>> GetThemeColorsByDecorServiceIdAsync(int decorServiceId);
        Task<BaseResponse<DecorServiceDetailsResponse>> GetStyleNColorByServiceIdAsync(int decorServiceId);
        Task<BaseResponse<OfferingAndDesignResponse>> GetAllOfferingAndStylesAsync();
        Task<BaseResponse<ServiceRelatedProductPageResult>> GetRelatedProductsAsync(ServiceRelatedProductRequest request);
        Task<BaseResponse> GetAddedProductServiceAsync(int serviceId, int accountId);
        Task<BaseResponse> AddRelatedProductAsync(int serviceId, int accountId ,int productId, int quantity);
        Task<BaseResponse> UpdateQuantityAsync(int relatedProductId, int productId, int quantity);
        Task<BaseResponse> RemoveRelatedProductAsync(int relatedProductId, int productId);
    }
}
