using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Http;
using BusinessLogicLayer.ModelResponse.Pagination;
using System.Linq.Expressions;
using BusinessLogicLayer.ModelResponse.Product;
using Org.BouncyCastle.Asn1.Ocsp;
using BusinessLogicLayer.Utilities.DataMapping;
using AutoMapper;
using BusinessLogicLayer.ModelRequest.Pagination;
using LinqKit;
using Microsoft.Extensions.Configuration;
using static DataAccessObject.Models.Booking;
using static DataAccessObject.Models.Quotation;

namespace BusinessLogicLayer.Services
{
    public class QuotationService : IQuotationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly string _clientBaseUrl;
        private readonly IWalletService _walletService;

        public QuotationService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IMapper mapper, INotificationService notificationService, IConfiguration configuration, IWalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
            _notificationService = notificationService;
            _clientBaseUrl = configuration["AppSettings:ClientBaseUrl"];
            _walletService = walletService;
        }

        /// <summary>
        /// Tạo báo giá cho một booking
        /// </summary>
        public async Task<BaseResponse> CreateQuotationAsync(string bookingCode, CreateQuotationRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                if (booking.Status != Booking.BookingStatus.Quoting)
                {
                    response.Message = "Quotation can only be created during the Quoting phase.";
                    return response;
                }

                // Kiểm tra nếu đã có báo giá và không phải bị từ chối thì không cho tạo
                var existingQuotation = await _unitOfWork.QuotationRepository.Queryable()
                    .FirstOrDefaultAsync(q => q.BookingId == booking.Id && q.Status != Quotation.QuotationStatus.Closed);

                if (existingQuotation != null)
                {
                    response.Message = existingQuotation.Status == Quotation.QuotationStatus.Confirmed
                        ? "Quotation already confirmed. Cannot create a new one."
                        : "Quotation already exists for this booking.";
                    return response;
                }

                // Validate labor task area
                foreach (var task in request.ConstructionTasks)
                {
                    if (task.Area == null || task.Area <= 0)
                    {
                        response.Message = $"Invalid area for construction task \"{task.TaskName}\". Area must be greater than 0.";
                        return response;
                    }

                    if (task.Cost <= 0)
                    {
                        response.Message = $"Invalid cost for construction task \"{task.TaskName}\". Cost must be greater than 0.";
                        return response;
                    }
                }

                // Validate materials
                foreach (var material in request.Materials)
                {
                    if (material.Quantity <= 0)
                    {
                        response.Message = $"Invalid quantity for material \"{material.MaterialName}\". Quantity must be greater than 0.";
                        return response;
                    }

                    if (material.Cost < 0)
                    {
                        response.Message = $"Invalid cost for material \"{material.MaterialName}\". Cost cannot be negative.";
                        return response;
                    }
                }

                // Tạo mã báo giá mới
                var quotationCode = GenerateQuotationCode();

                // Tính toán chi phí
                decimal totalMaterialCost = request.Materials.Sum(m => m.Cost * m.Quantity);
                decimal totalLaborCost = request.ConstructionTasks.Sum(c => c.Cost * (c.Area ?? 0m));
                decimal? totalProductCost = null;

                var depositPercentage = Math.Min(request.DepositPercentage, 20m);

                List<ProductDetail> productDetails = new();
                //if (request.Products != null && request.Products.Any())
                //{
                //    // Lấy thông tin sản phẩm từ cơ sở dữ liệu
                //    foreach (var p in request.Products)
                //    {
                //        var product = await _unitOfWork.ProductRepository.Queryable()
                //            .FirstOrDefaultAsync(prod => prod.Id == p.ProductId);

                //        if (product != null)
                //        {
                //            productDetails.Add(new ProductDetail
                //            {
                //                ProductId = p.ProductId,
                //                ProductName = product.ProductName,
                //                Quantity = p.Quantity,
                //                UnitPrice = product.ProductPrice,
                //                TotalPrice = p.Quantity * product.ProductPrice
                //            });
                //        }
                //    }

                //    totalProductCost = productDetails.Sum(p => p.Quantity * p.UnitPrice);
                //}

                if (booking.RelatedProductId.HasValue)
                {
                    var relatedProduct = await _unitOfWork.RelatedProductRepository.Queryable()
                        .Where(rp => rp.Id == booking.RelatedProductId.Value)
                        .FirstOrDefaultAsync();

                    var relatedItems = await _unitOfWork.RelatedProductItemRepository.Queryable()
                        .Where(item => item.RelatedProductId == relatedProduct.Id)
                        .ToListAsync();

                    var productIds = relatedItems.Select(i => i.ProductId).Distinct().ToList();
                    var products = await _unitOfWork.ProductRepository.Queryable()
                        .Include(p => p.ProductImages)
                        .Where(p => productIds.Contains(p.Id))
                        .ToListAsync();

                    foreach (var item in relatedItems)
                    {
                        var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            productDetails.Add(new ProductDetail
                            {
                                ProductId = product.Id,
                                ProductName = product.ProductName,
                                Quantity = item.Quantity,
                                UnitPrice = product.ProductPrice,
                                TotalPrice = item.Quantity * product.ProductPrice
                            });
                        }
                    }

                    totalProductCost = productDetails.Sum(p => p.TotalPrice);

                    // Remove RelatedProductId
                    booking.RelatedProductId = null;

                    // Remove RelatedProductItem
                    _unitOfWork.RelatedProductItemRepository.RemoveRange(relatedItems);

                    // Remove RelatedProduct
                    _unitOfWork.RelatedProductRepository.Delete(relatedProduct.Id);
                }

                // Tạo báo giá mới hoàn toàn
                var quotation = new Quotation
                {
                    BookingId = booking.Id,
                    QuotationCode = quotationCode,
                    MaterialCost = totalMaterialCost,
                    ConstructionCost = totalLaborCost,
                    DepositPercentage = depositPercentage,
                    CreatedAt = DateTime.Now,
                    Status = Quotation.QuotationStatus.Pending
                };

                if (totalProductCost.HasValue)
                {
                    quotation.ProductCost = totalProductCost.Value;
                }

                await _unitOfWork.QuotationRepository.InsertAsync(quotation);
                await _unitOfWork.CommitAsync();

                // Gán QuotationId cho ProductDetail
                foreach (var product in productDetails)
                {
                    product.QuotationId = quotation.Id;
                }
                await _unitOfWork.ProductDetailRepository.InsertRangeAsync(productDetails);

                // Thêm chi tiết vật liệu
                var materialDetails = request.Materials.Select(m => new MaterialDetail
                {
                    QuotationId = quotation.Id,
                    MaterialName = m.MaterialName,
                    Quantity = m.Quantity,                   
                    Cost = m.Cost,
                    Note = m.Note
                }).ToList();

                await _unitOfWork.MaterialDetailRepository.InsertRangeAsync(materialDetails);

                // Thêm chi tiết công trình
                var constructionDetails = request.ConstructionTasks.Select(c => new LaborDetail
                {
                    QuotationId = quotation.Id,
                    TaskName = c.TaskName,
                    Cost = c.Cost,
                    Unit = c.Unit,
                    Area = c.Area,
                    Note = c.Note
                }).ToList();

                await _unitOfWork.LaborDetailRepository.InsertRangeAsync(constructionDetails);
                booking.IsQuoted = true;
                quotation.isQuoteExisted = true;
                await _unitOfWork.CommitAsync();

                // ========================
                // ✅ Thêm thông báo cho khách hàng
                // ========================

                string quotationUrl = $"{_clientBaseUrl}/quotation/{quotation.QuotationCode}"; // URL chi tiết báo giá
                string htmlQuotationCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{quotation.QuotationCode}</span>";

                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = booking.AccountId, // ID khách hàng từ booking
                    Title = "New Quotation",
                    Content = $"A quotation {htmlQuotationCode} has been generated for your order.",
                    Url = quotationUrl
                });

                response.Success = true;
                response.Message = "Quotation created successfully.";
                response.Data = new
                {
                    Quotation = quotation,
                    MaterialDetails = materialDetails,
                    ConstructionDetails = constructionDetails,
                    ProductDetails = productDetails.Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Quantity,
                        p.UnitPrice,
                        p.TotalPrice
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to create quotation.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> UploadQuotationFileAsync(string bookingCode, IFormFile quotationFile)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .FirstOrDefaultAsync(q => q.BookingId == booking.Id);

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                if (quotationFile == null || quotationFile.Length == 0)
                {
                    response.Message = "Invalid file.";
                    return response;
                }

                using var stream = quotationFile.OpenReadStream();
                var filePath = await _cloudinaryService.UploadFileAsync(
                    stream,
                    quotationFile.FileName,
                    quotationFile.ContentType
                );

                quotation.QuotationFilePath = filePath;
                _unitOfWork.QuotationRepository.Update(quotation);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Quotation file uploaded successfully.";
                response.Data = new { FilePath = filePath };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to upload quotation file.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        ///// <summary>
        ///// Lấy báo giá theo BookingId
        ///// </summary>
        //public async Task<BaseResponse>
        //QuotationByBookingCodeAsync(string bookingCode)
        //{
        //    var response = new BaseResponse();
        //    try
        //    {
        //        var booking = await _unitOfWork.BookingRepository.Queryable()
        //            .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

        //        if (booking == null)
        //        {
        //            response.Message = "Booking not found.";
        //            return response;
        //        }

        //        var quotation = await _unitOfWork.QuotationRepository.Queryable()
        //            .FirstOrDefaultAsync(q => q.BookingId == booking.Id);

        //        if (quotation == null)
        //        {
        //            response.Message = "Quotation not found.";
        //            return response;
        //        }

        //        var materialDetails = await _unitOfWork.MaterialDetailRepository.Queryable()
        //            .Where(m => m.QuotationId == quotation.Id)
        //            .ToListAsync();

        //        var constructionDetails = await _unitOfWork.ConstructionDetailRepository.Queryable()
        //            .Where(c => c.QuotationId == quotation.Id)
        //            .ToListAsync();

        //        response.Success = true;
        //        response.Message = "Quotation retrieved successfully.";
        //        response.Data = new
        //        {
        //            Quotation = quotation,
        //            MaterialDetails = materialDetails,
        //            ConstructionDetails = constructionDetails
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Failed to retrieve quotation.";
        //        response.Errors.Add(ex.Message);
        //    }
        //    return response;
        //}

        public async Task<BaseResponse> AddProductToQuotationAsync(string quotationCode, int productId, int quantity)
        {
            var response = new BaseResponse();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                                                .Include(q => q.ProductDetails)
                                                .Where(q => q.QuotationCode == quotationCode &&
                                                                    q.Status == Quotation.QuotationStatus.Pending)
                                                .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found or already processed!";
                    return response;
                }

                var product = await _unitOfWork.ProductRepository.Queryable()
                                                .Include(p => p.ProductImages)
                                                .Where(p => p.Id == productId)
                                                .FirstOrDefaultAsync();

                if (product == null)
                {
                    response.Message = "Product not found in quotation!";
                    return response;
                }

                // Check existing product quantity
                if (product.Quantity < quantity)
                {
                    response.Message = "Not enough existing product!";
                    return response;
                }

                if (product.Quantity < 0)
                {
                    response.Message = "Product quantity has to be > 0";
                    return response;
                }

                var productDetail = quotation.ProductDetails
                                            .Where(pd => pd.ProductId == productId)
                                            .FirstOrDefault();

                decimal unitPrice = product.ProductPrice;

                // Add product to cart
                if (productDetail == null)
                {
                    productDetail = new ProductDetail
                    {
                        QuotationId = quotation.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = quantity * unitPrice,
                        ProductName = product.ProductName,
                        Image = product.ProductImages?.FirstOrDefault()?.ImageUrl
                    };

                    await _unitOfWork.ProductDetailRepository.InsertAsync(productDetail);
                }
                else
                {
                    if (product.Quantity < productDetail.Quantity + quantity)
                    {
                        response.Message = "Not enough existing product!";
                        return response;
                    }

                    // Update productDetail
                    productDetail.Quantity += quantity;
                    productDetail.TotalPrice = productDetail.Quantity * unitPrice;
                    _unitOfWork.ProductDetailRepository.Update(productDetail);
                }

                // Update quotation
                quotation.ProductCost = quotation.ProductDetails.Sum(pd => pd.TotalPrice);
                _unitOfWork.QuotationRepository.Update(quotation);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product added to quotation successfully.";
                response.Data = new
                {
                    quotation.QuotationCode,
                    Status = quotation.Status.ToString(),
                    quotation.ProductCost,
                    ProductDetails = quotation.ProductDetails.Select(pd => new
                    {
                        pd.Id,
                        pd.ProductId,
                        pd.ProductName,
                        pd.Quantity,
                        pd.UnitPrice,
                        pd.TotalPrice,
                        pd.Image
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error adding product to quotation!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> RemoveProductFromQuotationAsync(string quotationCode, int productId)
        {
            var response = new BaseResponse();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository
                                    .Queryable()
                                    .Include(q => q.ProductDetails)
                                    .Where(q => q.QuotationCode == quotationCode &&
                                                              q.Status == Quotation.QuotationStatus.Pending)
                                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found or already processed!";
                    return response;
                }

                var productDetail = quotation.ProductDetails.FirstOrDefault(pd => pd.ProductId == productId);

                if (productDetail == null)
                {
                    response.Message = "Product not found in quotation!";
                    return response;
                }

                // Cập nhật ProductCost
                quotation.ProductCost -= productDetail.TotalPrice;

                // Xóa ProductDetail
                _unitOfWork.ProductDetailRepository.Delete(productDetail.Id);

                // Update lại Quotation
                _unitOfWork.QuotationRepository.Update(quotation);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Product removed from quotation successfully.";
                response.Data = new
                {
                    quotation.QuotationCode,
                    quotation.ProductCost,
                    RemovedProduct = new
                    {
                        productDetail.ProductId,
                        productDetail.ProductName
                    }
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error removing product from quotation!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
        public async Task<BaseResponse> ConfirmQuotationAsync(string quotationCode, bool isConfirmed)
        {
            var response = new BaseResponse();
            try
            {
                // Tìm quotation theo mã
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.DecorService)
                    .Where(q => q.QuotationCode == quotationCode && q.Status == Quotation.QuotationStatus.Pending)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found or already processed.";
                    return response;
                }

                var booking = quotation.Booking;

                if (booking == null)
                {
                    response.Message = "Associated booking not found.";
                    return response;
                }

                // ✅ Cập nhật tổng chi phí booking

                if (quotation.ProductCost.HasValue)
                {
                    booking.TotalPrice = quotation.MaterialCost + quotation.ConstructionCost + quotation.ProductCost.Value - booking.CommitDepositAmount;
                }
                else
                {
                    booking.TotalPrice = quotation.MaterialCost + quotation.ConstructionCost - booking.CommitDepositAmount;
                }

                if (isConfirmed)
                {
                    // Check nếu BookingDetail đã tồn tại
                    var existingDetails = await _unitOfWork.BookingDetailRepository.Queryable()
                        .Where(bd => bd.BookingId == booking.Id)
                        .ToListAsync();

                    if (existingDetails.Any())
                    {
                        response.Message = "Booking details already exist.";
                        response.Success = true;
                        return response;
                    }

                    decimal totalCost = 0m;
                    if (quotation.ProductCost.HasValue)
                    {
                        totalCost = quotation.MaterialCost + quotation.ConstructionCost + quotation.ProductCost.Value -booking.CommitDepositAmount;
                    }
                    else
                    {
                        totalCost = quotation.MaterialCost + quotation.ConstructionCost -booking.CommitDepositAmount;
                    }

                    decimal depositAmount = (quotation.DepositPercentage / 100) * totalCost;

                    // Tạo BookingDetail mới
                    var bookingDetails = new List<BookingDetail>
                    {
                        new BookingDetail 
                        { 
                            BookingId = booking.Id, 
                            ServiceItem = "Materials Cost", 
                            Cost = quotation.MaterialCost 
                        },
                        
                        new BookingDetail 
                        { 
                            BookingId = booking.Id, 
                            ServiceItem = "Labor Cost", 
                            Cost = quotation.ConstructionCost 
                        }
                    };

                    if (quotation.ProductCost.HasValue)
                    {
                        bookingDetails.Add(new BookingDetail
                        {
                            BookingId = booking.Id,
                            ServiceItem = "Product Cost",
                            Cost = quotation.ProductCost.Value
                        });
                    }

                    bookingDetails.Add(new BookingDetail
                    {
                        BookingId = booking.Id,
                        ServiceItem = "Deposit (" + quotation.DepositPercentage + "%)",
                        Cost = depositAmount
                    });

                    bookingDetails.Add(new BookingDetail
                    {
                        BookingId = booking.Id,
                        ServiceItem = "Total Cost",
                        Cost = totalCost
                    });

                    await _unitOfWork.BookingDetailRepository.InsertRangeAsync(bookingDetails);

                    quotation.Status = Quotation.QuotationStatus.Confirmed;
                    booking.Status = Booking.BookingStatus.Contracting;

                    response.Message = "Quotation confirmed and booking details created.";
                }
                else
                {
                    // Từ chối báo giá
                    quotation.Status = Quotation.QuotationStatus.Closed;
                    quotation.isQuoteExisted = false;

                    response.Message = "Quotation has been denied. You can now create a new quotation.";
                }

                await _unitOfWork.CommitAsync();

                string colorbookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{booking.BookingCode}</span>";
                // Gửi thông báo cho provider
                if (booking?.DecorService?.AccountId != null)
                {
                    var providerId = booking.DecorService.AccountId;
                    var customerName = booking.Account?.LastName + " " + booking.Account?.FirstName;

                    var title = isConfirmed ? "Quotation Confirmed" : "Quotation Denied";
                    var content = isConfirmed
                        ? $"Customer {customerName} has confirmed your quotation for booking #{colorbookingCode}."
                        : $"Customer {customerName} has denied your quotation for booking #{colorbookingCode}. Please create again";

                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = providerId,
                        Title = title,
                        Content = content,
                        Url = "" // cập nhật URL frontend thực tế nếu có
                    });
                }


                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = isConfirmed ? "Failed to confirm quotation." : "Failed to deny quotation.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<PageResult<QuotationResponseForCustomer>>> GetPaginatedQuotationsForCustomerAsync(QuotationFilterRequest request, int accountId)
        {
            var response = new BaseResponse<PageResult<QuotationResponseForCustomer>>();
            try
            {
                // Filter condition
                Expression<Func<Quotation, bool>> filter = q =>
                    q.Booking.AccountId == accountId &&
                    (string.IsNullOrEmpty(request.QuotationCode) || q.QuotationCode.Contains(request.QuotationCode)) &&
                    (!request.Status.HasValue || q.Status == request.Status.Value);

                // Sorting
                Expression<Func<Quotation, object>> orderByExpression = request.SortBy switch
                {
                    "QuotationCode" => q => q.QuotationCode,
                    "Status" => q => q.Status,
                    "TotalCost" => q => (q.MaterialCost + q.ConstructionCost),
                    _ => q => q.CreatedAt
                };

                // Includes
                Func<IQueryable<Quotation>, IQueryable<Quotation>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.Address)
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.DecorService)
                            .ThenInclude(ds => ds.Account)
                    .Include(q => q.MaterialDetails)
                    .Include(q => q.LaborDetails)
                    .Include(q => q.ProductDetails)
                        .ThenInclude(pd => pd.Product)
                    .Include(q => q.Contract);

                // Get paginated data
                (IEnumerable<Quotation> quotations, int totalCount) =
                    await _unitOfWork.QuotationRepository.GetPagedAndFilteredAsync(
                        filter,
                        request.PageIndex,
                        request.PageSize,
                        orderByExpression,
                        request.Descending,
                        null,
                        customQuery);

                // Map to DTO
                var quotationResponses = quotations.Select(q => new QuotationResponseForCustomer
                {
                    Id = q.Id,
                    QuotationCode = q.QuotationCode,
                    Style = q.Booking.DecorService.Style,
                    MaterialCost = q.MaterialCost,
                    ConstructionCost = q.ConstructionCost,
                    ProductCost = q.ProductCost,
                    DepositPercentage = q.DepositPercentage,
                    CreatedAt = q.CreatedAt,
                    FilePath = q.QuotationFilePath,
                    Status = (int)q.Status,
                    IsQuoteExisted = q.isQuoteExisted,
                    IsContractExisted = q.Contract != null && q.Contract.isContractExisted,
                    IsSigned = q.Contract != null && q.Contract.isSigned == true,

                    MaterialDetails = q.MaterialDetails.Select(m => new MaterialDetailResponse
                    {
                        MaterialName = m.MaterialName,
                        Quantity = m.Quantity,
                        Cost = m.Cost,
                        //Category = m.Category
                        Note = m.Note
                    }).ToList(),

                    ConstructionDetails = q.LaborDetails.Select(c => new ConstructionDetailResponse
                    {
                        TaskName = c.TaskName,
                        Cost = c.Cost,
                        Unit = c.Unit,
                        Area = c.Area,
                        Note = c.Note
                    }).ToList(),

                    ProductDetails = q.ProductDetails.Select(p => new ProductsDetailResponse
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Quantity = p.Quantity,
                        UnitPrice = p.UnitPrice,
                        TotalPrice = p.TotalPrice,
                        Image = p.Image
                    }).ToList(),

                    Provider = new ProviderResponse
                    {
                        BusinessName = q.Booking.DecorService.Account.BusinessName,
                        Avatar = q.Booking.DecorService.Account.Avatar,
                    },
                }).ToList();

                response.Success = true;
                response.Data = new PageResult<QuotationResponseForCustomer>
                {
                    Data = quotationResponses,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving quotations";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<QuotationResponseForProvider>>> GetPaginatedQuotationsForProviderAsync(QuotationFilterRequest request, int providerId)
        {
            var response = new BaseResponse<PageResult<QuotationResponseForProvider>>();
            try
            {
                // Filter condition
                Expression<Func<Quotation, bool>> filter = q =>
                    q.Booking.DecorService.AccountId == providerId &&
                    (string.IsNullOrEmpty(request.QuotationCode) || q.QuotationCode.Contains(request.QuotationCode)) &&
                    (!request.Status.HasValue || q.Status == request.Status.Value);

                // Sorting
                Expression<Func<Quotation, object>> orderByExpression = request.SortBy switch
                {
                    "QuotationCode" => q => q.QuotationCode,
                    "Status" => q => q.Status,
                    "TotalCost" => q => (q.MaterialCost + q.ConstructionCost),
                    _ => q => q.CreatedAt
                };

                // Includes (additional customer info for provider view)
                Func<IQueryable<Quotation>, IQueryable<Quotation>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.Address)
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.Account)
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.DecorService)
                    .Include(q => q.MaterialDetails)
                    .Include(q => q.LaborDetails)
                    .Include(q => q.ProductDetails)
                        .ThenInclude(pd => pd.Product)
                    .Include(q => q.Contract);

                // Same pagination logic as customer method
                (IEnumerable<Quotation> quotations, int totalCount) =
                    await _unitOfWork.QuotationRepository.GetPagedAndFilteredAsync(
                        filter,
                        request.PageIndex,
                        request.PageSize,
                        q => q.CreatedAt, // Default sorting
                        request.Descending,
                        null,
                        customQuery);

                // Map to DTO with additional customer info
                var quotationResponses = quotations.Select(q => new QuotationResponseForProvider
                {
                    // All properties from QuotationResponse
                    Id = q.Id,
                    Style = q.Booking.DecorService.Style,
                    QuotationCode = q.QuotationCode,
                    MaterialCost = q.MaterialCost,
                    ConstructionCost = q.ConstructionCost,
                    ProductCost = q.ProductCost,
                    DepositPercentage = q.DepositPercentage,
                    CreatedAt = q.CreatedAt,
                    Status = (int)q.Status,
                    IsQuoteExisted = q.isQuoteExisted,
                    IsContractExisted = q.Contract != null && q.Contract.isContractExisted,
                    IsSigned = q.Contract != null && q.Contract.isSigned == true,
                    FilePath = q.QuotationFilePath,

                    MaterialDetails = q.MaterialDetails.Select(m => new MaterialDetailResponse
                    {
                        MaterialName = m.MaterialName,
                        Quantity = m.Quantity,
                        Cost = m.Cost,
                        //Category = m.Category
                        Note = m.Note
                    }).ToList(),

                    ConstructionDetails = q.LaborDetails.Select(c => new ConstructionDetailResponse
                    {
                        TaskName = c.TaskName,
                        Cost = c.Cost,
                        Unit = c.Unit,
                        Area = c.Area,
                        Note = c.Note
                    }).ToList(),

                    ProductDetails = q.ProductDetails.Select(p => new ProductsDetailResponse
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Quantity = p.Quantity,
                        UnitPrice = p.UnitPrice,
                        TotalPrice = p.TotalPrice,
                        Image = p.Image
                    }).ToList(),

                    Customer = new CustomerResponse
                    {
                        Id = q.Booking.Account.Id,
                        FullName = $"{q.Booking.Account.FirstName} {q.Booking.Account.LastName}",
                        Avatar = q.Booking.Account.Avatar,
                        Phone = q.Booking.Account.Phone,
                        Email = q.Booking.Account.Email
                    }
                }).ToList();

                response.Success = true;
                response.Data = new PageResult<QuotationResponseForProvider>
                {
                    Data = quotationResponses,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving provider quotations";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<QuotationDetailResponseForCustomer>> GetQuotationDetailByCustomerAsync(string quotationCode, int customerId)
        {
            var response = new BaseResponse<QuotationDetailResponseForCustomer>();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Include(q => q.MaterialDetails)
                    .Include(q => q.LaborDetails)
                    .Include(q => q.ProductDetails)
                    .Include(q => q.Booking)// cần để truy cập AccountId
                        .ThenInclude(b => b.DecorService).ThenInclude(ds => ds.Account)
                    
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.DecorService).ThenInclude(ds => ds.DecorCategory)

                    .Include(q => q.Contract)
                    .FirstOrDefaultAsync(q => q.QuotationCode == quotationCode && q.Booking.AccountId == customerId);

                if (quotation == null)
                {
                    response.Message = "Quotation not found or access denied.";
                    return response;
                }

                var result = new QuotationDetailResponseForCustomer
                {
                    Id = quotation.Id,
                    QuotationCode = quotation.QuotationCode,
                    DecorCategoryName = quotation.Booking.DecorService.DecorCategory.CategoryName,
                    Style = quotation.Booking.DecorService.Style,
                    MaterialCost = quotation.MaterialCost,
                    ConstructionCost = quotation.ConstructionCost,
                    ProductCost = quotation.ProductCost,
                    DepositPercentage = quotation.DepositPercentage,
                    QuotationFilePath = quotation.QuotationFilePath,
                    Status = (int)quotation.Status,
                    IsQuoteExisted = quotation.isQuoteExisted,
                    IsContractExisted = quotation.Contract != null && quotation.Contract.isContractExisted,
                    IsSigned = quotation.Contract != null && quotation.Contract.isSigned == true,
                    CreatedAt = quotation.CreatedAt,

                    Materials = quotation.MaterialDetails.Select(m => new MaterialDetailResponse
                    {
                        MaterialName = m.MaterialName,
                        Quantity = m.Quantity,
                        Cost = m.Cost,
                        Note = m.Note,
                    }).ToList(),

                    ConstructionTasks = quotation.LaborDetails.Select(c => new ConstructionDetailResponse
                    {
                        TaskName = c.TaskName,
                        Cost = c.Cost,
                        Unit = c.Unit,
                        Area = c.Area,
                        Note = c.Note
                    }).ToList(),

                    ProductDetails = quotation.ProductDetails.Select(p => new ProductsDetailResponse
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Quantity = p.Quantity,
                        UnitPrice = p.UnitPrice,
                        TotalPrice = p.TotalPrice,
                        Image = p.Image
                    }).ToList(),

                    Provider = new ProviderResponse
                    {
                        BusinessName = quotation.Booking.DecorService.Account.BusinessName,
                        Avatar = quotation.Booking.DecorService.Account.Avatar,
                    },
                };

                response.Success = true;
                response.Message = "Quotation details retrieved successfully.";
                response.Data = result;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to retrieve quotation details.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<RelatedProductPageResult>> GetPaginatedRelatedProductAsync(PagingRelatedProductRequest request)
        {
            var response = new BaseResponse<RelatedProductPageResult>();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                                                .Include(q => q.Booking)
                                                    .ThenInclude(b => b.DecorService)
                                                    .ThenInclude(ds => ds.Account)
                                                    .Where(q => q.QuotationCode == request.QuotationCode)
                                                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found!";
                    return response;
                }

                // Get provider
                var provider = quotation?.Booking.DecorService.Account;

                if (provider == null)
                {
                    response.Message = "Provider not found!";
                    return response;
                }

                // Get decor services
                var decorServices = await _unitOfWork.DecorServiceRepository.Queryable()
                                            .Include(ds => ds.DecorCategory)
                                            .Where(ds => ds.AccountId == provider.Id)
                                            .ToListAsync();

                if (decorServices == null || !decorServices.Any())
                {
                    response.Message = "Provider has no decor services!";
                    return response;
                }

                // Get decor category names used by provider
                var providerDecorCategories = decorServices
                    .Select(ds => ds.DecorCategory.CategoryName)
                    .Distinct()
                    .ToList();

                var decorServiceSeasons = await _unitOfWork.DecorServiceRepository.Queryable()
                                            .Where(ds => ds.Id == quotation.Booking.DecorServiceId)
                                            .Include(ds => ds.DecorServiceSeasons)
                                                .ThenInclude(dss => dss.Season)
                                            .SelectMany(ds =>ds.DecorServiceSeasons.Select(dss => dss.SeasonId))
                                            .ToListAsync();

                // Map decor category -> allowed product categories
                var allowedProductCategories = providerDecorCategories
                    .SelectMany(decorCategory =>
                        DecorCategoryMapping.DecorToProductCategoryMap.TryGetValue(decorCategory, out var relatedProductCats)
                            ? relatedProductCats
                            : new List<string>())
                    .Distinct()
                    .ToList();

                // Filter expression
                Expression<Func<Product, bool>> filter = p =>
                    p.AccountId == provider.Id && allowedProductCategories.Contains(p.Category.CategoryName) &&
                    p.ProductSeasons.Any(ps => decorServiceSeasons.Contains(ps.SeasonId)) &&
                    (string.IsNullOrEmpty(request.Category) || p.Category.CategoryName == request.Category);

                // Check user
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(request.UserId);

                if ((account != null && account.RoleId == 1) || (account != null && account.RoleId == 2))
                {
                    // Display all products
                }
                else
                {
                    filter = filter.And(p => p.Quantity > 0); // Display in stock products
                }

                // Sort expression
                Expression<Func<Product, object>> orderByExpression = request.SortBy switch
                {
                    "ProductName" => p => p.ProductName,
                    "ProductPrice" => p => p.ProductPrice,
                    "CreateAt" => p => p.CreateAt,
                    _ => p => p.Id // default sort
                };

                // Include navigation properties
                Func<IQueryable<Product>, IQueryable<Product>> customQuery = query =>
                    query.Include(p => p.ProductImages)
                         .Include(p => p.Category)
                         .Include(p => p.ProductSeasons)
                            .ThenInclude(ps => ps.Season);

                // Get paginated products
                var (products, totalCount) = await _unitOfWork.ProductRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                var relatedProducts = new List<RelatedProductResponse>();

                foreach (var product in products)
                {
                    // Get orderDetails
                    var orderDetails = await _unitOfWork.OrderDetailRepository
                        .Queryable()
                        .Where(po => po.ProductId == product.Id && po.Order.Status == Order.OrderStatus.Paid)
                        .Include(po => po.Order)
                            .ThenInclude(o => o.Reviews)
                        .ToListAsync();

                    // Get reviews
                    var reviews = orderDetails
                                    .SelectMany(po => po.Order.Reviews)
                                    .ToList();

                    // Calculate average rate
                    var averageRate = reviews.Any() ? reviews.Average(r => r.Rate) : 0;

                    // Calculate total sold
                    var totalSold = orderDetails.Sum(od => od.Quantity);

                    var productResponse = new RelatedProductResponse
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        ProductPrice = product.ProductPrice,
                        Rate = averageRate,
                        TotalSold = totalSold,
                        Quantity = product.Quantity,
                        Status = product.Quantity > 0
                            ? Product.ProductStatus.InStock.ToString()
                            : Product.ProductStatus.OutOfStock.ToString(),
                        ImageUrls = product.ProductImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
                        Category = product.Category.CategoryName,
                        Seasons = product.ProductSeasons?
                             .Select(ps => ps.Season.SeasonName)
                             .Distinct()
                             .ToList() ?? new List<string>()
                    };

                    relatedProducts.Add(productResponse);
                }

                var decorCategory = quotation.Booking.DecorService.DecorCategory.CategoryName;

                var result = new RelatedProductPageResult
                {
                    Category = decorCategory,
                    Data = relatedProducts,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Products retrieved successfully.";
                response.Data = result;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving products!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> RequestCancelQuotationAsync(string quotationCode, int quotationCancelId, string cancelReason)
        {
            var response = new BaseResponse();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Include(q => q.Booking)
                    .ThenInclude(b => b.DecorService)
                    .Where(q => q.QuotationCode == quotationCode)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found or not in confirmable state.";
                    return response;
                }

                // Kiểm tra QuotationCancelId hợp lệ (lý do hủy phải hợp lệ từ QuotationCancel)
                var cancelReasonSeletion = await _unitOfWork.CancelTypeRepository.Queryable()
                    .FirstOrDefaultAsync(c => c.Id == quotationCancelId);

                if (cancelReasonSeletion == null)
                {
                    response.Message = "Invalid cancellation reason.";
                    return response;
                }

                // Cập nhật lý do hủy vào Quotation
                quotation.Status = Quotation.QuotationStatus.PendingCancel;
                quotation.CancelTypeId = quotationCancelId; // Lưu lý do hủy từ QuotationCancel
                quotation.CancelReason = cancelReason; // Lý do hủy do người dùng nhập

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Quotation cancellation request submitted. Awaiting provider approval.";

                // ✅ Gửi thông báo cho Provider
                var providerId = quotation.Booking?.DecorService?.AccountId;
                if (providerId != null)
                {
                    string colorBookingCode = $"<span style='color:#f66;font-weight:bold;'>#{quotation.Booking.BookingCode}</span>";
                    string url = $"{_clientBaseUrl}/seller/quotation";

                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = providerId.Value,
                        Title = "Requested To Cancel Booking",
                        Content = $"Customer has requested to cancel #{colorBookingCode}.",
                        Url = url
                    });
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to request cancellation.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> ApproveCancelQuotationAsync(string quotationCode)
        {
            var response = new BaseResponse();
            try
            {
                // Lấy thông tin Quotation từ QuotationCode
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Where(q => q.QuotationCode == quotationCode && q.Status == Quotation.QuotationStatus.PendingCancel)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                // Lấy Booking liên quan đến Quotation này
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Where(b => b.Id == quotation.BookingId)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found for this quotation.";
                    return response;
                }

                // Lấy Provider từ DecorService
                var providerId = await _unitOfWork.BookingRepository.Queryable()
                    .Where(ds => ds.Id == booking.DecorServiceId)
                    .Select(ds => ds.DecorService.AccountId)
                    .FirstOrDefaultAsync();

                if (providerId == 0)
                {
                    response.Message = "Provider not found.";
                    return response;
                }

                // Lấy % hoa hồng từ Setting (để tính số tiền trả lại cho Provider và Admin)
                var commissionRate = await _unitOfWork.SettingRepository.Queryable()
                    .Select(s => s.Commission)
                    .FirstOrDefaultAsync();

                // Lấy ví của Provider và Admin
                var providerWallet = await _unitOfWork.WalletRepository.Queryable()
                    .Where(w => w.AccountId == providerId)
                    .FirstOrDefaultAsync();

                var adminWallet = await _unitOfWork.WalletRepository.Queryable()
                    .Where(w => w.Account.RoleId == 1) // Giả sử role 1 là admin
                    .FirstOrDefaultAsync();

                if (providerWallet == null || adminWallet == null)
                {
                    response.Message = "Provider or Admin wallet not found.";
                    return response;
                }

                // Tính toán số tiền trả lại cho Provider và Admin
                decimal totalAmountToRefund = booking.CommitDepositAmount;
                decimal adminComissionAmount = totalAmountToRefund * commissionRate;
                decimal providerAmount = totalAmountToRefund - adminComissionAmount;

                // Cập nhật ví của Provider và Admin
                await _walletService.UpdateWallet(adminWallet.Id, adminWallet.Balance + adminComissionAmount);
                await _walletService.UpdateWallet(providerWallet.Id, providerWallet.Balance - adminComissionAmount);

                // Lưu giao dịch hoàn trả cho Provider và Admin
                var providerRefundTransaction = new PaymentTransaction
                {
                    Amount = providerAmount,
                    TransactionDate = DateTime.Now,
                    TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                    TransactionType = PaymentTransaction.EnumTransactionType.FinalPay,
                    BookingId = booking.Id
                };
                await _unitOfWork.PaymentTransactionRepository.InsertAsync(providerRefundTransaction);

                var adminRefundTransaction = new PaymentTransaction
                {
                    Amount = adminComissionAmount,
                    TransactionDate = DateTime.Now,
                    TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                    TransactionType = PaymentTransaction.EnumTransactionType.FinalPay,
                    BookingId = booking.Id
                };
                await _unitOfWork.PaymentTransactionRepository.InsertAsync(adminRefundTransaction);

                await _unitOfWork.CommitAsync(); // Lưu giao dịch vào cơ sở dữ liệu

                // Cập nhật trạng thái của Booking
                quotation.Status = QuotationStatus.Closed;
                quotation.isQuoteExisted = false;
                _unitOfWork.QuotationRepository.Update(quotation);

                booking.Status = Booking.BookingStatus.Canceled;
                booking.IsBooked = false;
                _unitOfWork.BookingRepository.Update(booking);

                await _unitOfWork.CommitAsync();

                // Thêm thông báo cho Provider và Admin
                string providerUrl = ""; // FE route cho provider
                string adminUrl = "";    // FE route cho admin

                // Thông báo cho Provider
                if (providerId > 0)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = providerId,
                        Title = "Booking Canceled",
                        Content = $"The customer has canceled booking with quotation #{quotationCode}.",
                        Url = providerUrl
                    });
                }

                // Thông báo cho Admin
                var adminIds = await _unitOfWork.AccountRepository.Queryable()
                    .Where(a => a.RoleId == 1) // Giả sử role 1 là admin
                    .Select(a => a.Id)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = adminId,
                        Title = "Revenue Notice",
                        Content = $"You have been credited with an additional amount in your income.",
                        Url = adminUrl
                    });
                }

                response.Success = true;
                response.Message = "Quotation successfully canceled.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to cancel the quotation.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> RequestChangeQuotationAsync(string quotationCode, string? changeReason)
        {
            var response = new BaseResponse();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.DecorService)
                    .Where(q => q.QuotationCode == quotationCode && q.Status == Quotation.QuotationStatus.Pending)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found or not in pending state.";
                    return response;
                }

                quotation.Status = Quotation.QuotationStatus.PendingChanged;
                quotation.CancelReason = changeReason;
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Quotation change request submitted. Awaiting provider approval.";

                // ✅ Gửi thông báo cho Provider
                var providerId = quotation.Booking?.DecorService?.AccountId;
                if (providerId != null)
                {
                    string colorQuotation = $"<span style='color:#5fc1f1;font-weight:bold;'>#{quotation.QuotationCode}</span>";
                    string url = $"{_clientBaseUrl}/seller/quotation";

                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = providerId.Value,
                        Title = "Requested To Change Quotation ",
                        Content = $"Customer has requested changes to quotation {colorQuotation}.",
                        Url = url
                    });
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to request quotation change.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> ApproveChangeQuotationAsync(string quotationCode)
        {
            var response = new BaseResponse();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Include(q => q.Booking)
                    .ThenInclude(b => b.DecorService) // để dùng .DecorService?.AccountId bên dưới
                    .Where(q => q.QuotationCode == quotationCode && q.Status == Quotation.QuotationStatus.PendingChanged)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found or not in pending rejection state.";
                    return response;
                }

                var booking = quotation.Booking;
                if (booking == null)
                {
                    response.Message = "Booking associated with this quotation was not found.";
                    return response;
                }

                quotation.Status = Quotation.QuotationStatus.Closed;
                booking.IsQuoted = false;
                quotation.isQuoteExisted = false;

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Quotation has been rejected. Provider can now create a new quotation.";

                // ✅ Gửi thông báo cho Provider
                var providerId = booking.DecorService?.AccountId;
                if (providerId != null)
                {
                    string colorQuotationCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{quotation.QuotationCode}</span>";
                    string url = $"{_clientBaseUrl}/seller/quotation";

                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = providerId.Value,
                        Title = "Quotation Change Successful",
                        Content = $"Approved request to change quotation {colorQuotationCode} success. Please create a new quotation.",
                        Url = url
                    });
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to approve rejection.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }


        public async Task<BaseResponse<QuotationCancelDetailResponse>> GetQuotationCancelDetailAsync(string quotationCode)
        {
            var response = new BaseResponse<QuotationCancelDetailResponse>();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Include(q => q.CancelType)
                    .FirstOrDefaultAsync(q => q.QuotationCode == quotationCode && q.Status == QuotationStatus.PendingCancel);

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                var result = new QuotationCancelDetailResponse
                {
                    QuotationCode = quotation.QuotationCode,
                    Status = (int)quotation.Status,
                    CancelType = quotation.CancelType?.Type,
                    Reason = quotation.CancelReason
                };

                response.Success = true;
                response.Message = "Get quotation cancellation detail successfully.";
                response.Data = result;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to get cancellation details.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }


        public async Task<BaseResponse<RequestQuotationChangeDetailResponse>> GetRequestQuotationChangeDetailAsync(string quotationCode)
        {
            var response = new BaseResponse<RequestQuotationChangeDetailResponse>();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Where(q => q.QuotationCode == quotationCode && q.Status == QuotationStatus.PendingChanged)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                var result = new RequestQuotationChangeDetailResponse
                {
                    QuotationCode = quotation.QuotationCode,
                    Status = (int)quotation.Status,
                    Reason = quotation.CancelReason
                };

                response.Success = true;
                response.Message = "Get request to change quotation detail successfully.";
                response.Data = result;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to get request to change quotation detail.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
        #region
        private string GenerateQuotationCode()
        {
            return "QUO" + DateTime.Now.Ticks;
        }
        #endregion
    }
}