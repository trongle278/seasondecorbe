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

        /// <summary>
        /// Lấy danh sách booking của một người dùng cụ thể (có thể là customer hoặc provider)
        /// </summary>
        public async Task<BaseResponse> GetBookingsByUserAsync(int accountId, int page = 1, int pageSize = 10)
        {
            var response = new BaseResponse();
            try
            {
                // Kiểm tra tài khoản tồn tại
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    response.Message = "Account not found.";
                    return response;
                }

                // Tính toán số lượng bản ghi bỏ qua cho phân trang
                int skip = (page - 1) * pageSize;

                // Lấy danh sách booking theo loại tài khoản
                var query = _unitOfWork.BookingRepository.Queryable()
                    .Include(b => b.DecorService)
                    .Include(b => b.Address)
                    .AsQueryable();

                if (account.IsProvider == true)
                {
                    // Nếu là provider, lấy các booking của dịch vụ mà provider cung cấp
                    query = query.Where(b => b.DecorService.AccountId == accountId);
                }
                else
                {
                    // Nếu là customer, lấy các booking mà customer đã tạo
                    query = query.Where(b => b.AccountId == accountId);
                }

                // Đếm tổng số bản ghi để phân trang
                var totalRecords = await query.CountAsync();

                // Lấy dữ liệu theo phân trang và sắp xếp theo thời gian tạo (mới nhất trước)
                var bookings = await query
                    .OrderByDescending(b => b.CreateAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                // Map sang DTO nếu cần (giả sử có BookingDTO)
                // var bookingDTOs = _mapper.Map<List<BookingDTO>>(bookings);

                response.Success = true;
                response.Message = "Bookings retrieved successfully.";
                response.Data = new
                {
                    Bookings = bookings,
                    TotalRecords = totalRecords,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving bookings.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Lấy chi tiết booking theo ID (kiểm tra quyền truy cập)
        /// </summary>
        public async Task<BaseResponse> GetBookingDetailsAsync(int bookingId, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                // Kiểm tra booking tồn tại và load cả thông tin liên quan
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Include(b => b.DecorService)
                    .Include(b => b.Address)
                    .Include(b => b.Account)
                    .Include(b => b.BookingDetails)
                    .Include(b => b.Trackings)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                // Kiểm tra quyền truy cập: 
                // - Nếu là customer, chỉ được xem booking của chính mình
                // - Nếu là provider, chỉ được xem booking thuộc dịch vụ của mình
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    response.Message = "Account not found.";
                    return response;
                }

                if (account.IsProvider == true)
                {
                    if (booking.DecorService.AccountId != accountId)
                    {
                        response.Message = "You don't have permission to view this booking.";
                        return response;
                    }
                }
                else
                {
                    if (booking.AccountId != accountId)
                    {
                        response.Message = "You don't have permission to view this booking.";
                        return response;
                    }
                }

                // Trả về thông tin chi tiết booking
                response.Success = true;
                response.Message = "Booking details retrieved successfully.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving booking details.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Thêm chi tiết báo giá vào booking
        /// </summary>
        public async Task<BaseResponse> AddBookingDetailAsync(int bookingId, BookingDetailRequest request)
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

                // Kiểm tra trạng thái booking có phù hợp để thêm chi tiết báo giá không
                if (booking.Status != Booking.BookingStatus.Survey && booking.Status != Booking.BookingStatus.Confirm)
                {
                    response.Message = "Can only add booking details during Survey or Confirm stage.";
                    return response;
                }

                // Tạo chi tiết báo giá mới
                var bookingDetail = new BookingDetail
                {
                    BookingId = bookingId,
                    ServiceItem = request.ServiceItem,
                    Cost = request.Cost,
                    EstimatedCompletion = request.EstimatedCompletion,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _unitOfWork.BookingDetailRepository.InsertAsync(bookingDetail);

                // Cập nhật tổng giá của booking
                var allDetails = await _unitOfWork.BookingDetailRepository.Queryable()
                    .Where(d => d.BookingId == bookingId)
                    .SumAsync(d => (double)d.Cost);
                booking.TotalPrice = allDetails;
                _unitOfWork.BookingRepository.Update(booking);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking detail added successfully.";
                response.Data = bookingDetail;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error adding booking detail.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        // <summary>
        /// Thêm tracking (cập nhật tiến độ thi công) vào booking
        /// </summary>
        public async Task<BaseResponse> AddTrackingAsync(int bookingId, TrackingRequest request)
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

                // Kiểm tra trạng thái booking có phù hợp để thêm tracking không
                if (booking.Status != Booking.BookingStatus.Progressing)
                {
                    response.Message = "Can only add tracking during Progressing stage.";
                    return response;
                }

                // Tạo tracking mới
                var tracking = new Tracking
                {
                    BookingId = bookingId,
                    Stage = request.Stage,
                    PlannedDate = request.PlannedDate,
                    ActualDate = request.ActualDate,
                    ImageUrls = request.ImageUrls,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _unitOfWork.TrackingRepository.InsertAsync(tracking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Tracking added successfully.";
                response.Data = tracking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error adding tracking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Tạo booking mới với thông tin dịch vụ, địa chỉ… của khách hàng.
        /// Kiểm tra: 
        /// - Account tồn tại và không phải Provider.
        /// - Dịch vụ (DecorService) tồn tại và không do chính chủ đặt.
        /// - Địa chỉ (Address) tồn tại.
        /// Trạng thái ban đầu: Pending.
        /// </summary>
        public async Task<BaseResponse> CreateBookingAsync(CreateBookingRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                // Kiểm tra tài khoản: Chỉ khách hàng (không phải Provider) mới được đặt dịch vụ
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    response.Message = "Account not found.";
                    return response;
                }
                if (account.IsProvider == true)
                {
                    response.Message = "Providers cannot book services.";
                    return response;
                }

                // Kiểm tra DecorService: tồn tại và không phải của chính người đặt
                var decorService = await _unitOfWork.DecorServiceRepository.GetByIdAsync(request.DecorServiceId);
                if (decorService == null)
                {
                    response.Message = "DecorService not found.";
                    return response;
                }
                if (decorService.AccountId == accountId)
                {
                    response.Message = "Service creator cannot book their own service.";
                    return response;
                }

                // Kiểm tra địa chỉ
                var address = await _unitOfWork.AddressRepository.GetByIdAsync(request.AddressId);
                if (address == null)
                {
                    response.Message = "Address not found.";
                    return response;
                }

                // Khởi tạo booking với trạng thái Pending, tổng tiền 0 (sẽ cập nhật sau khi có báo giá)
                var booking = new Booking
                {
                    BookingCode = GenerateBookingCode(),
                    TotalPrice = 0.0,
                    CreateAt = DateTime.Now,
                    Status = Booking.BookingStatus.Pending,
                    AccountId = accountId,
                    DecorServiceId = request.DecorServiceId,
                    AddressId = request.AddressId
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
                response.Message = "Error creating booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Chuyển booking từ trạng thái Pending sang Survey.
        /// (Provider xác nhận booking và lên lịch khảo sát).
        /// </summary>
        public async Task<BaseResponse> SurveyBookingAsync(int bookingId)
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
                    response.Message = "Only Pending bookings can be moved to Survey state.";
                    return response;
                }
                booking.Status = Booking.BookingStatus.Survey;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking status updated to Survey.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update booking to Survey state.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Khi khách hàng đã khảo sát và đọc điều khoản hợp đồng, họ xác nhận chốt hợp đồng.
        /// Chuyển booking từ Survey sang Confirm và lưu số tiền đặt cọc.
        /// </summary>
        /// <summary>
        /// Khi khách hàng xác nhận booking, tiến hành đặt cọc.
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
                if (booking.Status != Booking.BookingStatus.Survey)
                {
                    response.Message = "Only bookings in Survey state can be confirmed.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.Confirm;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking confirmed. Customer can now proceed with deposit.";
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

        /// <summary>
        /// Chuyển trạng thái booking từ DepositPaid sang Preparing (Chuẩn bị nguyên liệu).
        /// </summary>
        public async Task<BaseResponse> MarkPreparingAsync(int bookingId)
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
                if (booking.Status != Booking.BookingStatus.DepositPaid)
                {
                    response.Message = "Booking must be in DepositPaid state to transition to Preparing.";
                    return response;
                }
                booking.Status = Booking.BookingStatus.Preparing;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking updated to Preparing.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update booking to Preparing.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Chuyển trạng thái booking từ Preparing sang InTransit (Nguyên liệu được chuyển đến khách hàng).
        /// </summary>
        public async Task<BaseResponse> MarkInTransitAsync(int bookingId)
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
                if (booking.Status != Booking.BookingStatus.Preparing)
                {
                    response.Message = "Booking must be in Preparing state to transition to InTransit.";
                    return response;
                }
                booking.Status = Booking.BookingStatus.InTransit;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking updated to InTransit.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update booking to InTransit.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Chuyển trạng thái booking từ InTransit sang Progressing (Thi công đang diễn ra).
        /// </summary>
        public async Task<BaseResponse> MarkProgressingAsync(int bookingId)
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
                if (booking.Status != Booking.BookingStatus.InTransit)
                {
                    response.Message = "Booking must be in InTransit state to transition to Progressing.";
                    return response;
                }
                booking.Status = Booking.BookingStatus.Progressing;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking updated to Progressing.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update booking to Progressing.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }      

        /// <summary>
        /// Hoàn thành booking: chuyển trạng thái từ ConstructionPayment sang Completed.
        /// </summary>
        public async Task<BaseResponse> CompleteBookingAsync(int bookingId)
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
                if (booking.Status != Booking.BookingStatus.ConstructionPayment)
                {
                    response.Message = "Booking must be in ConstructionPayment state to be completed.";
                    return response;
                }
                booking.Status = Booking.BookingStatus.Completed;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking completed successfully.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to complete booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Hủy booking ở bất kỳ giai đoạn nào.
        /// </summary>
        public async Task<BaseResponse> CancelBookingAsync(int bookingId)
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
                booking.Status = Booking.BookingStatus.Cancelled;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking cancelled successfully.";
                response.Data = booking;
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
        /// Đặt cọc - Chuyển toàn bộ tiền cọc vào ví Admin
        /// </summary>
        public async Task<BaseResponse> DepositForBookingAsync(int bookingId)
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

                // Lấy tổng giá trị báo giá từ BookingDetail
                var bookingDetails = await _unitOfWork.BookingDetailRepository.Queryable()
                    .Where(bd => bd.BookingId == bookingId)
                    .ToListAsync();

                if (!bookingDetails.Any())
                {
                    response.Message = "No booking details found. Cannot process deposit.";
                    return response;
                }

                // Nếu hệ thống không có DepositPercentage, dùng mặc định 30%
                var depositPercentage = 0.2m; // 30% giá trị báo giá
                var totalBookingAmount = bookingDetails.Sum(bd => (decimal)bd.Cost);
                var depositAmount = totalBookingAmount * depositPercentage;

                var adminAccount = await _unitOfWork.AccountRepository.Queryable()
                    .Include(a => a.Role)
                    .Where(a => a.Role.RoleName == "Admin")
                    .FirstOrDefaultAsync();

                if (adminAccount == null)
                {
                    response.Message = "Admin account not found.";
                    return response;
                }

                // Gọi PaymentService để trừ tiền từ ví Customer, chuyển vào Admin
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
                response.Message = $"Deposit successful. {depositAmount} transferred to Admin.";
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

        /// <summary>
        /// Thanh toán thi công - Chuyển toàn bộ tiền vào ví Admin
        /// </summary>
        public async Task<BaseResponse> PayForConstructionAsync(int bookingId)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Include(b => b.DecorService)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

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

                var adminAccount = await _unitOfWork.AccountRepository.Queryable()
                    .Include(a => a.Role)
                    .Where(a => a.Role.RoleName == "Admin")
                    .FirstOrDefaultAsync();

                if (adminAccount == null)
                {
                    response.Message = "Admin account not found.";
                    return response;
                }

                // Lấy tổng chi phí từ BookingDetail
                var totalCost = await _unitOfWork.BookingDetailRepository.Queryable()
                    .Where(bd => bd.BookingId == bookingId)
                    .SumAsync(bd => (decimal)bd.Cost);

                // Tính số tiền cần thanh toán (chỉ còn phần còn lại sau khi trừ cọc)
                var remainingAmount = totalCost - booking.DepositAmount;

                if (remainingAmount <= 0)
                {
                    response.Message = "No remaining balance to pay.";
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
                response.Message = $"Construction payment successful. {remainingAmount} transferred to Admin.";
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
