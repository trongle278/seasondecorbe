using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;
using BusinessLogicLayer.Interfaces;

namespace BusinessLogicLayer.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly ITrackingService _trackingService;

        public BookingService(IUnitOfWork unitOfWork, IPaymentService paymentService, ITrackingService trackingService)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _trackingService = trackingService;
        }

        public async Task<BaseResponse<List<Booking>>> GetBookingsByUserAsync(int accountId)
        {
            var response = new BaseResponse<List<Booking>>();
            var bookings = await _unitOfWork.BookingRepository.Queryable()
                .Where(b => b.AccountId == accountId)
                .ToListAsync();
            response.Success = true;
            response.Data = bookings;
            return response;
        }

        public async Task<BaseResponse<Booking>> GetBookingDetailsAsync(int bookingId)
        {
            var response = new BaseResponse<Booking>();
            var booking = await _unitOfWork.BookingRepository.Queryable()
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
            response.Success = booking != null;
            response.Data = booking;
            return response;
        }

        public async Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                // 🔹 Kiểm tra nếu `DecorServiceId` hợp lệ
                var decorService = await _unitOfWork.DecorServiceRepository.Queryable()
                    .FirstOrDefaultAsync(ds => ds.Id == request.DecorServiceId);

                if (decorService == null)
                {
                    response.Message = "Invalid DecorServiceId. Please check your input.";
                    return response;
                }

                // 🔹 Kiểm tra nếu người tạo booking cũng là chủ của dịch vụ
                if (decorService.AccountId == accountId)
                {
                    response.Message = "You cannot create a booking for your own service.";
                    return response;
                }

                var booking = new Booking
                {
                    BookingCode = GenerateBookingCode(),
                    AccountId = accountId,
                    AddressId = request.AddressId,
                    DecorServiceId = request.DecorServiceId,
                    Status = Booking.BookingStatus.Pending,
                    CreateAt = DateTime.UtcNow
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

        public async Task<BaseResponse<bool>> ChangeBookingStatusAsync(int bookingId)
        {
            var response = new BaseResponse<bool>();
            var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                response.Message = "Booking not found.";
                return response;
            }

            // 🔹 Xác định trạng thái tiếp theo
            Booking.BookingStatus? newStatus = booking.Status switch
            {
                Booking.BookingStatus.Pending => Booking.BookingStatus.Survey,
                Booking.BookingStatus.Survey => Booking.BookingStatus.Confirm,
                Booking.BookingStatus.Confirm when booking.DepositAmount > 0 => Booking.BookingStatus.DepositPaid,
                Booking.BookingStatus.DepositPaid => Booking.BookingStatus.Preparing,
                Booking.BookingStatus.Preparing => Booking.BookingStatus.InTransit,
                Booking.BookingStatus.InTransit => Booking.BookingStatus.Progressing,
                Booking.BookingStatus.Progressing when booking.DepositAmount >= booking.TotalPrice => Booking.BookingStatus.ConstructionPayment,
                Booking.BookingStatus.ConstructionPayment => Booking.BookingStatus.Completed,
                _ => null // Giữ nguyên nếu không hợp lệ
            };

            if (newStatus == null)
            {
                response.Message = "Invalid status transition.";
                return response;
            }

            switch (newStatus)
            {
                case Booking.BookingStatus.Survey:
                    break;

                case Booking.BookingStatus.Confirm:
                    // 🔹 Khi booking chuyển sang Confirm, tạo BookingDetail từ Quotation
                    var quotation = await _unitOfWork.QuotationRepository.Queryable()
                        .FirstOrDefaultAsync(q => q.BookingId == bookingId);

                    if (quotation == null)
                    {
                        response.Message = "Quotation not found. Please create a quotation first.";
                        return response;
                    }

                    // ✅ Cập nhật `TotalPrice` trong `Booking`
                    booking.TotalPrice = quotation.MaterialCost + quotation.LaborCost;

                    // Kiểm tra nếu BookingDetail đã tồn tại
                    var existingDetails = await _unitOfWork.BookingDetailRepository.Queryable()
                        .Where(bd => bd.BookingId == bookingId)
                        .ToListAsync();

                    if (!existingDetails.Any())
                    {
                        var bookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { BookingId = bookingId, ServiceItem = "Chi Phí Nguyên liệu", Cost = quotation.MaterialCost },
                    new BookingDetail { BookingId = bookingId, ServiceItem = "Chi Phí Thi công", Cost = quotation.LaborCost }
                };

                        await _unitOfWork.BookingDetailRepository.InsertRangeAsync(bookingDetails);
                        await _unitOfWork.CommitAsync();
                    }
                    break;

                case Booking.BookingStatus.DepositPaid:
                    // ✅ Kiểm tra đã đặt cọc chưa
                    if (booking.DepositAmount == 0)
                    {
                        response.Message = "Deposit is required before proceeding.";
                        return response;
                    }
                    break;

                case Booking.BookingStatus.Preparing:
                case Booking.BookingStatus.InTransit:
                case Booking.BookingStatus.Progressing:
                    break;

                case Booking.BookingStatus.ConstructionPayment:
                    // ✅ Kiểm tra đã thanh toán thi công chưa trước khi chuyển sang `ConstructionPayment`
                    if (booking.TotalPrice > 0 && booking.DepositAmount < booking.TotalPrice)
                    {
                        response.Message = "Full payment is required before proceeding.";
                        return response;
                    }
                    break;

                case Booking.BookingStatus.Completed:
                    break;
            }

            // Cập nhật trạng thái booking
            booking.Status = newStatus.Value;
            _unitOfWork.BookingRepository.Update(booking);
            await _unitOfWork.CommitAsync();

            // ✅ Gọi `AddBookingTrackingAsync` để lưu tracking
            await _trackingService.AddTrackingAsync(booking.Id, newStatus.Value, "Status updated automatically.");

            response.Success = true;
            response.Message = $"Booking status changed to {newStatus}.";
            response.Data = true;
            return response;
        }


        public async Task<BaseResponse<bool>> CancelBookingAsync(int bookingId)
        {
            var response = new BaseResponse<bool>();
            var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
            if (booking == null || (booking.Status != Booking.BookingStatus.Pending && booking.Status != Booking.BookingStatus.Survey))
            {
                response.Message = "Booking cannot be cancelled.";
                return response;
            }
            booking.Status = Booking.BookingStatus.Cancelled;
            _unitOfWork.BookingRepository.Update(booking);
            await _unitOfWork.CommitAsync();
            response.Success = true;
            response.Data = true;
            return response;
        }

        public async Task<BaseResponse> ProcessDepositAsync(int bookingId)
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
                if (booking.Status != Booking.BookingStatus.Confirm)
                {
                    response.Message = "Only confirmed bookings can be deposited.";
                    return response;
                }

                // Tính tổng tiền đặt cọc = nguyên liệu + thi công
                var totalAmount = await _unitOfWork.BookingDetailRepository.Queryable()
                    .Where(bd => bd.BookingId == bookingId)
                    .SumAsync(bd => (decimal)bd.Cost);

                // Áp dụng đặt cọc 20%
                decimal depositRate = 0.2m; // 20%
                var depositAmount = totalAmount * depositRate;

                var adminAccount = await _unitOfWork.AccountRepository.Queryable()
                    .Where(a => a.Role.RoleName == "Admin")
                    .FirstOrDefaultAsync();
                if (adminAccount == null)
                {
                    response.Message = "Admin account not found.";
                    return response;
                }

                // Gọi PaymentService để thực hiện đặt cọc
                bool paymentSuccess = await _paymentService.Deposit(booking.AccountId, adminAccount.Id, depositAmount, booking.Id);
                if (!paymentSuccess)
                {
                    response.Message = "Deposit payment failed.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.DepositPaid;
                booking.DepositAmount = depositAmount;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = $"Deposit successful: {depositAmount} transferred.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to process deposit.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> ProcessConstructionPaymentAsync(int bookingId)
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
                    response.Message = "Booking must be in Progressing state to pay for construction.";
                    return response;
                }

                // Tính số tiền cần thanh toán (tổng giá - đã đặt cọc)
                var totalCost = await _unitOfWork.BookingDetailRepository.Queryable()
                    .Where(bd => bd.BookingId == bookingId)
                    .SumAsync(bd => (decimal)bd.Cost);

                var remainingAmount = totalCost - booking.DepositAmount;
                if (remainingAmount <= 0)
                {
                    response.Message = "No remaining balance to pay.";
                    return response;
                }

                var adminAccount = await _unitOfWork.AccountRepository.Queryable()
                    .Where(a => a.Role.RoleName == "Admin")
                    .FirstOrDefaultAsync();
                if (adminAccount == null)
                {
                    response.Message = "Admin account not found.";
                    return response;
                }

                // Gọi PaymentService để thanh toán phần còn lại
                bool paymentSuccess = await _paymentService.Pay(booking.AccountId, remainingAmount, adminAccount.Id, booking.Id);
                if (!paymentSuccess)
                {
                    response.Message = "Construction payment failed.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.ConstructionPayment;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = $"Construction payment successful: {remainingAmount} transferred.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to process construction payment.";
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
