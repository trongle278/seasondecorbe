using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Product;
using DataAccessObject.Models;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductCategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse> GetAllProductCategory()
        {
            var response = new BaseResponse();
            try
            {
                var productCategory = await _unitOfWork.ProductCategoryRepository.GetAllAsync();
                response.Success = true;
                response.Message = "Product category list retrieved successfully";
                response.Data = _mapper.Map<List<ProductCategoryResponse>>(productCategory);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving product category list";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> GetProductCategoryById(int id)
        {
            var response = new BaseResponse();
            try
            {
                var productCategory = await _unitOfWork.ProductCategoryRepository.GetByIdAsync(id);

                if (productCategory == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product category";
                    return response;
                }

                response.Success = true;
                response.Message = "Product category retrieved successfully";
                response.Data = _mapper.Map<ProductCategoryResponse>(productCategory);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving product category";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CreateProductCategory(ProductCategoryRequest request)
        {
            var response = new BaseResponse();
            try
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product category request";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    response.Success = false;
                    response.Message = "Product category name is required";
                    return response;
                }

                var productCategory = _mapper.Map<ProductCategory>(request);
                await _unitOfWork.ProductCategoryRepository.InsertAsync(productCategory);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product category created successfully";
                response.Data = _mapper.Map<ProductCategoryResponse>(productCategory);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating product category";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateProductCategory(int id, ProductCategoryRequest request)
        {
            var response = new BaseResponse();
            try
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product category";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    response.Success = false;
                    response.Message = "Product category name is required";
                    return response;
                }

                var productCategory = await _unitOfWork.ProductCategoryRepository.GetByIdAsync(id);

                if (productCategory == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product category";
                    return response;
                }

                _mapper.Map(request, productCategory);
                _unitOfWork.ProductCategoryRepository.Update(productCategory);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product category updated successfully";
                response.Data = _mapper.Map<ProductCategoryResponse>(productCategory);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating product category";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> DeleteProductCategory(int id)
        {
            var response = new BaseResponse();
            try
            {
                var productCategory = await _unitOfWork.ProductCategoryRepository.GetByIdAsync(id);

                if (productCategory == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product category";
                    return response;
                }

                _unitOfWork.ProductCategoryRepository.Delete(productCategory.Id);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product category deleted successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting product category";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
