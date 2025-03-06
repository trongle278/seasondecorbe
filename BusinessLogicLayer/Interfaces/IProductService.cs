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
        Task<BaseResponse> GetProductByCategoryId(int id);
        Task<BaseResponse> GetProductByProviderId(int id);
        Task<BaseResponse> CreateProduct(CreateProductRequest request);
        Task<BaseResponse> UpdateProduct(int id, UpdateProductRequest request);
        Task<BaseResponse> DeleteProduct(int id);
    }
}
