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
using BusinessLogicLayer.ModelResponse.Pagination;
using System.Linq.Expressions;
using System.Drawing;
using static DataAccessObject.Models.Booking;
using Nest;

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

        public async Task<BaseResponse<List<BookingResponse>>> GetPendingCancellationBookingsForProviderAsync(int providerId)
        {
            var response = new BaseResponse<List<BookingResponse>>();
            try
            {
                var bookings = await _unitOfWork.BookingRepository.Queryable()
                    .Where(b => b.Status == BookingStatus.PendingCancellation && b.DecorService.AccountId == providerId)
                    .Include(b => b.DecorService)
                    .Include(b => b.Address)
                    .Include(b => b.CancelType) // Lấy thông tin loại hủy
                    .ToListAsync();

                var result = bookings.Select(booking => new BookingResponse
                {
                    BookingId = booking.Id,
                    BookingCode = booking.BookingCode,
                    TotalPrice = booking.TotalPrice,
                    Status = (int)booking.Status,
                    Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",
                    CreatedAt = booking.CreateAt,

                    DecorService = new DecorServiceDTO
                    {
                        Id = booking.DecorService.Id,
                        Style = booking.DecorService.Style,
                        BasePrice = booking.DecorService.BasePrice
                    },

                    Provider = new ProviderResponse
                    {
                        Id = booking.DecorService.Account.Id,
                        BusinessName = booking.DecorService.Account.BusinessName
                    },

                    CancelType = booking.CancelType.Type,
                    CancelReason = booking.CancelReason,
                }).ToList();

                response.Success = true;
                response.Data = result;
                response.Message = "Pending cancellation bookings for provider retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving pending cancellation bookings.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<BookingResponse>>> GetPaginatedBookingsForCustomerAsync(BookingFilterRequest request, int accountId)
        {
            var response = new BaseResponse<PageResult<BookingResponse>>();
            try
            {
                // 🔹 Filter Condition
                Expression<Func<Booking, bool>> filter = booking =>
                    booking.AccountId == accountId &&
                    (string.IsNullOrEmpty(request.Status) || booking.Status.ToString().Contains(request.Status)) &&
                    (!request.DecorServiceId.HasValue || booking.DecorServiceId == request.DecorServiceId.Value);

                // 🔹 Sorting Condition
                Expression<Func<Booking, object>> orderByExpression = request.SortBy switch
                {
                    "BookingCode" => booking => booking.BookingCode,
                    "Status" => booking => booking.Status,
                    _ => booking => booking.CreateAt // Mặc định: Booking mới nhất
                };

                // 🔹 Includes (Lấy thêm thông tin)
                Func<IQueryable<Booking>, IQueryable<Booking>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(b => b.DecorService)
                        .ThenInclude(ds => ds.DecorImages) // Hình ảnh
                    .Include(b => b.DecorService.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season) // Season
                    .Include(b => b.DecorService.Account) // Provider
                    .Include(b => b.BookingDetails) // Booking details
                    .Include(b => b.Address);

                // 🔹 Get paginated data & filter
                (IEnumerable<Booking> bookings, int totalCount) = await _unitOfWork.BookingRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                // 🔹 Convert to DTO
                var bookingResponses = bookings.Select(booking => new BookingResponse
                {
                    BookingId = booking.Id,
                    BookingCode = booking.BookingCode,
                    TotalPrice = booking.TotalPrice,
                    Status = (int)booking.Status,                  
                    Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",
                    CreatedAt = booking.CreateAt,

                    // ⭐ Thông tin DecorService
                    DecorService = new DecorServiceDTO
                    {
                        Id = booking.DecorService.Id,
                        Style = booking.DecorService.Style,
                        BasePrice = booking.DecorService.BasePrice,
                        Description = booking.DecorService.Description,
                        StartDate = booking.DecorService.StartDate,

                        // ⭐ Hình ảnh decor
                        Images = booking.DecorService.DecorImages.Select(di => new DecorImageResponse
                        {
                            Id = di.Id,
                            ImageURL = di.ImageURL
                        }).ToList(),

                        // ⭐ Danh sách mùa decor
                        Seasons = booking.DecorService.DecorServiceSeasons.Select(ds => new SeasonResponse
                        {
                            Id = ds.Season.Id,
                            SeasonName = ds.Season.SeasonName
                        }).ToList()
                    },

                    // ⭐ Thông tin Provider
                    Provider = new ProviderResponse
                    {
                        Id = booking.DecorService.Account.Id,
                        BusinessName = booking.DecorService.Account.BusinessName,
                        Avatar = booking.DecorService.Account.Avatar,
                        Phone = booking.DecorService.Account.Phone,
                        Slug = booking.DecorService.Account.Slug
                    },

                    // ⭐ Booking Details
                    //BookingDetails = booking.BookingDetails.Select(bd => new BookingDetailResponse
                    //{
                    //    Id = bd.Id,
                    //    ServiceItem = bd.ServiceItem,
                    //    Cost = bd.Cost,
                    //    EstimatedCompletion = bd.EstimatedCompletion
                    //}).ToList()

                    // 🆕 **Gộp BookingDetails vào Booking**
                    ServiceItems = booking.BookingDetails.Any()
                        ? string.Join(", ", booking.BookingDetails.Select(bd => bd.ServiceItem))
                        : "No Service Items",

                    Cost = booking.BookingDetails.Any()
                        ? booking.BookingDetails.Sum(bd => bd.Cost)
                        : 0,

                    EstimatedCompletion = booking.BookingDetails.Any()
                        ? booking.BookingDetails.Max(bd => bd.EstimatedCompletion)
                        : null
                }).ToList();

                // 🔹 Return result
                response.Success = true;
                response.Data = new PageResult<BookingResponse>
                {
                    Data = bookingResponses,
                    TotalCount = totalCount
                };
                response.Message = "Bookings retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving bookings.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<BookingResponseForProvider>>> GetPaginatedBookingsForProviderAsync(BookingFilterRequest request, int providerId)
        {
            var response = new BaseResponse<PageResult<BookingResponseForProvider>>();
            try
            {
                // 🔹 Filter: Lấy booking mà dịch vụ được tạo bởi provider (DecorService.AccountId == providerId)
                Expression<Func<Booking, bool>> filter = booking =>
                    booking.DecorService.AccountId == providerId &&
                    (string.IsNullOrEmpty(request.Status) || booking.Status.ToString().Contains(request.Status)) &&
                    (!request.DecorServiceId.HasValue || booking.DecorServiceId == request.DecorServiceId.Value);

                // 🔹 Sorting: Mặc định sắp xếp theo CreateAt giảm dần (Booking mới nhất trước)
                Expression<Func<Booking, object>> orderByExpression = request.SortBy switch
                {
                    "BookingCode" => booking => booking.BookingCode,
                    "Status" => booking => booking.Status,
                    _ => booking => booking.CreateAt
                };

                // 🔹 Include: Sử dụng custom query để include các thông tin cần thiết
                Func<IQueryable<Booking>, IQueryable<Booking>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(b => b.DecorService)
                        .ThenInclude(ds => ds.DecorImages) // Hình ảnh decor
                    .Include(b => b.DecorService.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season) // Season
                    .Include(b => b.Account) // Customer (khách hàng đặt booking)
                    .Include(b => b.BookingDetails) // Booking details
                    .Include(b => b.Address);

                // 🔹 Get paginated data & filter
                (IEnumerable<Booking> bookings, int totalCount) = await _unitOfWork.BookingRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                // 🔹 Map dữ liệu thành DTO theo góc nhìn của Provider
                var bookingResponses = bookings.Select(booking => new BookingResponseForProvider
                {
                    BookingId = booking.Id,
                    BookingCode = booking.BookingCode,
                    TotalPrice = booking.TotalPrice,
                    Status = (int)booking.Status,                
                    Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",
                    CreatedAt = booking.CreateAt,


                    // Thông tin DecorService (không thay đổi)
                    DecorService = new DecorServiceDTO
                    {
                        Id = booking.DecorService.Id,
                        Style = booking.DecorService.Style,
                        BasePrice = booking.DecorService.BasePrice,
                        Description = booking.DecorService.Description,
                        StartDate = booking.DecorService.StartDate,
                        //ImageUrls = booking.DecorService.DecorImages?.Select(di => di.ImageURL).ToList() ?? new List<string>(),
                        Images = booking.DecorService.DecorImages?.Select(di => new DecorImageResponse
                        {
                            Id = di.Id,
                            ImageURL = di.ImageURL
                        }).ToList() ?? new List<DecorImageResponse>(),
                        Seasons = booking.DecorService.DecorServiceSeasons?
                            .Where(ds => ds.Season != null)
                            .Select(ds => new SeasonResponse
                            {
                                Id = ds.Season.Id,
                                SeasonName = ds.Season.SeasonName
                            }).ToList() ?? new List<SeasonResponse>()
                    },

                    // Thông tin Customer (khách hàng đặt booking)
                    Customer = new CustomerResponse
                    {
                        Id = booking.Account.Id,
                        FullName = $"{booking.Account.FirstName} {booking.Account.LastName}",
                        Email = booking.Account.Email,
                        Phone = booking.Account.Phone,
                        Slug = booking.Account.Slug,
                        Avatar = booking.Account.Avatar
                    },

                    // Chi tiết Booking
                    //BookingDetails = booking.BookingDetails?.Select(bd => new BookingDetailResponse
                    //{
                    //    Id = bd.Id,
                    //    ServiceItem = bd.ServiceItem,
                    //    Cost = bd.Cost,
                    //    EstimatedCompletion = bd.EstimatedCompletion
                    //}).ToList() ?? new List<BookingDetailResponse>()
                    ServiceItems = booking.BookingDetails.Any()
                        ? string.Join(", ", booking.BookingDetails.Select(bd => bd.ServiceItem))
                        : "No Service Items",

                    Cost = booking.BookingDetails.Any()
                        ? booking.BookingDetails.Sum(bd => bd.Cost)
                        : 0,

                    EstimatedCompletion = booking.BookingDetails.Any()
                        ? booking.BookingDetails.Max(bd => bd.EstimatedCompletion)
                        : null
                }).ToList();

                response.Success = true;
                response.Data = new PageResult<BookingResponseForProvider>
                {
                    Data = bookingResponses,
                    TotalCount = totalCount
                };
                response.Message = "Bookings retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving bookings.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<List<BookingResponse>>> GetBookingsByUserAsync(int accountId)
        {
            var response = new BaseResponse<List<BookingResponse>>();
            try
            {
                var bookings = await _unitOfWork.BookingRepository.Queryable()
                    .Where(b => b.AccountId == accountId)
                    .Include(b => b.DecorService)
                        .ThenInclude(ds => ds.Account) // ⭐ Join Provider
                    .ToListAsync();

                var result = bookings.Select(booking => new BookingResponse
                {
                    BookingId = booking.Id,
                    BookingCode = booking.BookingCode,
                    TotalPrice = booking.TotalPrice,
                    Status = (int)booking.Status,
                    CreatedAt = booking.CreateAt,

                    // ⭐ Thông tin DecorService
                    DecorService = new DecorServiceDTO
                    {
                        Id = booking.DecorService.Id,
                        Style = booking.DecorService.Style,
                        BasePrice = booking.DecorService.BasePrice,
                        Description = booking.DecorService.Description,
                        StartDate = booking.DecorService.StartDate 
                    },

                    // ⭐ Thêm thông tin Provider
                    Provider = new ProviderResponse
                    {
                        Id = booking.DecorService.Account.Id,
                        BusinessName = booking.DecorService.Account.BusinessName,
                        Avatar = booking.DecorService.Account.Avatar,
                    }
                }).ToList();

                response.Success = true;
                response.Data = result;
                response.Message = "Bookings retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving bookings.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<BookingDetailForProviderResponse>> GetBookingDetailForProviderAsync(string bookingCode, int accountId)
        {
            var response = new BaseResponse<BookingDetailForProviderResponse>();
            try
            {
                // Get booking with all related data
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Include(b => b.DecorService)
                        .ThenInclude(ds => ds.Account)
                    .Include(b => b.Account) // Customer info
                    .Include(b => b.TimeSlots) // Survey times
                    .Include(b => b.Address) // Address
                    .Include(b => b.BookingDetails) // Booking details
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    response.Success = false;
                    response.Message = "Booking not found";
                    return response;
                }

                // Verify access rights
                if (booking.DecorService.AccountId != accountId ||
                    !booking.DecorService.Account.IsProvider.GetValueOrDefault())
                {
                    response.Success = false;
                    response.Message = "Access denied. Only the service provider can view booking details";
                    return response;
                }

                // Get the first booking detail (assuming there's at least one)
                var bookingDetail = booking.BookingDetails.FirstOrDefault();
                if (bookingDetail == null)
                {
                    response.Success = false;
                    response.Message = "No booking details found";
                    return response;
                }

                // Map to response object
                response.Data = new BookingDetailForProviderResponse
                {
                    BookingDetails = booking.BookingDetails.Select(bd => new BookingDetailResponse
                    {
                        Id = bd.Id,
                        ServiceItem = bd.ServiceItem,
                        Cost = bd.Cost,
                        EstimatedCompletion = bd.EstimatedCompletion,
                    }).ToList(),
                    SurveyDate = booking.TimeSlots.FirstOrDefault()?.SurveyDate,
                    Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",
                    Customer = new CustomerResponse
                    {
                        Id = booking.Account.Id,
                        FullName = $"{booking.Account.FirstName} {booking.Account.LastName}".Trim(),
                        Email = booking.Account.Email,
                        Phone = booking.Account.Phone,
                        Avatar = booking.Account.Avatar
                    }
                };

                response.Success = true;
                response.Message = "Booking details retrieved successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving booking details";
                response.Errors.Add(ex.Message);
                
            }
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
                    response.Message = "Decorate service not exists";
                    return response;
                }

                // 🔹 Kiểm tra nếu service đã không còn available
                if (decorService.Status == DecorService.DecorServiceStatus.NotAvailable)
                {
                    response.Message = "This service is currently not available for booking.";
                    return response;
                }

                /////------------------------------------------------------------------------------------------
                // 🔹 Kiểm tra provider có đang bận không
                var provider = await _unitOfWork.AccountRepository.Queryable()
                    .FirstOrDefaultAsync(acc => acc.Id == decorService.AccountId);

                if (provider == null)
                {
                    response.Message = "Service provider not found.";
                    return response;
                }

                if (provider.ProviderStatus == Account.AccountStatus.Busy)
                {
                    response.Message = "The service provider is currently busy. Please try again later.";
                    return response;
                }
                /////------------------------------------------------------------------------------------------

                // 🔹 Kiểm tra nếu người tạo booking cũng là chủ của dịch vụ
                if (decorService.AccountId == accountId)
                {
                    response.Message = "You cannot create a booking for your own service.";
                    return response;
                }

                /////------------------------------------------------------------------------------------------
                // 🔹 Kiểm tra nếu đã có booking khác đang được xử lý (không phải canceled hoặc rejected)
                // 🔹 Kiểm tra nếu đã có booking khác đang diễn ra
                bool hasOngoingBooking = await _unitOfWork.BookingRepository.Queryable()
                    .AnyAsync(b => b.AccountId == accountId &&
                                   b.Status != Booking.BookingStatus.Canceled &&
                                   b.Status != Booking.BookingStatus.Rejected &&
                                   b.Status != Booking.BookingStatus.Completed);

                if (hasOngoingBooking)
                {
                    response.Message = "You cannot create a new booking until your current booking is completed or canceled or rejected.";
                    return response;
                }
                /////------------------------------------------------------------------------------------------

                // 🔹 Kiểm tra nếu ngày khảo sát có hợp lệ với ngày bắt đầu dịch vụ
                if (request.SurveyDate < decorService.StartDate)
                {
                    response.Message = "This service is only available starting from " + decorService.StartDate.ToString("dd-MM-yyyy");
                    return response;
                }

                // 🔹 Kiểm tra ngày khảo sát hợp lệ
                if (request.SurveyDate < DateTime.Today)
                {
                    response.Message = "Survey date must be in the future.";
                    return response;
                }

                var booking = new Booking
                {
                    BookingCode = GenerateBookingCode(),
                    AccountId = accountId,
                    AddressId = request.AddressId,
                    DecorServiceId = request.DecorServiceId,
                    Status = Booking.BookingStatus.Pending,
                    CreateAt = DateTime.Now
                };

                await _unitOfWork.BookingRepository.InsertAsync(booking);
                await _unitOfWork.CommitAsync();

                // 🔹 Lưu thông tin khảo sát vào TimeSlot
                var timeSlot = new TimeSlot
                {
                    BookingId = booking.Id,
                    SurveyDate = request.SurveyDate,
                };

                await _unitOfWork.TimeSlotRepository.InsertAsync(timeSlot);
                decorService.Status = DecorService.DecorServiceStatus.NotAvailable;
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

        public async Task<BaseResponse<bool>> ChangeBookingStatusAsync(string bookingCode)
        {
            var response = new BaseResponse<bool>();
            
            var booking = await _unitOfWork.BookingRepository.Queryable()
                .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);
            if (booking == null)
            {
                response.Message = "Booking not found.";
                return response;
            }

            // 🔹 Xác định trạng thái tiếp theo
            Booking.BookingStatus? newStatus = booking.Status switch
            {
                Booking.BookingStatus.Pending => Booking.BookingStatus.Planning,
                Booking.BookingStatus.Planning => Booking.BookingStatus.Confirm,
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
                case Booking.BookingStatus.Planning:
                    break;
                case Booking.BookingStatus.Confirm:
                    // 🔹 Khi booking chuyển sang Confirm, tạo BookingDetail từ Quotation
                    var quotation = await _unitOfWork.QuotationRepository.Queryable()
                        .FirstOrDefaultAsync(q => q.BookingId == booking.Id);

                    if (quotation == null)
                    {
                        response.Message = "Quotation not found. Please create a quotation first.";
                        return response;
                    }

                    // ✅ Cập nhật `TotalPrice` trong `Booking`
                    booking.TotalPrice = quotation.MaterialCost + quotation.ConstructionCost;

                    // Kiểm tra nếu BookingDetail đã tồn tại
                    var existingDetails = await _unitOfWork.BookingDetailRepository.Queryable()
                        .Where(bd => bd.BookingId == booking.Id)
                        .ToListAsync();

                    if (!existingDetails.Any())
                    {
                        var bookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { BookingId = booking.Id, ServiceItem = "Chi Phí Nguyên liệu", Cost = quotation.MaterialCost },
                    new BookingDetail { BookingId = booking.Id, ServiceItem = "Chi Phí Thi công", Cost = quotation.ConstructionCost }
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
                    break;

                case Booking.BookingStatus.InTransit:
                    // ✅ Khi chuyển sang `InTransit`, cập nhật EstimatedCompletion cho `Chi Phí Nguyên liệu`
                    var materialDetail = await _unitOfWork.BookingDetailRepository.Queryable()
                        .FirstOrDefaultAsync(bd => bd.BookingId == booking.Id && bd.ServiceItem == "Chi Phí Nguyên liệu");

                    if (materialDetail != null)
                    {
                        materialDetail.EstimatedCompletion = DateTime.Now;
                        _unitOfWork.BookingDetailRepository.Update(materialDetail);
                    }
                    break;

                case Booking.BookingStatus.Progressing:
                    // ✅ Khi vào Progressing, tạo Tracking để lưu ảnh thi công
                    var tracking = new Tracking
                    {
                        BookingId = booking.Id,
                        Status = Booking.BookingStatus.Progressing,
                        Note = "Construction phase started.",
                        CreatedAt = DateTime.Now
                    };

                    await _unitOfWork.TrackingRepository.InsertAsync(tracking);
                    await _unitOfWork.CommitAsync();
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
                    // ✅ Khi chuyển sang `Completed`, cập nhật EstimatedCompletion cho `Chi Phí Thi công`
                    var laborDetail = await _unitOfWork.BookingDetailRepository.Queryable()
                        .FirstOrDefaultAsync(bd => bd.BookingId == booking.Id && bd.ServiceItem == "Chi Phí Thi công");

                    if (laborDetail != null)
                    {
                        laborDetail.EstimatedCompletion = DateTime.Now;
                        _unitOfWork.BookingDetailRepository.Update(laborDetail);
                    }

                    ///---------------------------------------------------------------------------------------
                    // ✅ Lấy Provider từ `DecorService`
                    var provider = await _unitOfWork.AccountRepository.Queryable()
                        .FirstOrDefaultAsync(a => a.Id == _unitOfWork.DecorServiceRepository.Queryable()
                            .Where(ds => ds.Id == booking.DecorServiceId)
                            .Select(ds => ds.AccountId)
                            .FirstOrDefault());

                    if (provider != null)
                    {
                        // ✅ Cập nhật trạng thái Provider thành `Available`
                        provider.ProviderStatus = Account.AccountStatus.Available;
                        _unitOfWork.AccountRepository.Update(provider);
                    }
                    ///---------------------------------------------------------------------------------------
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

        public async Task<BaseResponse> RequestCancellationAsync(string bookingCode, int accountId, int cancelTypeId, string? cancelReason)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                        .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    response.Success = false;
                    response.Message = "Booking not found.";
                    return response;
                }

                // Kiểm tra quyền
                if (booking.AccountId != accountId)
                {
                    response.Success = false;
                    response.Message = "You are not authorized to request cancellation.";
                    return response;
                }

                // Kiểm tra trạng thái hợp lệ để hủy
                if (booking.Status != BookingStatus.Pending && 
                    booking.Status != BookingStatus.Planning)
                {
                    response.Success = false;
                    response.Message = "You can only request cancellation in Pending, Accepted, or Survey status.";
                    return response;
                }

                if (cancelTypeId == 7 && string.IsNullOrEmpty(cancelReason))
                {
                    response.Success = false;
                    response.Message = "Please provide a reason for cancellation when selecting 'Other'.";
                    return response;
                }

                // Cập nhật trạng thái yêu cầu hủy
                booking.Status = BookingStatus.PendingCancellation;
                booking.CancelTypeId = cancelTypeId;
                booking.CancelReason = cancelReason;

                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Cancellation request submitted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error requesting cancellation.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> ApproveCancellationAsync(string bookingCode, int providerId)
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

                var service = await _unitOfWork.DecorServiceRepository.Queryable()
                    .FirstOrDefaultAsync(ds => ds.Id == booking.DecorServiceId);

                if (service == null || service.AccountId != providerId)
                {
                    response.Message = "You do not have permission to approve this cancellation.";
                    return response;
                }

                if (booking.Status != Booking.BookingStatus.PendingCancellation)
                {
                    response.Message = "No pending cancellation request to approve.";
                    return response;
                }

                booking.Status = BookingStatus.Canceled;
                _unitOfWork.BookingRepository.Update(booking);
                service.Status = DecorService.DecorServiceStatus.Available;
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking cancellation approved.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to approve cancellation.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> RevokeCancellationRequestAsync(string bookingCode, int accountId)
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

                if (booking.AccountId != accountId)
                {
                    response.Message = "You do not have permission to revoke this cancellation request.";
                    return response;
                }

                if (booking.Status != Booking.BookingStatus.PendingCancellation)
                {
                    response.Message = "There is no pending cancellation request to revoke.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.Pending;
                booking.CancelReason = null;

                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Cancellation request has been revoked.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to revoke cancellation request.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> RejectBookingAsync(string bookingCode, int accountId, string reason)
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

                var service = await _unitOfWork.DecorServiceRepository.Queryable()
                    .FirstOrDefaultAsync(ds => ds.Id == booking.DecorServiceId);

                if (service == null || service.AccountId != accountId)
                {
                    response.Message = "You do not have permission to reject this booking.";
                    return response;
                }

                if (booking.Status == Booking.BookingStatus.Canceled || booking.Status == Booking.BookingStatus.Rejected)
                {
                    response.Message = "This booking has already been canceled or rejected.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.Rejected;
                booking.RejectReason = reason; // Lưu lý do reject
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Booking has been rejected successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to reject booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> ProcessDepositAsync(string bookingCode)
        {
            var response = new BaseResponse();

            try
            {
                // 🔹 Lấy thông tin booking
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

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

                // 🔹 Lấy báo giá của booking
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .FirstOrDefaultAsync(q => q.BookingId == booking.Id);

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                // 🔹 Tính tổng chi phí booking
                var totalAmount = quotation.MaterialCost + quotation.ConstructionCost;

                // 🔹 Lấy phần trăm đặt cọc từ báo giá, tối đa 20%
                var depositRate = Math.Min(quotation.DepositPercentage, 20m) / 100m;
                var depositAmount = totalAmount * depositRate;

                // 🔹 Lấy Provider từ `DecorService`
                var provider = await _unitOfWork.AccountRepository.Queryable()
                    .FirstOrDefaultAsync(a => a.Id == _unitOfWork.DecorServiceRepository.Queryable()
                        .Where(ds => ds.Id == booking.DecorServiceId)
                        .Select(ds => ds.AccountId)
                        .FirstOrDefault());

                if (provider == null)
                {
                    response.Message = "Provider not found.";
                    return response;
                }

                // 🔹 Thực hiện thanh toán, chuyển tiền cho Provider
                bool paymentSuccess = await _paymentService.Deposit(booking.AccountId, provider.Id, depositAmount, booking.Id);
                if (!paymentSuccess)
                {
                    response.Message = "Deposit payment failed.";
                    return response;
                }

                // 🔹 Cập nhật trạng thái booking
                booking.Status = Booking.BookingStatus.DepositPaid;
                booking.DepositAmount = depositAmount;
                _unitOfWork.BookingRepository.Update(booking);

                // 🔹 Chuyển trạng thái Provider sang "Busy"
                provider.ProviderStatus = Account.AccountStatus.Busy;
                _unitOfWork.AccountRepository.Update(provider);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = $"Deposit successful: {depositAmount} transferred to provider.";
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

        public async Task<BaseResponse> ProcessFinalPaymentAsync(string bookingCode)
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
                if (booking.Status != Booking.BookingStatus.Progressing)
                {
                    response.Message = "Only progressing bookings can be paid for.";
                    return response;
                }

                var remainingAmount = booking.TotalPrice - booking.DepositAmount;

                if (remainingAmount <= 0)
                {
                    response.Message = "No remaining amount to be paid.";
                    return response;
                }

                // Lấy Provider từ DecorService
                var providerId = await _unitOfWork.DecorServiceRepository.Queryable()
                    .Where(ds => ds.Id == booking.DecorServiceId)
                    .Select(ds => ds.AccountId)
                    .FirstOrDefaultAsync();

                if (providerId == 0)
                {
                    response.Message = "Provider not found.";
                    return response;
                }

                // Lấy % hoa hồng từ Setting
                var commissionRate = await _unitOfWork.SettingRepository.Queryable()
                    .Select(s => s.Commission)
                    .FirstOrDefaultAsync();

                // Gọi PaymentService.Pay với 5 tham số
                bool paymentSuccess = await _paymentService.FinalPay(
                    booking.AccountId, remainingAmount, providerId, booking.Id, commissionRate);

                if (!paymentSuccess)
                {
                    response.Message = "Construction payment failed.";
                    return response;
                }

                booking.Status = Booking.BookingStatus.ConstructionPayment;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Construction payment successful.";
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
            return "BKG" + DateTime.Now.Ticks;
        }
        #endregion
    }
}
