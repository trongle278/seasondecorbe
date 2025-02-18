using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IProductCategoryService
    {
        Task<BaseResponse> GetAllProductCategory();
        Task<BaseResponse> GetProductCategoryById(int id);
        Task<BaseResponse> CreateProductCategory(ProductCategoryRequest request);
        Task<BaseResponse> UpdateProductCategory(int id, ProductCategoryRequest request);
        Task<BaseResponse> DeleteProductCategory(int id);
    }
}
