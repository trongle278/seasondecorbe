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
                    Category = m.Category
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