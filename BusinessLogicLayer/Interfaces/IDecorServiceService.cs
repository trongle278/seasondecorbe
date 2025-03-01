using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
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
        Task<DecorServiceResponse> GetDecorServiceByIdAsync(int id);
        Task<DecorServiceListResponse> GetAllDecorServicesAsync();
        Task<BaseResponse> UpdateDecorServiceAsync(int id, UpdateDecorServiceRequest request, int accountId);
        //Task<BaseResponse> UpdateDecorServiceAsyncWithImage(int id, UpdateDecorServiceRequest request, int accountId);
        Task<BaseResponse> DeleteDecorServiceAsync(int id, int accountId);
        Task<DecorServiceListResponse> SearchDecorServices(string keyword);
    }
}
