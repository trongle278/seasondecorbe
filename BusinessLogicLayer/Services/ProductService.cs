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
using BusinessLogicLayer.ModelResponse.Review;
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
        private readonly ICloudinaryService _cloudinaryService;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
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
                    // Get productOrder of product
                    var productOrders = await _unitOfWork.ProductOrderRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Completed)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = productOrders
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                    // Calculate total sold
                    var totalSold = productOrders.Sum(oi => oi.Quantity);

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

                // Get productOrder of product
                var productOrders = await _unitOfWork.ProductOrderRepository
                                        .Query(po => po.ProductId == product.Id
                                                    && po.Order.Status == Order.OrderStatus.Completed)
                                        .Include(po => po.Order)
                                            .ThenInclude(o => o.Reviews)
                                        .ToListAsync();

                // Get review of product
                var reviews = productOrders
                                .SelectMany(po => po.Order.Reviews)
                                .ToList();

                // Calculate average rate
                var averageRate = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                // Calculate total rate
                var totalRate = reviews.Sum(r => r.Rating);

                // Calculate total sold
                var totalSold = productOrders.Sum(oi => oi.Quantity);

                // Mapping reviews to response
                var reviewResponses = reviews.Select(r => new ReviewResponse
                {
                    Rating = r.Rating,
                    Comment = r.Comment,
                    Image = r.Image,
                    CreateAt = r.CreateAt,
                    UpdateAt = r.UpdateAt
                }).ToList();

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
                    ImageUrls = product.ProductImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
                    Reviews = reviewResponses
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

        public async Task<BaseResponse> GetProductByCategoryId(int id)
        {
            var response = new BaseResponse();
            try
            {               
                var products = await _unitOfWork.ProductRepository
                                                .Query(p => p.CategoryId == id)
                                                .Include(p => p.ProductImages)
                                                .ToListAsync();

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get productOrder of product
                    var productOrders = await _unitOfWork.ProductOrderRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Completed)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = productOrders
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                    // Calculate total sold
                    var totalSold = productOrders.Sum(oi => oi.Quantity);

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

        public async Task<BaseResponse> GetProductByProviderId(int id)
        {
            var response = new BaseResponse();
            try
            {
                var products = await _unitOfWork.ProductRepository
                                                .Query(p => p.ProviderId == id)
                                                .Include(p => p.ProductImages)
                                                .ToListAsync();

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get productOrder of product
                    var productOrders = await _unitOfWork.ProductOrderRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Completed)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = productOrders
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                    // Calculate total sold
                    var totalSold = productOrders.Sum(oi => oi.Quantity);

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

        public async Task<BaseResponse> CreateProduct(CreateProductRequest request)
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

                if (request.Images != null && request.Images.Count > 5)
                {
                    response.Success = false;
                    response.Message = "Maximum 5 images";
                    return response;
                }

                // CreateProduct
                var product = new Product
                {
                    ProductName = request.ProductName,
                    Description = request.Description,
                    ProductPrice = request.ProductPrice,
                    Quantity = request.Quantity,
                    MadeIn = request.MadeIn,
                    ShipFrom = request.ShipFrom,
                    CategoryId = request.CategoryId,
                    ProviderId = request.ProviderId,
                    CreateAt = DateTime.UtcNow.ToLocalTime(),
                    ProductImages = new List<ProductImage>()
                };

                // Upload images
                if (request.Images != null && request.Images.Any())
                {
                    foreach(var imageFile in request.Images )
                    {
                        using var stream = imageFile.OpenReadStream();
                        var imageUrl = await _cloudinaryService.UploadFileAsync(
                            stream,
                            imageFile.FileName,
                            imageFile.ContentType
                            );
                        product.ProductImages.Add(new ProductImage { ImageUrl = imageUrl });
                    }
                }

                await _unitOfWork.ProductRepository.InsertAsync(product);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product created successfully";
                response.Data = _mapper.Map<CreateProductResponse>(product);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating product";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateProduct(int id, UpdateProductRequest request)
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

                if (request.Images != null && request.Images.Count > 5)
                {
                    response.Success = false;
                    response.Message = "Maximum 5 images";
                    return response;
                }

                product.ProductName = request.ProductName;
                product.Description = request.Description;
                product.ProductPrice = request.ProductPrice;
                product.Quantity = request.Quantity;
                product.MadeIn = request.MadeIn;
                product.ShipFrom = request.ShipFrom;
                product.CategoryId = request.CategoryId;

                if (request.Images != null && request.Images.Any())
                {
                    if (product.ProductImages.Any())
                    {
                        product.ProductImages.Clear();

                        foreach (var imageFile in request.Images)
                        {
                            using var stream = imageFile.OpenReadStream();
                            var imageUrl = await _cloudinaryService.UploadFileAsync(
                                stream,
                                imageFile.FileName,
                                imageFile.ContentType
                                );
                            product.ProductImages.Add(new ProductImage { ImageUrl = imageUrl });
                        }
                    }
                }

                _unitOfWork.ProductRepository.Update(product);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product updated successfully";
                response.Data = _mapper.Map<UpdateProductResponse>(product);
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
