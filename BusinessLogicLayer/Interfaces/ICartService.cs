using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest.Cart;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface ICartService
    {
        Task<BaseResponse> CreateCartAsync(CartRequest request);
        Task<BaseResponse> GetCart(int accountId);
        Task<BaseResponse> AddToCart(int accountId, int productId, int quantity);
        Task<BaseResponse> UpdateProductQuantity(int accountId, int productId, int quantity);
        Task<BaseResponse> RemoveProduct(int accountId, int productId);
    }
}
