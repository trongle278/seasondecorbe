using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Favorite;

namespace BusinessLogicLayer.Interfaces
{
    public interface IFavoriteProductService
    {
        Task<BaseResponse<List<FavoriteProductResponse>>> GetFavoriteProduct(int accountId);
        Task<BaseResponse> AddToFavorite(int accountId, int productId);
        Task<BaseResponse> RemoveFromFavorite(int accountId, int productId);
    }
}
