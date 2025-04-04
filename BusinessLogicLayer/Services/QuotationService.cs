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

namespace BusinessLogicLayer.Services
{
    public class QuotationService: IQuotationService
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
                // 🔹 Tìm booking theo BookingCode
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                if (booking.Status != Booking.BookingStatus.Planning)
                {
                    response.Message = "Quotation can only be created during the Planning phase.";
                    return response;
                }

                // 🔹 Kiểm tra xem đã có báo giá chưa
                var existingQuotation = await _unitOfWork.QuotationRepository.Queryable()
                    .FirstOrDefaultAsync(q => q.BookingId == booking.Id);

                if (existingQuotation != null)
                {
                    response.Message = "Quotation already exists for this booking.";
                    return response;
                }

                // Tạo mã báo giá mới
                var quotationCode = $"QU{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";

                // Tính tổng chi phí
                decimal totalMaterialCost = request.Materials.Sum(m => m.Cost * m.Quantity);
                decimal totalConstructionCost = request.ConstructionTasks.Sum(c =>
                    c.Unit == "m2" ? (c.Cost * ((c.Length ?? 0m) * (c.Width ?? 0m))) : c.Cost);

                // Giới hạn đặt cọc tối đa 20%
                var depositPercentage = Math.Min(request.DepositPercentage, 20m);

                // Tạo báo giá
                var quotation = new Quotation
                {
                    BookingId = booking.Id, // 🔹 Lưu booking ID
                    QuotationCode = quotationCode,
                    MaterialCost = totalMaterialCost,
                    ConstructionCost = totalConstructionCost,
                    DepositPercentage = depositPercentage,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.QuotationRepository.InsertAsync(quotation);
                await _unitOfWork.CommitAsync(); // Lưu báo giá để lấy ID

                // Thêm chi tiết vật liệu
                var materialDetails = request.Materials.Select(m => new MaterialDetail
                {
                    QuotationId = quotation.Id,
                    MaterialName = m.MaterialName,
                    Quantity = m.Quantity,
                    Cost = m.Cost,
                    //Category = m.Category
                }).ToList();

                await _unitOfWork.MaterialDetailRepository.InsertRangeAsync(materialDetails);

                // Thêm chi tiết công trình
                var constructionDetails = request.ConstructionTasks.Select(c => new ConstructionDetail
                {
                    QuotationId = quotation.Id,
                    TaskName = c.TaskName,
                    Cost = c.Cost,
                    Unit = c.Unit
                }).ToList();

                await _unitOfWork.ConstructionDetailRepository.InsertRangeAsync(constructionDetails);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Quotation created successfully.";
                response.Data = new
                {
                    Quotation = quotation,
                    MaterialDetails = materialDetails,
                    ConstructionDetails = constructionDetails
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

        /// <summary>
        /// Lấy báo giá theo BookingId
        /// </summary>
        public async Task<BaseResponse> GetQuotationByBookingCodeAsync(string bookingCode)
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

                var materialDetails = await _unitOfWork.MaterialDetailRepository.Queryable()
                    .Where(m => m.QuotationId == quotation.Id)
                    .ToListAsync();

                var constructionDetails = await _unitOfWork.ConstructionDetailRepository.Queryable()
                    .Where(c => c.QuotationId == quotation.Id)
                    .ToListAsync();

                response.Success = true;
                response.Message = "Quotation retrieved successfully.";
                response.Data = new
                {
                    Quotation = quotation,
                    MaterialDetails = materialDetails,
                    ConstructionDetails = constructionDetails
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to retrieve quotation.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }


        //phải thêm logic +TotalPrice bên bảng Booking vào nữa
        public async Task<BaseResponse> ConfirmQuotationAsync(int bookingId)
        {
            var response = new BaseResponse();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .FirstOrDefaultAsync(q => q.BookingId == bookingId);

                if (quotation == null)
                {
                    response.Message = "Quotation not found. Please create a quotation first.";
                    return response;
                }

                // Kiểm tra nếu BookingDetail đã tồn tại
                var existingDetails = await _unitOfWork.BookingDetailRepository.Queryable()
                    .Where(bd => bd.BookingId == bookingId)
                    .ToListAsync();

                if (existingDetails.Any())
                {
                    response.Message = "Booking details already exist.";
                    response.Success = true;
                    return response;
                }

                // Tạo BookingDetail từ báo giá
                var bookingDetails = new List<BookingDetail>
        {
            new BookingDetail { BookingId = bookingId, ServiceItem = "Chi Phí Nguyên liệu", Cost = quotation.MaterialCost },
            new BookingDetail { BookingId = bookingId, ServiceItem = "Chi Phí Thi Công", Cost = quotation.ConstructionCost }
        };

                await _unitOfWork.BookingDetailRepository.InsertRangeAsync(bookingDetails);
                quotation.Status = Quotation.QuotationStatus.Confirmed; //chuyển sang trạng thái confirm
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Quotation confirmed and booking details created.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to confirm quotation.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<QuotationResponse>>> GetPaginatedQuotationsForCustomerAsync(QuotationFilterRequest request, int accountId)
        {
            var response = new BaseResponse<PageResult<QuotationResponse>>();
            try
            {
                // Filter condition
                Expression<Func<Quotation, bool>> filter = q =>
                    q.Booking.AccountId == accountId;

                // Sorting
                Expression<Func<Quotation, object>> orderByExpression = request.SortBy switch
                {
                    "QuotationCode" => q => q.QuotationCode,
                    "TotalCost" => q => (q.MaterialCost + q.ConstructionCost),
                    _ => q => q.CreatedAt
                };

                // Includes
                Func<IQueryable<Quotation>, IQueryable<Quotation>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.Address)
                    .Include(q => q.MaterialDetails)
                    .Include(q => q.ConstructionDetails);

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
                var quotationResponses = quotations.Select(q => new QuotationResponse
                {
                    Id = q.Id,
                    QuotationCode = q.QuotationCode,
                    MaterialCost = q.MaterialCost,
                    ConstructionCost = q.ConstructionCost,
                    DepositPercentage = q.DepositPercentage,
                    CreatedAt = q.CreatedAt,
                    FilePath = q.QuotationFilePath,
                    MaterialDetails = q.MaterialDetails.Select(m => new MaterialDetailResponse
                    {
                        MaterialName = m.MaterialName,
                        Quantity = m.Quantity,
                        Cost = m.Cost,
                        //Category = m.Category
                    }).ToList(),
                    ConstructionDetails = q.ConstructionDetails.Select(c => new ConstructionDetailResponse
                    {
                        TaskName = c.TaskName,
                        Cost = c.Cost,
                        Unit = c.Unit
                    }).ToList(),
                }).ToList();

                response.Success = true;
                response.Data = new PageResult<QuotationResponse>
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
                    q.Booking.DecorService.AccountId == providerId; 

                // Includes (additional customer info for provider view)
                Func<IQueryable<Quotation>, IQueryable<Quotation>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.Address)
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.Account)
                    .Include(q => q.MaterialDetails)
                    .Include(q => q.ConstructionDetails);

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
                    QuotationCode = q.QuotationCode,
                    // ... (same mapping as customer method)

                    // Additional provider-specific info
                    Customer = new CustomerResponse
                    {
                        Id = q.Booking.Account.Id,
                        FullName = $"{q.Booking.Account.FirstName} {q.Booking.Account.LastName}",
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
    }
}