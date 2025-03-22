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


        public BookingService(IUnitOfWork unitOfWork, IPaymentService paymentService)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
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

        public async Task<BaseResponse<Booking>> CreateBookingAsync(CreateBookingRequest request, int accountId)
        {
            var response = new BaseResponse<Booking>();

            var service = await _unitOfWork.DecorServiceRepository.GetByIdAsync(request.DecorServiceId);
            if (service == null)
            {
                response.Message = "Service not found.";
                return response;
            }

            var booking = new Booking
            {
                DecorServiceId = request.DecorServiceId,
                BookingCode = $"BKG-{DateTime.UtcNow.Ticks}",
                TotalPrice = 0, // Giá sẽ được cập nhật sau khi báo giá
                CreateAt = DateTime.Now,
                Status = Booking.BookingStatus.Pending,
                AccountId = accountId,
                AddressId = request.AddressId
            };

            await _unitOfWork.BookingRepository.InsertAsync(booking);
            await _unitOfWork.CommitAsync();

            response.Success = true;
            response.Message = "Booking created successfully.";
            response.Data = booking;
            return response;
        }

        public async Task<BaseResponse<bool>> ChangeBookingStatusAsync(int bookingId, Booking.BookingStatus newStatus)
        {
            var response = new BaseResponse<bool>();
            var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                response.Message = "Booking not found.";
                return response;
            }

            switch (booking.Status)
            {
                case Booking.BookingStatus.Pending when newStatus == Booking.BookingStatus.Survey:
                    break;

                case Booking.BookingStatus.Survey when newStatus == Booking.BookingStatus.Confirm:
                    // 🔹 Khi booking chuyển sang Confirm, tạo BookingDetail từ Quotation
                    var quotation = await _unitOfWork.QuotationRepository.Queryable()
                        .FirstOrDefaultAsync(q => q.BookingId == bookingId);

                    if (quotation == null)
                    {
                        response.Message = "Quotation not found. Please create a quotation first.";
                        return response;
                    }

                    var existingDetails = await _unitOfWork.BookingDetailRepository.Queryable()
                        .Where(bd => bd.BookingId == bookingId)
                        .ToListAsync();

                    if (!existingDetails.Any())
                    {
                        var bookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { BookingId = bookingId, ServiceItem = "Nguyên liệu", Cost = quotation.MaterialCost },
                    new BookingDetail { BookingId = bookingId, ServiceItem = "Thi công", Cost = quotation.LaborCost }
                };

                        await _unitOfWork.BookingDetailRepository.InsertRangeAsync(bookingDetails);
                        await _unitOfWork.CommitAsync();
                    }
                    break;

                case Booking.BookingStatus.Confirm when newStatus == Booking.BookingStatus.DepositPaid:
                case Booking.BookingStatus.DepositPaid when newStatus == Booking.BookingStatus.Preparing:
                case Booking.BookingStatus.Preparing when newStatus == Booking.BookingStatus.InTransit:
                case Booking.BookingStatus.InTransit when newStatus == Booking.BookingStatus.Progressing:
                case Booking.BookingStatus.Progressing when newStatus == Booking.BookingStatus.ConstructionPayment:
                case Booking.BookingStatus.ConstructionPayment when newStatus == Booking.BookingStatus.Completed:
                    break;

                default:
                    response.Message = "Invalid status transition.";
                    return response;
            }

            // Cập nhật trạng thái booking
            booking.Status = newStatus;
            _unitOfWork.BookingRepository.Update(booking);
            await _unitOfWork.CommitAsync();

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

                var depositAmount = totalAmount * 2/10; // Đặt cọc toàn bộ tiền nguyên liệu + thi công

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
