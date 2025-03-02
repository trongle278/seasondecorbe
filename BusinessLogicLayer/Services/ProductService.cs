using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Product;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
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
                Expression<Func<Product, object>>[] includeProperties = { p => p.ProductImages };
                var products = await _unitOfWork.ProductRepository.GetAllAsync(includeProperties);

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get review of product
                    var reviews = await _unitOfWork.ReviewRepository
                                        .Query(r => r.ProductId == product.Id)
                                        .ToListAsync();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                    // Get total product sold
                    var productOrder = await _unitOfWork.ProductOrderRepository
                                            .Query(po => po.ProductId == product.Id 
                                                        && po.Order.Status == Order.OrderStatus.Completed)
                                            .ToListAsync();

                    // Calculate total sold
                    var totalSold = productOrder.Sum(oi => oi.Quantity);

                    var productResponse = new ProductListResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Rate = averageRate,
                        ProductPrice = product.ProductPrice,
                        TotalSold = totalSold,
                        ImageUrls = product.ProductImages?.FirstOrDefault()?.ImageUrl != null
                            ? new List<string> { product.ProductImages.FirstOrDefault()?.ImageUrl }
                            : new List<string>()
                    };

                    productResponses.Add(productResponse);
                }

                response.Success = true;
                response.Message = "Product list retrieved successfully";
                response.Data = productResponses;
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
                var product = await _unitOfWork.ProductRepository
                                        .Query(p => p.Id == id)
                                        .Include(p => p.ProductImages)
                                        .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                // Get review of product
                var reviews = await _unitOfWork.ReviewRepository
                                    .Query(r => r.ProductId == product.Id)
                                    .ToListAsync();

                // Calculate average rate
                var averageRate = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                // Calculate total rate
                var totalRate = reviews.Sum(r => r.Rating);

                // Get total product sold
                var productOrder = await _unitOfWork.ProductOrderRepository
                                        .Query(po => po.ProductId == product.Id
                                                    && po.Order.Status == Order.OrderStatus.Completed)
                                        .ToListAsync();

                // Calculate total sold
                var totalSold = productOrder.Sum(oi => oi.Quantity);

                var productDetailResponse = new ProductDetailResponse
                {
                    Id = product.Id,
                    ProductName = product.ProductName,
                    Rate = averageRate,
                    TotalRate = totalRate,
                    TotalSold = totalSold,
                    Description = product.Description,
                    ProductPrice = product.ProductPrice,
                    Quantity = product.Quantity,
                    MadeIn = product.MadeIn,
                    ShipFrom = product.ShipFrom,
                    CategoryId = product.CategoryId,
                    ImageUrls = product.ProductImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>()
                };

                response.Success = true;
                response.Message = "Product retrieved successfully";
                response.Data = productDetailResponse;
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

                // CreateProduct
                var product = _mapper.Map<Product>(request);
                product.ProductImages = new List<ProductImage>();

                // Create ProductImage
                if (request.ImageUrls != null && request.ImageUrls.Any())
                {
                    foreach (var imageUrl in request.ImageUrls.Take(5)) // Limit to 5 images
                    {
                        var productImage = new ProductImage
                        {
                            ImageUrl = imageUrl,
                            Product = product
                        };
                        product.ProductImages.Add(productImage);
                    }
                }

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

                var product = await _unitOfWork.ProductRepository
                                        .Query(p => p.Id == id)
                                        .Include(p => p.ProductImages)
                                        .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                // Check if new imageUrl provided
                if (request.ImageUrls != null && request.ImageUrls.Any())
                {
                    // Delete all old images
                    product.ProductImages.Clear();

                    // Add new images
                    foreach (var imageUrl in request.ImageUrls)
                    {
                        product.ProductImages.Add(new ProductImage
                        {
                            ImageUrl = imageUrl
                        });
                    }
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
                var product = await _unitOfWork.ProductRepository
                                        .Query(p => p.Id == id)
                                        .Include(p => p.ProductImages)
                                        .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                // Delete ProductImages
                if (product.ProductImages != null && product.ProductImages.Any())
                {
                    _unitOfWork.ProductImageRepository.RemoveRange(product.ProductImages);
                }

                // Delete Product
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
