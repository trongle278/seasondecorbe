using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IFavoriteServiceService
    {
        Task<BaseResponse<List<FavoriteServiceResponse>>> GetFavoriteServicesAsync(int accountId);
        Task<BaseResponse> AddToFavoritesAsync(int accountId, int decorServiceId);
        Task<BaseResponse> RemoveFromFavoritesAsync(int accountId, int decorServiceId);
    }
}