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
        public async Task<BaseResponse> CreateQuotationAsync(CreateQuotationRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(request.BookingId);
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

                var quotation = new Quotation
                {
                    BookingId = request.BookingId,
                    MaterialCost = request.MaterialCost,
                    LaborCost = request.LaborCost,
                };

                await _unitOfWork.QuotationRepository.InsertAsync(quotation);
                await _unitOfWork.CommitAsync(); // ✅ Lưu báo giá trước khi cập nhật booking

                // ✅ Cập nhật `QuotationId` trong `Booking`
                booking.QuotationId = quotation.Id;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync(); // ✅ Lưu lại Booking với `QuotationId` mới

                response.Success = true;
                response.Message = "Quotation created successfully.";
                response.Data = quotation;
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
        public async Task<BaseResponse> GetQuotationByBookingIdAsync(int bookingId)
        {
            var response = new BaseResponse();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .FirstOrDefaultAsync(q => q.BookingId == bookingId);

                if (quotation == null)
                {
                    response.Message = "Quotation not found for this booking.";
                    return response;
                }

                response.Success = true;
                response.Message = "Quotation retrieved successfully.";
                response.Data = quotation;
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
            new BookingDetail { BookingId = bookingId, ServiceItem = "Thi công", Cost = quotation.LaborCost }
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