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
    public class BookingService: IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOS _payOS;

        public BookingService(IUnitOfWork unitOfWork, PayOS payOS)
        {
            _unitOfWork = unitOfWork;
            _payOS = payOS;
        }

        /// <summary>
        /// Khi khách hàng tạo booking, trạng thái ban đầu là Pending.
        /// </summary>
        public async Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountid)
        {
            var response = new BaseResponse();
            try
            {
                var booking = new Booking
                {                   
                    DecorServiceId = request.DecorServiceId,
                    BookingCode = GenerateBookingCode(),
                    AccountId = accountid,
                    Status = Booking.BookingStatus.Pending, // Ban đầu là Pending
                    CreateAt = DateTime.UtcNow,
                    VoucherId = null
                    // Thiết lập các thuộc tính khác nếu cần
                };

                await _unitOfWork.BookingRepository.InsertAsync(booking);
                await _unitOfWork.CommitAsync();

                // Map danh sách PaymentPhase từ request
                var paymentPhases = request.PaymentPhases.Select(p => new PaymentPhase
                {
                    BookingId = booking.Id,
                    Phase = p.Phase,
                    ScheduledAmount = p.ScheduledAmount,
                    DueDate = p.DueDate,
                    Status = PaymentPhase.PaymentPhaseStatus.Pending
                }).ToList();

                foreach (var phase in paymentPhases)
                {
                    await _unitOfWork.PaymentPhaseRepository.InsertAsync(phase);
                }
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
        /// Provider xác nhận booking: chuyển trạng thái từ Pending -> Confirmed.
        /// </summary>
        public async Task<BaseResponse> ConfirmBookingAsync(int bookingId)
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
                if (booking.Status != Booking.BookingStatus.Pending)
                {
                    response.Message = "Booking must be in Pending status to confirm.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.Confirmed;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking confirmed successfully.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error confirming booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> StartSurveyAsync(int bookingId)
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
                if (booking.Status != Booking.BookingStatus.Confirmed)
                {
                    response.Message = "Booking must be in Confirmed status to start survey.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.Surveying;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking updated to Surveying. Provider and customer can now meet for survey.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error starting survey.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Sau khi khảo sát (Surveying), khách hàng chốt OK và đặt cọc.
        /// Tạo link thanh toán qua payOS, chuyển booking sang trạng thái Procuring.
        /// </summary>
        public async Task<BaseResponse> ApproveSurveyAndDepositAsync(int bookingId, Payment depositPayment)
        {
            var response = new BaseResponse();
            try
            {
                // 1) Lấy booking từ DB và kiểm tra
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

                // 2) Tìm giai đoạn đặt cọc (Deposit)
                var depositPhase = await _unitOfWork.PaymentPhaseRepository
                    .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.Deposit)
                    .FirstOrDefaultAsync();

                if (depositPhase == null)
                {
                    response.Message = "Deposit phase not found.";
                    return response;
                }

                // 3) Chuẩn bị orderCode (long) và ép kiểu amount (int)
                // Ví dụ: nếu ngày 05/03/2025 và bookingId = 1, code sẽ là "050325001"
                string formattedCode = DateTime.Now.ToString("ddMMyy") + bookingId.ToString("D3");
                long orderCode = long.Parse(formattedCode);

                // 4) PayOS yêu cầu amount kiểu int(ĐÂY CHÍNH LÀ SỐ TIỀN MÀ PROVIDER CẦN NHẬP SỐ TIỀN ĐẶT CỌC ĐỂ CHO CUSTOMER THANH TOÁN)
                int depositAmount = (int)Math.Round(depositPayment.Total);

                // 5) Tạo List<ItemData> cho PaymentData
                var items = new List<ItemData>
                {
                    new ItemData("Đặt cọc dịch vụ trang trí", 1, depositAmount)
                };

                // Tạo description ngắn (không vượt quá 25 ký tự)
                string description = $"Đặt cọc BookingID{bookingId}";
                if (description.Length > 25)
                {
                    description = description.Substring(0, 25);
                }

                // 6) Tạo PaymentData với đầy đủ tham số
                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: depositAmount,
                    description: description,
                    items: items,
                    cancelUrl: "http://localhost:5297/payment-cancel",
                    returnUrl: "http://localhost:5297/payment-success"
                // signature: null,
                // buyerName: null,
                // buyerEmail: null,
                // buyerPhone: null,
                // buyerAddress: null,
                // expiredAt: null
                );

                // 7) Gọi API payOS để tạo link thanh toán
                var payResult = await _payOS.createPaymentLink(paymentData);

                // 8) (MINH HỌA) Đánh dấu thanh toán cọc là completed ngay lập tức
                //    Trong thực tế, bạn nên đặt Pending và chờ webhook/callback.
                depositPayment.Status = Payment.PaymentStatus.Completed;
                depositPayment.PaymentPhaseId = depositPhase.Id;
                depositPayment.Date = DateTime.UtcNow;
                // Lưu depositPayment nếu bạn có PaymentRepository
                // await _unitOfWork.PaymentRepository.InsertAsync(depositPayment);

                // Cập nhật depositPhase
                depositPhase.Status = PaymentPhase.PaymentPhaseStatus.Completed;
                depositPhase.PaymentDate = DateTime.UtcNow;
                _unitOfWork.PaymentPhaseRepository.Update(depositPhase);

                // Chuyển booking sang Procuring
                booking.Status = Booking.BookingStatus.Procuring;
                _unitOfWork.BookingRepository.Update(booking);

                // Lưu thay đổi
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Deposit payment link created; booking updated to Procuring.";
                // Trả về link để client redirect người dùng
                response.Data = new
                {
                    CheckoutUrl = payResult.checkoutUrl,
                    Booking = booking
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
        /// Khi bắt đầu thi công, chuyển trạng thái từ Procuring -> Progressing.
        /// </summary>
        public async Task<BaseResponse> StartProgressAsync(int bookingId)
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
                if (booking.Status != Booking.BookingStatus.Procuring)
                {
                    response.Message = "Booking must be in Procuring status to start progress.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.Progressing;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking updated to Progressing. Construction has started.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error starting progress.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Khi thi công xong, khách hàng thanh toán phần cuối (FinalPayment) và booking chuyển sang Completed.
        /// </summary>
        public async Task<BaseResponse> CompleteBookingAsync(int bookingId, Payment finalPayment)
        {
            var response = new BaseResponse();
            try
            {
                // 1) Lấy booking từ DB và kiểm tra
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

                // 2) Tìm giai đoạn thanh toán cuối (FinalPayment)
                var finalPhase = await _unitOfWork.PaymentPhaseRepository
                    .Query(pp => pp.BookingId == bookingId && pp.Phase == PaymentPhase.PaymentPhaseType.FinalPayment)
                    .FirstOrDefaultAsync();
                if (finalPhase == null)
                {
                    response.Message = "Final payment phase not found.";
                    return response;
                }

                // 3) Chuẩn bị orderCode (long) và ép kiểu amount (int)
                long orderCode;
                if (!long.TryParse(finalPayment.Code, out orderCode))
                {
                    orderCode = DateTimeOffset.Now.ToUnixTimeSeconds();
                }
                int finalAmount = (int)Math.Round(finalPayment.Total);

                // 4) Tạo List<ItemData> cho PaymentData
                var items = new List<ItemData>
        {
            new ItemData("Thanh toán cuối dịch vụ trang trí", 1, finalAmount)
        };

                string description = $"DC#{bookingId}";
                if (description.Length > 25)
                {
                    description = description.Substring(0, 25);
                }
                // 5) Tạo PaymentData với đầy đủ tham số (chú ý thứ tự tham số)
                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: finalAmount,
                    description: description,
                    items: items,
                    cancelUrl: "https://yourdomain.com/payment-cancel",
                    returnUrl: "https://yourdomain.com/payment-success"
                );

                // 6) Gọi API payOS để tạo link thanh toán cuối
                var payResult = await _payOS.createPaymentLink(paymentData);

                // 7) (MINH HỌA) Đánh dấu thanh toán cuối là Completed ngay lập tức
                //    (Trong thực tế, nên cập nhật trạng thái sau callback/webhook từ payOS)
                if (payResult != null)
                {
                    finalPayment.Status = Payment.PaymentStatus.Completed;
                    finalPayment.PaymentPhaseId = finalPhase.Id;
                    finalPayment.Date = DateTime.UtcNow;
                    // Cập nhật trạng thái của finalPhase
                    finalPhase.Status = PaymentPhase.PaymentPhaseStatus.Completed;
                    finalPhase.PaymentDate = DateTime.UtcNow;
                    _unitOfWork.PaymentPhaseRepository.Update(finalPhase);

                    // Cập nhật booking sang Completed
                    booking.Status = Booking.BookingStatus.Completed;
                    _unitOfWork.BookingRepository.Update(booking);

                    await _unitOfWork.CommitAsync();

                    response.Success = true;
                    response.Message = "Final payment processed; booking completed.";
                    response.Data = new
                    {
                        CheckoutUrl = payResult.checkoutUrl,
                        Booking = booking
                    };
                }
                else
                {
                    finalPayment.Status = Payment.PaymentStatus.Failed;
                    await _unitOfWork.CommitAsync();
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
        #region
        private string GenerateBookingCode()
        {
            return "BKG-" + DateTime.UtcNow.Ticks;
        }
        #endregion
    }
}
