using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayosService _payosService;
        public BookingService(IUnitOfWork unitOfWork, IPayosService payosService)
        {
            _unitOfWork = unitOfWork;
            _payosService = payosService;
        }

        // 1. Tạo booking
        public async Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var booking = new Booking
                {
                    BookingCode = GenerateBookingCode(),
                    TotalPrice = request.TotalPrice,
                    CreateAt = DateTime.UtcNow,
                    Status = Booking.BookingStatus.Pending,
                    AccountId = accountId,
                    DecorServiceId = request.DecorServiceId,
                    VoucherId = request.VoucherId
                };

                await _unitOfWork.BookingRepository.InsertAsync(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking created successfully.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to create booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // 2. Xác nhận booking + tạo PaymentPhase
        public async Task<BaseResponse> ConfirmBookingAsync(ConfirmBookingRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository
                    .Query(b => b.Id == request.BookingId)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                if (booking.Status != Booking.BookingStatus.Pending && booking.Status != Booking.BookingStatus.Surveying)
                {
                    response.Message = "Booking must be in Pending/Surveying status to confirm.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.Confirmed;
                _unitOfWork.BookingRepository.Update(booking);

                foreach (var phaseReq in request.PaymentPhases)
                {
                    var phase = new PaymentPhase
                    {
                        BookingId = booking.Id,
                        Phase = (PaymentPhase.PaymentPhaseType)phaseReq.PhaseType,
                        ScheduledAmount = phaseReq.ScheduledAmount,
                        DueDate = phaseReq.DueDate,
                        Status = PaymentPhase.PaymentPhaseStatus.Pending
                    };
                    await _unitOfWork.PaymentPhaseRepository.InsertAsync(phase);
                }

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking confirmed & payment phases created.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to confirm booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // 3. Update booking status thủ công
        public async Task<BaseResponse> UpdateBookingStatusAsync(UpdateBookingStatusRequest request)
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

                booking.Status = (Booking.BookingStatus)request.NewStatus;
                _unitOfWork.BookingRepository.Update(booking);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking status updated.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update booking status.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // 4. Thanh toán 1 giai đoạn
        public async Task<BaseResponse> MakePaymentAsync(MakePaymentRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository
                    .Query(b => b.Id == request.BookingId)
                    .Include(b => b.PaymentPhases)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                var phase = booking.PaymentPhases.FirstOrDefault(p => p.Id == request.PaymentPhaseId);
                if (phase == null)
                {
                    response.Message = "Payment phase not found.";
                    return response;
                }

                // Tạo orderCode kiểu long (ví dụ: booking.Id * 1000 + phase.Id)
                long orderCode = booking.Id * 1000 + phase.Id;
                // Ép kiểu số tiền từ double sang int (theo SDK yêu cầu)
                int paymentAmount = (int)request.Amount;

                // Gọi PayosService tạo link thanh toán
                var createPaymentResult = await _payosService.CreatePaymentLinkAsync(
                    orderCode,
                    paymentAmount,
                    $"Thanh toán booking {booking.Id} - Phase {phase.Phase}",
                    null // Nếu có danh sách ItemData, truyền vào đây
                );

                // Giả sử nếu PaymentLink rỗng, coi như tạo link thất bại
                if (string.IsNullOrEmpty(createPaymentResult.paymentLinkId))
                {
                    response.Success = false;
                    response.Message = "Tạo link thanh toán PayOS thất bại.";
                    return response;
                }

                // Tạo record Payment với trạng thái Pending
                var payment = new Payment
                {
                    Code = GeneratePaymentCode(),
                    Date = DateTime.UtcNow,
                    Total = request.Amount,
                    Status = Payment.PaymentStatus.Pending,
                    BookingId = booking.Id,
                    AccountId = accountId,
                    PaymentPhaseId = phase.Id,
                    OrderId = request.OrderId
                };

                await _unitOfWork.PaymentRepository.InsertAsync(payment);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Tạo link thanh toán thành công, vui lòng chuyển hướng.";
                response.Data = new { PaymentUrl = createPaymentResult.paymentLinkId };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to make payment.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // 5. Lấy booking
        public async Task<BaseResponse> GetBookingAsync(int bookingId)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository
                    .Query(b => b.Id == bookingId)
                    .Include(b => b.PaymentPhases)
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                response.Success = true;
                response.Message = "Booking retrieved.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to get booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // Helper
        private string GenerateBookingCode()
        {
            return "BK" + DateTime.UtcNow.Ticks;
        }
        private string GeneratePaymentCode()
        {
            return "PM" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }
}
