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
using CloudinaryDotNet.Actions;

namespace BusinessLogicLayer.Services
{
    public class QuotationService : IQuotationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public QuotationService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
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
                    .FirstOrDefaultAsync(q => q.BookingId == booking.Id && q.Status != Quotation.QuotationStatus.Denied);

                if (existingQuotation != null)
                {
                    response.Message = existingQuotation.Status == Quotation.QuotationStatus.Confirmed
                        ? "Quotation already confirmed. Cannot create a new one."
                        : "Quotation already exists for this booking.";
                    return response;
                }

                // Tạo mã báo giá mới
                var quotationCode = GenerateQuotationCode();

                // Tính toán chi phí
                decimal totalMaterialCost = request.Materials.Sum(m => m.Cost * m.Quantity);
                decimal totalConstructionCost = request.ConstructionTasks.Sum(c =>
                    c.Unit == "m2" ? (c.Cost * ((c.Area ?? 0m))) : c.Cost);
                decimal? totalProductCost = null;

                var depositPercentage = Math.Min(request.DepositPercentage, 20m);

                List<ProductDetail> productDetails = new();
                if (request.Products != null && request.Products.Any())
                {
                    // Lấy thông tin sản phẩm từ cơ sở dữ liệu
                    foreach (var p in request.Products)
                    {
                        var product = await _unitOfWork.ProductRepository.Queryable()
                            .FirstOrDefaultAsync(prod => prod.Id == p.ProductId);

                        if (product != null)
                        {
                            productDetails.Add(new ProductDetail
                            {
                                ProductId = p.ProductId,
                                ProductName = product.ProductName,
                                Quantity = p.Quantity,
                                UnitPrice = product.ProductPrice,
                                TotalPrice = p.Quantity * product.ProductPrice
                            });
                        }
                    }

                    totalProductCost = productDetails.Sum(p => p.Quantity * p.UnitPrice);
                }

                // Tạo báo giá mới hoàn toàn
                var quotation = new Quotation
                {
                    BookingId = booking.Id,
                    QuotationCode = quotationCode,
                    MaterialCost = totalMaterialCost,
                    ConstructionCost = totalConstructionCost,
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
                }).ToList();

                await _unitOfWork.MaterialDetailRepository.InsertRangeAsync(materialDetails);

                // Thêm chi tiết công trình
                var constructionDetails = request.ConstructionTasks.Select(c => new LaborDetail
                {
                    QuotationId = quotation.Id,
                    TaskName = c.TaskName,
                    Cost = c.Cost,
                    Unit = c.Unit,
                    Area = c.Area
                }).ToList();

                await _unitOfWork.LaborDetailRepository.InsertRangeAsync(constructionDetails);
                quotation.isQuoteExisted = true;
                await _unitOfWork.CommitAsync();

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
        //public async Task<BaseResponse> GetQuotationByBookingCodeAsync(string bookingCode)
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

        public async Task<BaseResponse> ConfirmQuotationAsync(string quotationCode, bool isConfirmed)
        {
            var response = new BaseResponse();
            try
            {
                // Tìm quotation theo mã
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Include(q => q.Booking)
                    .FirstOrDefaultAsync(q => q.QuotationCode == quotationCode && q.Status == Quotation.QuotationStatus.Pending);

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
                    booking.TotalPrice = quotation.MaterialCost + quotation.ConstructionCost + quotation.ProductCost.Value;
                }
                else
                {
                    booking.TotalPrice = quotation.MaterialCost + quotation.ConstructionCost;
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
                        totalCost = quotation.MaterialCost + quotation.ConstructionCost + quotation.ProductCost.Value;
                    }
                    else
                    {
                        totalCost = quotation.MaterialCost + quotation.ConstructionCost;
                    }
                    
                    decimal depositAmount = (quotation.DepositPercentage / 100) * totalCost;

                    // Tạo BookingDetail mới
                    var bookingDetails = new List<BookingDetail>
                    {
                        new BookingDetail { BookingId = booking.Id, ServiceItem = "Materials Cost", Cost = quotation.MaterialCost },
                        new BookingDetail { BookingId = booking.Id, ServiceItem = "Construction Cost", Cost = quotation.ConstructionCost }
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
                    quotation.Status = Quotation.QuotationStatus.Denied;
                    quotation.isQuoteExisted = false;

                    response.Message = "Quotation has been denied. You can now create a new quotation.";
                }

                await _unitOfWork.CommitAsync();
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

        private async Task DeleteQuotationDetails(int quotationId)
        {
            // Xóa material details
            var materialDetails = await _unitOfWork.MaterialDetailRepository.Queryable()
                .Where(m => m.QuotationId == quotationId)
                .ToListAsync();

            if (materialDetails.Any())
            {
                _unitOfWork.MaterialDetailRepository.RemoveRange(materialDetails);
            }

            // Xóa construction details
            var constructionDetails = await _unitOfWork.LaborDetailRepository.Queryable()
                .Where(c => c.QuotationId == quotationId)
                .ToListAsync();

            if (constructionDetails.Any())
            {
                _unitOfWork.LaborDetailRepository.RemoveRange(constructionDetails);
            }
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
                    }).ToList(),

                    ConstructionDetails = q.LaborDetails.Select(c => new ConstructionDetailResponse
                    {
                        TaskName = c.TaskName,
                        Cost = c.Cost,
                        Unit = c.Unit,
                        Area = c.Area
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
                    }).ToList(),

                    ConstructionDetails = q.LaborDetails.Select(c => new ConstructionDetailResponse
                    {
                        TaskName = c.TaskName,
                        Cost = c.Cost,
                        Unit = c.Unit,
                        Area = c.Area
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
                    .Include(q => q.Booking)// cần để truy cập AccountId
                        .ThenInclude(b => b.DecorService).ThenInclude(ds => ds.Account)
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
                    Style = quotation.Booking.DecorService.Style,
                    MaterialCost = quotation.MaterialCost,
                    ConstructionCost = quotation.ConstructionCost,
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
                        Cost = m.Cost
                    }).ToList(),

                    ConstructionTasks = quotation.LaborDetails.Select(c => new ConstructionDetailResponse
                    {
                        TaskName = c.TaskName,
                        Cost = c.Cost,
                        Unit = c.Unit,
                        Area = c.Area
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

        #region
        private static int _quotationCounter = 0;

        private string GenerateQuotationCode()
        {
            _quotationCounter++;
            return $"QU{_quotationCounter:D4}";
        }
        #endregion
    }
}