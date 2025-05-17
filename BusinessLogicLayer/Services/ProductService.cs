using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using BusinessLogicLayer.ModelResponse.Product;
using BusinessLogicLayer.ModelResponse.Review;
using CloudinaryDotNet;
using DataAccessObject.Models;
using LinqKit;
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
        private readonly IElasticClientService _elasticClientService;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ICloudinaryService cloudinaryService, IElasticClientService elasticClientService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _elasticClientService = elasticClientService;
        }

        public async Task<BaseResponse> GetAllProduct()
        {
            var response = new BaseResponse();
            try
            {
                Expression<Func<Product, object>>[] includeProperties = { p => p.ProductImages, p => p.Category };
                var products = await _unitOfWork.ProductRepository.GetAllAsync(includeProperties);

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get orderDetail of product
                    var orderDetails = await _unitOfWork.OrderDetailRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Paid)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = orderDetails
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? Math.Round(reviews.Average(r => r.Rate), 1) : 0;

                    // Calculate total sold
                    var totalSold = orderDetails.Sum(oi => oi.Quantity);

                    var productResponse = new ProductListResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Rate = averageRate,
                        Quantity = product.Quantity,
                        ProductPrice = product.ProductPrice,
                        TotalSold = totalSold,
                        Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                        ProductCategory = new ProductCategoryResponse
                        {
                            Id = product.Category.Id,
                            CategoryName = product.Category.CategoryName
                        },
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

        public async Task<BaseResponse<PageResult<ProductListResponse>>> GetPaginate(ProductFilterRequest request)
        {
            var response = new BaseResponse<PageResult<ProductListResponse>>();
            try
            {
                // Filter
                Expression<Func<Product, bool>> filter = product =>
                    (string.IsNullOrEmpty(request.ProductName) || product.ProductName.Contains(request.ProductName)) &&
                    (!request.MinPrice.HasValue || product.ProductPrice >= request.MinPrice.Value) &&
                    (!request.MaxPrice.HasValue || product.ProductPrice <= request.MaxPrice.Value);

                // Check user
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(request.UserId);

                if ((account != null && account.RoleId == 1) || (account != null && account.RoleId == 3 && account.ProviderVerified == true))
                {
                    // Display all products
                }
                else
                {
                    filter = filter.And(p => p.Quantity > 0); // Display in stock products
                }

                // Sort
                Expression<Func<Product, object>> orderByExpression = request.SortBy?.ToLower() switch
                {
                    "productname" => product => product.ProductName,
                    "productprice" => product => product.ProductPrice,
                    "createat" => product => product.CreateAt,
                    _ => product => product.Id
                };

                // Include entities
                Expression<Func<Product, object>>[] includeProperties =
                {
                    p => p.ProductImages,
                    p => p.Category
                };

                // Get paginated data and filter
                (IEnumerable<Product> products, int totalCount) = await _unitOfWork.ProductRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    includeProperties
                );

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get orderDetail of product
                    var orderDetails = await _unitOfWork.OrderDetailRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Paid)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = orderDetails
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? Math.Round(reviews.Average(r => r.Rate), 1) : 0;

                    // Calculate total sold
                    var totalSold = orderDetails.Sum(oi => oi.Quantity);

                    var productResponse = new ProductListResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Rate = averageRate,
                        Quantity = product.Quantity,
                        ProductPrice = product.ProductPrice,
                        TotalSold = totalSold,
                        Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                        ProductCategory = new ProductCategoryResponse
                        {
                            Id = product.Category.Id,
                            CategoryName = product.Category.CategoryName
                        },
                        ImageUrls = product.ProductImages?.FirstOrDefault()?.ImageUrl != null
                            ? new List<string> { product.ProductImages.FirstOrDefault()?.ImageUrl }
                            : new List<string>()
                    };

                    productResponses.Add(productResponse);
                }

                var pageResult = new PageResult<ProductListResponse>
                {
                    Data = productResponses,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Product list retrieved successfully";
                response.Data = pageResult;
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

                // Get orderDetail of product
                var orderDetails = await _unitOfWork.OrderDetailRepository
                                        .Query(po => po.ProductId == product.Id
                                                    && po.Order.Status == Order.OrderStatus.Paid)
                                        .Include(po => po.Order)
                                            .ThenInclude(o => o.Reviews)
                                            .ThenInclude(r => r.ReviewImages)
                                        .ToListAsync();

                // Get review of product
                var reviews = orderDetails
                                .SelectMany(po => po.Order.Reviews)
                                .ToList();

                // Calculate average rate
                var averageRate = reviews.Any() ? Math.Round(reviews.Average(r => r.Rate), 1) : 0;

                // Calculate total rate
                var totalRate = reviews.Sum(r => r.Rate);

                // Calculate total sold
                var totalSold = orderDetails.Sum(oi => oi.Quantity);

                // Mapping reviews to response
                var reviewResponses = reviews.Select(r => new ReviewResponse
                {
                    Rate = r.Rate,
                    Comment = r.Comment,
                    CreateAt = r.CreateAt,
                    Images = r.ReviewImages?.FirstOrDefault()?.ImageUrl != null
                            ? new List<string> { r.ReviewImages.FirstOrDefault()?.ImageUrl }
                            : new List<string>()
                }).ToList(); ;

                // Get follower
                var followers = await _unitOfWork.FollowRepository
                                    .Query(f => f.FollowerId == product.AccountId)
                                    .CountAsync();

                // Get following
                var followings = await _unitOfWork.FollowRepository
                                    .Query(f => f.FollowingId == product.AccountId)
                                    .CountAsync();

                // Calculate total product of provider
                var totalProduct = await _unitOfWork.ProductRepository
                                        .Query(p => p.AccountId == product.AccountId)
                                        .CountAsync();

                // Get provider of product
                var provider = await _unitOfWork.AccountRepository
                                    .Query(a => a.Id == product.AccountId)
                                    .FirstOrDefaultAsync();

                // Mapping provider to response
                var providerResponse = new ProductProviderResponse
                {
                    Id = provider.Id,
                    Slug = provider.Slug,
                    BusinessName = provider.BusinessName,
                    Avatar = provider.Avatar,
                    TotalRate = totalRate,
                    FollowersCount = followers,
                    FollowingsCount = followings,
                    TotalProduct = totalProduct
                };

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
                    Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                    MadeIn = product.MadeIn,
                    ShipFrom = product.ShipFrom,
                    CategoryId = product.CategoryId,
                    ImageUrls = product.ProductImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
                    Provider = providerResponse,
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
                                                .Include(p => p.Category)
                                                .ToListAsync();

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get orderDetail of product
                    var orderDetails = await _unitOfWork.OrderDetailRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Paid)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                                .ThenInclude(r => r.ReviewImages)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = orderDetails
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? Math.Round(reviews.Average(r => r.Rate), 1) : 0;

                    // Calculate total sold
                    var totalSold = orderDetails.Sum(oi => oi.Quantity);

                    var productResponse = new ProductListResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Rate = averageRate,
                        Quantity = product.Quantity,
                        ProductPrice = product.ProductPrice,
                        TotalSold = totalSold,
                        Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                        ProductCategory = new ProductCategoryResponse
                        {
                            Id = product.Category.Id,
                            CategoryName = product.Category.CategoryName
                        },
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

        public async Task<BaseResponse<PageResult<ProductListResponse>>> GetPaginateByCategory(FilterByCategoryRequest request)
        {
            var response = new BaseResponse<PageResult<ProductListResponse>>();
            try
            {
                var productCategory = await _unitOfWork.ProductCategoryRepository
                                                        .Query(p => p.Id == request.CategoryId)
                                                        .FirstOrDefaultAsync();

                if (productCategory == null)
                {
                    response.Message = "Category not found";
                    return response;
                }

                // Filter
                Expression<Func<Product, bool>> filter = product =>
                    product.CategoryId == request.CategoryId &&
                    (string.IsNullOrEmpty(request.ProductName) || product.ProductName.Contains(request.ProductName)) &&
                    (!request.MinPrice.HasValue || product.ProductPrice >= request.MinPrice.Value) &&
                    (!request.MaxPrice.HasValue || product.ProductPrice <= request.MaxPrice.Value);

                // Check user
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(request.UserId);

                if ((account != null && account.RoleId == 1) || (account != null && account.RoleId == 3 && account.ProviderVerified == true))
                {
                    // Display all products
                }
                else
                {
                    filter = filter.And(p => p.Quantity > 0); // Display in stock products
                }

                // Sort
                Expression<Func<Product, object>> orderByExpression = request.SortBy?.ToLower() switch
                {
                    "productname" => product => product.ProductName,
                    "productprice" => product => product.ProductPrice,
                    "createat" => product => product.CreateAt,
                    _ => product => product.Id
                };

                // Include Entities
                Expression<Func<Product, object>>[] includeProperties =
                {
                    p => p.ProductImages,
                    p => p.Category
                };

                // Get paginated data and filter
                (IEnumerable<Product> products, int totalCount) = await _unitOfWork.ProductRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    includeProperties
                );

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get orderDetail of product
                    var orderDetails = await _unitOfWork.OrderDetailRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Paid)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                                .ThenInclude(r => r.ReviewImages)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = orderDetails
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? Math.Round(reviews.Average(r => r.Rate), 1) : 0;

                    // Calculate total sold
                    var totalSold = orderDetails.Sum(oi => oi.Quantity);

                    var productResponse = new ProductListResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Rate = averageRate,
                        Quantity = product.Quantity,
                        ProductPrice = product.ProductPrice,
                        TotalSold = totalSold,
                        Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                        ProductCategory = new ProductCategoryResponse
                        {
                            Id = product.Category.Id,
                            CategoryName = product.Category.CategoryName
                        },
                        ImageUrls = product.ProductImages?.FirstOrDefault()?.ImageUrl != null
                            ? new List<string> { product.ProductImages.FirstOrDefault()?.ImageUrl }
                            : new List<string>()
                    };

                    productResponses.Add(productResponse);
                }

                var pageResult = new PageResult<ProductListResponse>
                {
                    Data = productResponses,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Product list retrieved successfully";
                response.Data = pageResult;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving product list";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> GetProductByProvider(string slug)
        {
            var response = new BaseResponse();
            try
            {
                var account = await _unitOfWork.AccountRepository
                                               .Query(a => a.Slug == slug && a.ProviderVerified == true)
                                               .FirstOrDefaultAsync();

                if (account == null || account.IsProvider == false)
                {
                    response.Message = "Provider not found";
                    return response;
                }

                var accountId = account.Id;

                var products = await _unitOfWork.ProductRepository
                                                .Query(p => p.AccountId == accountId)
                                                .Include(p => p.ProductImages)
                                                .Include(p => p.Category)
                                                .ToListAsync();

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get orderDetail of product
                    var orderDetails = await _unitOfWork.OrderDetailRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Paid)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                                .ThenInclude(r => r.ReviewImages)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = orderDetails
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? Math.Round(reviews.Average(r => r.Rate), 1) : 0;

                    // Calculate total sold
                    var totalSold = orderDetails.Sum(oi => oi.Quantity);

                    var productResponse = new ProductListResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Rate = averageRate,
                        Quantity = product.Quantity,
                        ProductPrice = product.ProductPrice,
                        TotalSold = totalSold,
                        Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                        ProductCategory = new ProductCategoryResponse
                        {
                            Id = product.Category.Id,
                            CategoryName = product.Category.CategoryName
                        },
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

        public async Task<BaseResponse<PageResult<ProductListResponse>>> GetPaginateByProvider(FilterByProviderRequest request)
        {
            var response = new BaseResponse<PageResult<ProductListResponse>>();
            try
            {
                var providerAccount = await _unitOfWork.AccountRepository
                                  .Query(a => a.Slug == request.Slug && a.ProviderVerified == true)
                                  .FirstOrDefaultAsync();

                if (providerAccount == null)
                {
                    response.Message = "Invalid provider slug";
                    return response;
                }

                // Filter
                Expression<Func<Product, bool>> filter = product =>
                    (product.Account.Slug == request.Slug && product.Account.ProviderVerified == true) &&
                    (string.IsNullOrEmpty(request.ProductName) || product.ProductName.Contains(request.ProductName)) &&
                    (!request.MinPrice.HasValue || product.ProductPrice >= request.MinPrice.Value) &&
                    (!request.MaxPrice.HasValue || product.ProductPrice <= request.MaxPrice.Value);

                // Check user
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(request.UserId);

                if ((account != null && account.RoleId == 1) || (account != null && account.RoleId == 3 && account.ProviderVerified == true))
                {
                    // Display all products
                }
                else
                {
                    filter = filter.And(p => p.Quantity > 0); // Display in stock products
                }

                // Sort
                Expression<Func<Product, object>> orderByExpression = request.SortBy?.ToLower() switch
                {
                    "productname" => product => product.ProductName,
                    "productprice" => product => product.ProductPrice,
                    "createat" => product => product.CreateAt,
                    _ => product => product.Id
                };

                // Include Entities
                Expression<Func<Product, object>>[] includeProperties =
                {
                    product => product.ProductImages,
                    product => product.Category
                };

                // Get paginated data and filter
                (IEnumerable<Product> products, int totalCount) = await _unitOfWork.ProductRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    includeProperties
                );

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get orderDetail of product
                    var orderDetails = await _unitOfWork.OrderDetailRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Paid)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                                .ThenInclude(r => r.ReviewImages)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = orderDetails
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? Math.Round(reviews.Average(r => r.Rate), 1) : 0;

                    // Calculate total sold
                    var totalSold = orderDetails.Sum(oi => oi.Quantity);

                    var productResponse = new ProductListResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Rate = averageRate,
                        Quantity = product.Quantity,
                        ProductPrice = product.ProductPrice,
                        TotalSold = totalSold,
                        Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                        ProductCategory = new ProductCategoryResponse
                        {
                            Id = product.Category.Id,
                            CategoryName = product.Category.CategoryName
                        },
                        ImageUrls = product.ProductImages?.FirstOrDefault()?.ImageUrl != null
                            ? new List<string> { product.ProductImages.FirstOrDefault()?.ImageUrl }
                            : new List<string>()
                    };

                    productResponses.Add(productResponse);
                }

                var pageResult = new PageResult<ProductListResponse>
                {
                    Data = productResponses,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Product list retrieved successfully";
                response.Data = pageResult;
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
                    response.Message = "Invalid product request!";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.ProductName))
                {
                    response.Message = "Product name is required!";
                    return response;
                }

                if (request.ProductPrice <= 0)
                {
                    response.Message = "Product price has to be greater than 0!";
                    return response;
                }

                if (request.Quantity < 0)
                {
                    response.Message = "Product quantity cannot be less than 0!";
                    return response;
                }

                if (request.Images != null && request.Images.Count > 5)
                {
                    response.Message = "Maximum 5 images!";
                    return response;
                }

                // CreateProduct
                var product = new Product
                {
                    ProductName = request.ProductName,
                    Description = request.Description,
                    ProductPrice = request.ProductPrice,
                    Quantity = request.Quantity,
                    Status = request.Quantity == 0
                        ? Product.ProductStatus.OutOfStock
                        : Product.ProductStatus.InStock,
                    MadeIn = request.MadeIn,
                    ShipFrom = request.ShipFrom,
                    CategoryId = request.CategoryId,
                    AccountId = request.AccountId,
                    CreateAt = DateTime.UtcNow.ToLocalTime(),
                    ProductImages = new List<ProductImage>(),
                    ProductSeasons = new List<ProductSeason>()
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

                // Add seasons
                if (request.SeasonIds != null && request.SeasonIds.Any())
                {
                    foreach (var seasonId in request.SeasonIds)
                    {
                        product.ProductSeasons.Add(new ProductSeason
                        {
                            SeasonId = seasonId
                        });
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
                response.Message = "Error creating product!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateProduct(int id, UpdateProductRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var product = await _unitOfWork.ProductRepository
                                        .Query(p => p.Id == id)
                                        .Include(p => p.ProductImages)
                                        .Include(p => p.ProductSeasons)
                                        .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.Message = "Invalid product!";
                    return response;
                }

                if (request == null)
                {
                    response.Message = "Invalid product detail!";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.ProductName))
                {
                    response.Message = "Product name is required!";
                    return response;
                }

                if (request.ProductPrice <= 0)
                {
                    response.Message = "Product price has to be greater than 0!";
                    return response;
                }

                if (request.Quantity < 0)
                {
                    response.Message = "Quantity cannot not be negative!";
                    return response;
                }

                if (request.Images != null && request.Images.Count > 5)
                {
                    response.Message = "Maximum 5 images!";
                    return response;
                }

                product.ProductName = request.ProductName;
                product.Description = request.Description;
                product.ProductPrice = request.ProductPrice;
                product.Quantity = request.Quantity;
                product.MadeIn = request.MadeIn;
                product.ShipFrom = request.ShipFrom;
                product.CategoryId = request.CategoryId;

                if (request.SeasonIds != null)
                {
                    product.ProductSeasons.Clear();

                    foreach (var seasonId in request.SeasonIds)
                    {
                        product.ProductSeasons.Add(new ProductSeason
                        {
                            SeasonId = seasonId
                        });
                    }
                }

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
                response.Message = "Error updating product!";
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
                                        .Include(p => p.ProductSeasons)
                                        .Include(p => p.ProductImages)
                                        .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.Message = "Invalid product!";
                    return response;
                }

                // Delete ProductSeasons
                if (product.ProductSeasons != null && product.ProductSeasons.Any())
                {
                    _unitOfWork.ProductSeasonRepository.RemoveRange(product.ProductSeasons);
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

        public async Task<BaseResponse> SearchMultiCriteriaProduct(SearchProductRequest request)
        {
            var response = new BaseResponse();
            try
            {
                Expression<Func<Product, object>>[] includeProperties =
                {
                    p => p.Category,
                    p => p.ProductImages
                };
                var products = await _unitOfWork.ProductRepository.GetAllAsync(includeProperties);

                if (!string.IsNullOrEmpty(request.ProductName))
                    products = products.Where(p => p.ProductName.Contains(request.ProductName));

                if (!string.IsNullOrEmpty(request.CategoryName))
                    products = products.Where(p => p.Category.CategoryName.Contains(request.CategoryName));
                
                if (!string.IsNullOrEmpty(request.ShipFrom))
                    products = products.Where(p => p.ShipFrom != null && p.ShipFrom.Contains(request.ShipFrom));
                
                if (!string.IsNullOrEmpty(request.MadeIn))
                    products = products.Where(p => p.MadeIn != null && p.MadeIn.Contains(request.MadeIn));

                var productResponses = new List<ProductListResponse>();

                foreach (var product in products)
                {
                    // Get orderDetail of product
                    var orderDetails = await _unitOfWork.OrderDetailRepository
                                            .Query(po => po.ProductId == product.Id
                                                        && po.Order.Status == Order.OrderStatus.Paid)
                                            .Include(po => po.Order)
                                                .ThenInclude(o => o.Reviews)
                                            .ToListAsync();

                    // Get review of product
                    var reviews = orderDetails
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? reviews.Average(r => r.Rate) : 0;

                    // Calculate total sold
                    var totalSold = orderDetails.Sum(oi => oi.Quantity);

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
                response.Message = "Error performing multi-criteria search";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<ProductListResponse>> SearchProduct(string keyword)
        {
            var response = new BaseResponse<ProductListResponse>();
            try
            {
                var result = await _elasticClientService.SearchProductAsync(keyword);

                var productResponse = _mapper.Map<ProductListResponse>(result);

                response.Success = true;
                response.Message = "Search completed successfully";
                response.Data = productResponse;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error searching products";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
