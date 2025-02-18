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
using Microsoft.Extensions.DependencyInjection;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse> GetAllProduct()
        {
            var response = new BaseResponse();
            try
            {
                var product = await _unitOfWork.ProductRepository.GetAllAsync();
                response.Success = true;
                response.Message = "Product list retrieved successfully";
                response.Data = _mapper.Map<List<ProductResponse>>(product);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving product list";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> GetProductById(int id)
        {
            var response = new BaseResponse();
            try
            {
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);

                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                response.Success = true;
                response.Message = "Product retrieved successfully";
                response.Data = _mapper.Map<ProductResponse>(product);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving product";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CreateProduct(ProductRequest request)
        {
            var response = new BaseResponse();
            try
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product request";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.ProductName))
                {
                    response.Success = false;
                    response.Message = "Product name is required";
                    return response;
                }

                if (request.ProductPrice < 0)
                {
                    response.Success = false;
                    response.Message = "Negative product price";
                    return response;
                }

                if (request.Quantity < 0)
                {
                    response.Success = false;
                    response.Message = "Negative quantity";
                    return response;
                }

                var product = _mapper.Map<Product>(request);
                await _unitOfWork.ProductRepository.InsertAsync(product);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product created successfully";
                response.Data = _mapper.Map<ProductResponse>(product);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating product";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateProduct(int id, ProductRequest request)
        {
            var response = new BaseResponse();
            try
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.ProductName))
                {
                    response.Success = false;
                    response.Message = "Product name is required";
                    return response;
                }

                if (request.ProductPrice < 0)
                {
                    response.Success = false;
                    response.Message = "Negative product price";
                    return response;
                }

                if (request.Quantity < 0)
                {
                    response.Success = false;
                    response.Message = "Negative quantity";
                    return response;
                }

                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);

                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                _mapper.Map(request, product);
                _unitOfWork.ProductRepository.Update(product);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product updated successfully";
                response.Data = _mapper.Map<ProductResponse>(product);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating product";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> DeleteProduct(int id)
        {
            var response = new BaseResponse();
            try
            {
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);

                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                _unitOfWork.ProductRepository.Delete(product.Id);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product deleted successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting product";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
