using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Net.payOS.Types;
using Net.payOS;
using Repository.UnitOfWork;
using BusinessLogicLayer.POS;
using BusinessLogicLayer.Interfaces;

namespace BusinessLogicLayer.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOS _payOS;

        public BookingService(IUnitOfWork unitOfWork, PayOS payOS)
        {
            _unitOfWork = unitOfWork;
            _payOS = payOS;
        }

        /// <summary>
        /// Tạo booking (trạng thái ban đầu là Pending).
        /// Không tạo PaymentPhase ở đây.
        /// Validation: chỉ cho tài khoản khách hàng (isProvider=false) và không được book dịch vụ do chính mình tạo.
        /// </summary>
        public async Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                // Lấy Account và kiểm tra isProvider
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    response.Message = "Account not found.";
                    return response;
                }
                // Nếu account là provider, không được book
                if (account.Provider != null && account.Provider.IsProvider)
                {
                    response.Message = "Providers are not allowed to book services.";
                    return response;
                }

                // Lấy DecorService để kiểm tra xem người book có phải là người tạo dịch vụ không
                var decorService = await _unitOfWork.DecorServiceRepository.GetByIdAsync(request.DecorServiceId);
                if (decorService == null)
                {
                    response.Message = "DecorService not found.";
                    return response;
                }
                // Giả sử DecorService có navigation property Provider với AccountId
                if (decorService.AccountId == accountId)
                {
                    response.Message = "Service creator cannot book their own service.";
                    return response;
                }

                var booking = new Booking
                {
                    DecorServiceId = request.DecorServiceId,
                    BookingCode = GenerateBookingCode(), // Hoặc bạn có thể sinh mã khác cho hiển thị lịch sử
                    AccountId = accountId,
                    Status = Booking.BookingStatus.Pending,
                    CreateAt = DateTime.UtcNow.ToLocalTime(),
                    VoucherId = null
                };

                await _unitOfWork.BookingRepository.InsertAsync(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking created successfully (Pending).";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Booking creation failed.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Các API chuyển trạng thái (Confirm, Survey, Procuring, Progressing, Completed, Cancelled)
        /// Chúng ta có thể gộp lại logic chuyển trạng thái đơn thuần vào 1 API AdvanceBookingPhaseAsync.
        /// Với điều kiện đặc biệt: từ Surveying sang Procuring thì cần có PaymentPhase Deposit đã hoàn thành.
        /// </summary>
        public async Task<BaseResponse> AdvanceBookingPhaseAsync(int bookingId)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                Booking.BookingStatus nextStatus;
                switch (booking.Status)
                {
                    case Booking.BookingStatus.Pending:
                        nextStatus = Booking.BookingStatus.Confirmed;
                        break;
                    case Booking.BookingStatus.Confirmed:
                        nextStatus = Booking.BookingStatus.Surveying;
                        break;
                    case Booking.BookingStatus.Surveying:
                        // Chuyển sang Procuring chỉ nếu có PaymentPhase Deposit đã hoàn thành (PaymentDate != null)
                        var depositPhase = await _unitOfWork.PaymentPhaseRepository
                            .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.Deposit)
                            .FirstOrDefaultAsync();
                        if (depositPhase == null || depositPhase.PaymentDate == null)
                        {
                            response.Message = "Cannot advance to Procuring: Deposit payment not completed.";
                            return response;
                        }
                        nextStatus = Booking.BookingStatus.Procuring;
                        break;
                    case Booking.BookingStatus.Procuring:
                        nextStatus = Booking.BookingStatus.Progressing;
                        break;
                    case Booking.BookingStatus.Progressing:
                        // Để hoàn thành, cần cả FinalPayment hoàn thành
                        var finalPhase = await _unitOfWork.PaymentPhaseRepository
                            .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.FinalPayment)
                            .FirstOrDefaultAsync();
                        if (finalPhase == null || finalPhase.PaymentDate == null)
                        {
                            response.Message = "Cannot advance to Completed: Final payment not completed.";
                            return response;
                        }
                        nextStatus = Booking.BookingStatus.Completed;
                        break;
                    default:
                        response.Message = "No further advancement possible.";
                        return response;
                }

                booking.Status = nextStatus;
                _unitOfWork.BookingRepository.Update(booking);

                // Nếu booking đã Completed, tính lại TotalPrice: tổng ScheduledAmount của Deposit và FinalPayment.
                if (nextStatus == Booking.BookingStatus.Completed)
                {
                    double depositAmount = 0, finalAmount = 0;
                    var depositPhase = await _unitOfWork.PaymentPhaseRepository
                        .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.Deposit)
                        .FirstOrDefaultAsync();
                    var finalPhase = await _unitOfWork.PaymentPhaseRepository
                        .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.FinalPayment)
                        .FirstOrDefaultAsync();
                    if (depositPhase != null)
                        depositAmount = depositPhase.ScheduledAmount;
                    if (finalPhase != null)
                        finalAmount = finalPhase.ScheduledAmount;
                    booking.TotalPrice = depositAmount + finalAmount;
                }

                await _unitOfWork.CommitAsync();
                response.Success = true;
                response.Message = $"Booking advanced to {booking.Status}.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error advancing booking phase.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Sau khi khảo sát, customer chốt OK và đặt cọc.
        /// Tạo PaymentPhase (Deposit) nếu chưa có, với ScheduledAmount = depositAmount.
        /// </summary>
        public async Task<BaseResponse> ApproveSurveyAndDepositAsync(int bookingId, double depositAmount)
        {
            var response = new BaseResponse();
            try
            {
                // 1) Lấy booking và kiểm tra
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }
                if (booking.Status != Booking.BookingStatus.Surveying)
                {
                    response.Message = "Booking must be in Surveying status for deposit.";
                    return response;
                }

                // 2) Tạo (hoặc tìm) PaymentPhase cho Deposit
                var depositPhase = await _unitOfWork.PaymentPhaseRepository
                    .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.Deposit)
                    .FirstOrDefaultAsync();

                // Sinh orderCode và description theo định dạng "yyMMdd" + bookingId (3 chữ số)
                long orderCode = 0;
                if (!long.TryParse("", out orderCode))
                {
                    orderCode = DateTimeOffset.Now.ToUnixTimeSeconds();
                }

                string phaseDescription = $"DatCocNGLieuID#{bookingId}";
                if (phaseDescription.Length > 25)
                {
                    phaseDescription = phaseDescription.Substring(0, 25);
                }

                if (depositPhase == null)
                {
                    depositPhase = new PaymentPhase
                    {
                        BookingId = bookingId,
                        Phase = PaymentPhase.PaymentPhaseType.Deposit,
                        ScheduledAmount = depositAmount,
                        OrderCode = orderCode,
                        Description = phaseDescription
                        // Các trường DueDate, PaymentPhaseStatus đã bị loại bỏ
                    };
                    await _unitOfWork.PaymentPhaseRepository.InsertAsync(depositPhase);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    depositPhase.ScheduledAmount = depositAmount;
                    depositPhase.OrderCode = orderCode;
                    depositPhase.Description = phaseDescription;
                    _unitOfWork.PaymentPhaseRepository.Update(depositPhase);
                    await _unitOfWork.CommitAsync();
                }

                // 3) Tạo PaymentData để gọi API payOS
                int depositAmountInt = (int)Math.Round(depositAmount);
                var items = new List<ItemData>
                {
                    new ItemData("Đặt cọc chuẩn bị nguyên liệu", 1, depositAmountInt)
                };

                string paymentDescription = phaseDescription; // Sử dụng cùng description
                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: depositAmountInt,
                    description: paymentDescription,
                    items: items,
                    cancelUrl: "http://localhost:5297/payment-cancel",
                    returnUrl: "http://localhost:5297/payment-success"
                );

                var payResult = await _payOS.createPaymentLink(paymentData);

                // 4) Lưu thông tin giao dịch đặt cọc trong PaymentPhase (ví dụ, cập nhật PaymentDate)
                depositPhase.PaymentDate = DateTime.UtcNow.ToLocalTime();
                _unitOfWork.PaymentPhaseRepository.Update(depositPhase);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Deposit payment link created; please complete payment via the provided link.";
                response.Data = new
                {
                    CheckoutUrl = payResult.checkoutUrl,
                    Booking = booking,
                    OrderCode = orderCode
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error processing deposit payment.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Khi thi công xong, khách hàng thanh toán phần cuối (FinalPayment) và booking chuyển sang Completed.
        /// Tạo PaymentPhase (FinalPayment) nếu chưa có, với ScheduledAmount = finalAmount.
        /// </summary>
        /// <summary>
        /// Xử lý thanh toán cuối (FinalPayment) và chuyển booking sang Completed.
        /// Tạo PaymentPhase (FinalPayment) nếu chưa có, với ScheduledAmount = finalAmount.
        /// Sử dụng logic orderCode: nếu finalPayment.Code không hợp lệ, sinh bằng Unix time.
        /// </summary>
        public async Task<BaseResponse> CompleteBookingAsync(int bookingId, double finalAmount)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }
                if (booking.Status != Booking.BookingStatus.Progressing)
                {
                    response.Message = "Booking must be in Progressing status to complete.";
                    return response;
                }

                var finalPhase = await _unitOfWork.PaymentPhaseRepository
                    .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.FinalPayment)
                    .FirstOrDefaultAsync();

                long orderCode = 0;
                // Giả sử finalPayment.Code nằm trong finalAmount (vì PaymentRequest không chứa Code)
                // Nếu không có, sinh bằng Unix time.
                // Bạn có thể tùy chỉnh nếu muốn nhận Code từ FE.
                if (!long.TryParse("", out orderCode))
                {
                    orderCode = DateTimeOffset.Now.ToUnixTimeSeconds();
                }
                string phaseDescription = $"ThanhToanThiCong#{bookingId}";
                if (phaseDescription.Length > 25)
                    phaseDescription = phaseDescription.Substring(0, 25);

                if (finalPhase == null)
                {
                    finalPhase = new PaymentPhase
                    {
                        BookingId = bookingId,
                        Phase = PaymentPhase.PaymentPhaseType.FinalPayment,
                        ScheduledAmount = finalAmount,
                        OrderCode = orderCode,
                        Description = phaseDescription
                    };
                    await _unitOfWork.PaymentPhaseRepository.InsertAsync(finalPhase);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    finalPhase.ScheduledAmount = finalAmount;
                    finalPhase.OrderCode = orderCode;
                    finalPhase.Description = phaseDescription;
                    _unitOfWork.PaymentPhaseRepository.Update(finalPhase);
                    await _unitOfWork.CommitAsync();
                }

                int finalAmountInt = (int)Math.Round(finalAmount);
                var items = new List<ItemData>
                {
                    new ItemData("Thanh toán thi công trang trí", 1, finalAmountInt)
                };

                string paymentDescription = phaseDescription;
                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: finalAmountInt,
                    description: paymentDescription,
                    items: items,
                    cancelUrl: "http://example.com/payment-cancel",
                    returnUrl: "http://example.com/payment-success"
                );

                var payResult = await _payOS.createPaymentLink(paymentData);

                if (payResult != null)
                {
                    finalPhase.PaymentDate = DateTime.UtcNow.ToLocalTime();
                    _unitOfWork.PaymentPhaseRepository.Update(finalPhase);

                    booking.Status = Booking.BookingStatus.Completed;
                    // Tính TotalPrice = ScheduledAmount của Deposit + FinalPayment
                    double depositAmount = 0;
                    var depositPhase = await _unitOfWork.PaymentPhaseRepository
                        .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.Deposit)
                        .FirstOrDefaultAsync();
                    if (depositPhase != null)
                        depositAmount = depositPhase.ScheduledAmount;
                    booking.TotalPrice = depositAmount + finalAmount;
                    _unitOfWork.BookingRepository.Update(booking);

                    await _unitOfWork.CommitAsync();

                    response.Success = true;
                    response.Message = "Final payment processed; booking completed.";
                    response.Data = new
                    {
                        CheckoutUrl = payResult.checkoutUrl,
                        Booking = booking,
                        OrderCode = orderCode.ToString()
                    };
                }
                else
                {
                    response.Message = "Final payment failed.";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error processing final payment.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Hủy booking và cập nhật trạng thái của các giai đoạn thanh toán liên quan.
        /// </summary>
        public async Task<BaseResponse> CancelBookingAsync(int bookingId)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Success = false;
                    response.Message = "Booking not found.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.Cancelled;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking cancelled successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error cancelling booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        } 

        /// <summary>
        /// API lấy lịch sử booking cho một tài khoản.
        /// Nếu booking đã Completed và có đủ 2 PaymentPhase (Deposit & FinalPayment),
        /// TotalPrice được tính là tổng ScheduledAmount của 2 PaymentPhase.
        /// </summary>
        public async Task<BaseResponse> GetBookingHistoryAsync(int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var bookings = await _unitOfWork.BookingRepository.Query(b => b.AccountId == accountId)
                    .Include(b => b.PaymentPhases)
                    .ToListAsync();

                // Chỉ lấy các booking có trạng thái Completed, và có cả Deposit và FinalPayment hoàn thành
                var history = bookings.Where(b => b.Status == Booking.BookingStatus.Completed &&
                    b.PaymentPhases.Any(pp => pp.Phase == PaymentPhase.PaymentPhaseType.Deposit && pp.PaymentDate != null) &&
                    b.PaymentPhases.Any(pp => pp.Phase == PaymentPhase.PaymentPhaseType.FinalPayment && pp.PaymentDate != null))
                    .Select(b =>
                    {
                        double deposit = b.PaymentPhases
                            .Where(pp => pp.Phase == PaymentPhase.PaymentPhaseType.Deposit && pp.PaymentDate != null)
                            .Sum(pp => pp.ScheduledAmount);
                        double final = b.PaymentPhases
                            .Where(pp => pp.Phase == PaymentPhase.PaymentPhaseType.FinalPayment && pp.PaymentDate != null)
                            .Sum(pp => pp.ScheduledAmount);
                        b.TotalPrice = deposit + final;
                        return b;
                    }).ToList();

                response.Success = true;
                response.Message = "Booking history retrieved.";
                response.Data = history;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving booking history.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        #region
        private string GenerateBookingCode()
        {
            return "BKG-" + DateTime.UtcNow.Ticks;
        }
        #endregion
    }
}
