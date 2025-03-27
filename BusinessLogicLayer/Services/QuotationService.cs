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

namespace BusinessLogicLayer.Services
{
    public class QuotationService: IQuotationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuotationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tạo báo giá cho một booking
        /// </summary>
        public async Task<BaseResponse> CreateQuotationAsync(int bookingId, CreateQuotationRequest request)
        {
            var response = new BaseResponse();
            try
            {
                // Validate booking exists and status
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                if (booking.Status != Booking.BookingStatus.Survey)
                {
                    response.Message = "Quotation can only be created during the Survey phase.";
                    return response;
                }

                // Check if quotation already exists
                var existingQuotation = await _unitOfWork.QuotationRepository.Queryable()
                    .FirstOrDefaultAsync(q => q.BookingId == bookingId);

                if (existingQuotation != null)
                {
                    response.Message = "Quotation already exists for this booking.";
                    return response;
                }

                // Calculate totals
                decimal totalMaterialCost = request.Materials.Sum(m => m.Cost * m.Quantity);
                decimal totalConstructionCost = request.ConstructionTasks.Sum(c => c.Cost);

                // Create quotation
                var quotation = new Quotation
                {
                    BookingId = bookingId,
                    MaterialCost = totalMaterialCost,
                    ConstructionCost = totalConstructionCost,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.QuotationRepository.InsertAsync(quotation);
                await _unitOfWork.CommitAsync(); // Save quotation first to get Id

                // Create material details
                var materialDetails = request.Materials.Select(m => new MaterialDetail
                {
                    QuotationId = quotation.Id,
                    MaterialName = m.MaterialName,
                    Quantity = m.Quantity,
                    Cost = m.Cost,
                    Category = m.Category
                }).ToList();

                await _unitOfWork.MaterialDetailRepository.InsertRangeAsync(materialDetails);

                // Create construction details
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

        /// <summary>
        /// Lấy báo giá theo BookingId
        /// </summary>
        public async Task<BaseResponse<QuotationDetailResponse>> GetQuotationDetailAsync(int bookingId)
        {
            var response = new BaseResponse<QuotationDetailResponse>();
            try
            {
                // Lấy quotation kèm theo chi tiết
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Include(q => q.MaterialDetails)
                    .Include(q => q.ConstructionDetails)
                    .FirstOrDefaultAsync(q => q.BookingId == bookingId);

                if (quotation == null)
                {
                    response.Message = "Quotation not found for this booking.";
                    return response;
                }

                // Map sang response DTO
                var quotationDetail = new QuotationDetailResponse
                {
                    Id = quotation.Id,
                    BookingId = quotation.BookingId,
                    MaterialCost = quotation.MaterialCost,
                    ConstructionCost = quotation.ConstructionCost,
                    CreatedAt = quotation.CreatedAt,

                    Materials = quotation.MaterialDetails.Select(m => new MaterialDetailResponse
                    {
                        Id = m.Id,
                        MaterialName = m.MaterialName,
                        Quantity = m.Quantity,
                        Cost = m.Cost,
                        Category = m.Category
                    }).ToList(),

                    ConstructionTasks = quotation.ConstructionDetails.Select(c => new ConstructionDetailResponse
                    {
                        Id = c.Id,
                        TaskName = c.TaskName,
                        Cost = c.Cost,
                        Unit = c.Unit
                    }).ToList()
                };

                response.Success = true;
                response.Message = "Quotation details retrieved successfully.";
                response.Data = quotationDetail;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to retrieve quotation details.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> ConfirmQuotationAsync(int bookingId)
        {
            var response = new BaseResponse();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .FirstOrDefaultAsync(q => q.BookingId == bookingId);

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
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
            new BookingDetail { BookingId = bookingId, ServiceItem = "Nguyên liệu", Cost = quotation.MaterialCost },
            new BookingDetail { BookingId = bookingId, ServiceItem = "Thi công", Cost = quotation.ConstructionCost }
        };

                await _unitOfWork.BookingDetailRepository.InsertRangeAsync(bookingDetails);
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

    }

}