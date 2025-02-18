using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IProductService
    {
        Task<BaseResponse> GetAllProduct(); 
        Task<BaseResponse> GetProductById(int id);
        Task<BaseResponse> CreateProduct(ProductRequest request);
        Task<BaseResponse> UpdateProduct(int id, ProductRequest request);
        Task<BaseResponse> DeleteProduct(int id);
    }
}
