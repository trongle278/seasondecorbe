using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using BusinessLogicLayer.ModelResponse.Product;

namespace BusinessLogicLayer.Interfaces
{
    public interface IProductService
    {
        Task<BaseResponse> GetAllProduct();
        Task<BaseResponse<PageResult<ProductListResponse>>> GetPaginate(ProductFilterRequest request);
        Task<BaseResponse> GetProductById(int id);
        Task<BaseResponse> GetProductByCategoryId(int id);
        Task<BaseResponse<PageResult<ProductListResponse>>> GetPaginateByCategory(FilterByCategoryRequest request);
        Task<BaseResponse> GetProductByProvider(string slug);
        Task<BaseResponse<PageResult<ProductListResponse>>> GetPaginateByProvider(FilterByProviderRequest request);
        Task<BaseResponse> CreateProduct(CreateProductRequest request);
        Task<BaseResponse> UpdateProduct(int id, UpdateProductRequest request);
        Task<BaseResponse> DeleteProduct(int id);
    }
}
