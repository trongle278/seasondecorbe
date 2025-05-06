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
using static System.Net.WebRequestMethods;
using Quartz.Impl.AdoJobStore.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
//{_clientBaseUrl}/booking/progress/{bookingCode}?is-tracked={booking.IsTracked}&status=9&quotation-code={quotation.QuotationCode}&provider={Uri.EscapeDataString(provider.BusinessName)}&avatar={Uri.EscapeDataString(provider.Avatar ?? "null")}&is-reviewed={booking.IsReviewed}

namespace BusinessLogicLayer.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly string _clientBaseUrl;
        private readonly IZoomService _zoomService;
        private readonly ILogger<BookingService> _logger;

        public BookingService(IUnitOfWork unitOfWork, IPaymentService paymentService, INotificationService notificationService, IConfiguration configuration, IZoomService zoomService, ILogger<BookingService> logger)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _clientBaseUrl = configuration["AppSettings:ClientBaseUrl"];
            _zoomService = zoomService;
            _logger = logger;
        }

        public async Task<BaseResponse<PendingCancelBookingDetailForProviderResponse>> GetPendingCancelBookingDetailByBookingCodeAsync(string bookingCode, int providerId)
        {
            var response = new BaseResponse<PendingCancelBookingDetailForProviderResponse>();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Include(b => b.DecorService)
                    .Include(b => b.Address)
                    .Include(b => b.CancelType)
                    .Include(b => b.Account)
                    .Where(b => b.BookingCode == bookingCode
                                && b.Status == BookingStatus.PendingCancel
                                && b.DecorService.AccountId == providerId)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Success = false;
                    response.Message = "Get pending cancellation booking detail successfully.";
                    return response;
                }

                var result = new PendingCancelBookingDetailForProviderResponse
                {
                    BookingCode = booking.BookingCode,
                    Status = (int)booking.Status,
                    Style = booking.DecorService.Style,
                    CustomerName = $"{booking.Account.FirstName} {booking.Account.LastName}",
                    Email = booking.Account.Email,
                    Phone = booking.Account.Phone,
                    Avatar =booking.Account.Avatar,
                    Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",
                    CreatedAt = booking.CreateAt,

                    CancelTypeId = booking.CancelType.Id,
                    CancelTypeName = booking.CancelType.Type,
                    CancelReason = booking.CancelReason
                }; 

                response.Success = true;
                response.Data = result;
                response.Message = "Pending cancellation booking retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving pending cancellation booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<BookingResponse>>> GetPaginatedBookingsForCustomerAsync(BookingFilterRequest request, int accountId)
        {
            var response = new BaseResponse<PageResult<BookingResponse>>();
            try
            {
                // Filter Condition
                Expression<Func<Booking, bool>> filter = booking =>
                    booking.AccountId == accountId &&
                    ((!request.Status.HasValue || booking.Status == request.Status.Value)) &&
                    (!request.DecorServiceId.HasValue || booking.DecorServiceId == request.DecorServiceId.Value);

                // Sorting Condition
                Expression<Func<Booking, object>> orderByExpression = request.SortBy switch
                {
                    "BookingCode" => booking => booking.BookingCode,
                    "Status" => booking => booking.Status,
                    _ => booking => booking.CreateAt
                };

                // Includes
                Func<IQueryable<Booking>, IQueryable<Booking>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(b => b.DecorService)
                        .ThenInclude(ds => ds.DecorImages)
                    .Include(b => b.DecorService.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season)
                    .Include(b => b.DecorService.Account)
                    .Include(b => b.BookingDetails)
                    .Include(b => b.Address)
                    .Include(b => b.Quotations) // 🔥 Lấy thêm Quotations để check isQuoteExisted
                        .ThenInclude(q => q.Contract); // 🔥 Lấy thêm Contract để check isContractExisted

                (IEnumerable<Booking> bookings, int totalCount) = await _unitOfWork.BookingRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                var bookingResponses = bookings.Select(booking =>
                {
                    // 🔥 Lấy Quotation gần nhất (CreatedAt mới nhất)
                    var latestQuotation = booking.Quotations
                        .OrderByDescending(q => q.CreatedAt)
                        .FirstOrDefault();

                    return new BookingResponse
                    {
                        BookingId = booking.Id,
                        BookingCode = booking.BookingCode,
                        QuotationCode = latestQuotation?.QuotationCode ?? "",
                        TotalPrice = booking.TotalPrice,
                        Status = (int)booking.Status,
                        Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",
                        CancelDisable = booking.CancelDisable,
                        IsCommitDepositPaid = booking.IsCommitDepositPaid,
                        CreatedAt = booking.CreateAt,

                        DecorService = new DecorServiceDTO
                        {
                            Id = booking.DecorService.Id,
                            Style = booking.DecorService.Style,
                            BasePrice = booking.DecorService.BasePrice,
                            Description = booking.DecorService.Description,
                            Status = (int)booking.DecorService.Status,
                            StartDate = booking.DecorService.StartDate,
                            Images = booking.DecorService.DecorImages.Select(di => new DecorImageResponse
                            {
                                Id = di.Id,
                                ImageURL = di.ImageURL
                            }).ToList(),
                            Seasons = booking.DecorService.DecorServiceSeasons.Select(ds => new SeasonResponse
                            {
                                Id = ds.Season.Id,
                                SeasonName = ds.Season.SeasonName
                            }).ToList()
                        },

                        Provider = new ProviderResponse
                        {
                            Id = booking.DecorService.Account.Id,
                            BusinessName = booking.DecorService.Account.BusinessName,
                            Avatar = booking.DecorService.Account.Avatar,
                            Phone = booking.DecorService.Account.Phone,
                            Slug = booking.DecorService.Account.Slug
                        },

                        ServiceItems = booking.BookingDetails.Any()
                            ? string.Join(", ", booking.BookingDetails.Select(bd => bd.ServiceItem))
                            : "No Service Items",

                        Cost = booking.BookingDetails.Any()
                            ? booking.BookingDetails.Sum(bd => bd.Cost)
                            : 0,

                        //EstimatedCompletion = booking.BookingDetails.Any()
                        //    ? booking.BookingDetails.Max(bd => bd.EstimatedCompletion)
                        //    : null,

                        // 🔥 Thêm isQuoteExisted và isContractExisted
                        IsQuoteExisted = latestQuotation?.isQuoteExisted ?? false,
                        IsContractSigned = latestQuotation?.Contract?.isSigned ?? false,
                        IsTracked = booking.IsTracked ?? false,
                        IsReviewed = booking.IsReviewed ?? false,
                    };
                }).ToList();

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
                    ((!request.Status.HasValue || booking.Status == request.Status.Value)) &&
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
                    .Include(b => b.Quotations)
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
                    CancelDisable = booking.CancelDisable,
                    Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",
                    CreatedAt = booking.CreateAt,
                    IsCommitDepositPaid = booking.IsCommitDepositPaid,
                    IsQuoteExisted = booking.Quotations.Any(),
                    IsTracked = booking.IsTracked ?? false,
                    IsReviewed = booking.IsReviewed ?? false,

                    // Thông tin DecorService (không thay đổi)
                    DecorService = new DecorServiceDTO
                    {
                        Id = booking.DecorService.Id,
                        Style = booking.DecorService.Style,
                        BasePrice = booking.DecorService.BasePrice,
                        Description = booking.DecorService.Description,
                        StartDate = booking.DecorService.StartDate,
                        Status = (int)booking.DecorService.Status,                       
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

                    //EstimatedCompletion = booking.BookingDetails.Any()
                    //    ? booking.BookingDetails.Max(bd => bd.EstimatedCompletion)
                    //    : null
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
                        Status = (int)booking.DecorService.Status,
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
                    .AsSplitQuery()
                    .Include(b => b.DecorService)
                        .ThenInclude(ds => ds.Account)
                     .Include(b => b.DecorService)
                        .ThenInclude(ds => ds.DecorImages) // Hình ảnh decor
                    .Include(b => b.DecorService.DecorServiceSeasons)
                        .ThenInclude(dss => dss.Season) // Season
                    .Include(b => b.Account) // Customer info
                    .Include(b => b.TimeSlots) // Survey times
                    .Include(b => b.Address) // Address
                    .Include(b => b.BookingDetails) // Booking details
                    .Where(b => b.BookingCode == bookingCode)
                    .FirstOrDefaultAsync();

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
                //var bookingDetail = booking.BookingDetails.FirstOrDefault();
                //if (bookingDetail == null)
                //{
                //    response.Success = false;
                //    response.Message = "No booking details found";
                //    return response;
                //}

                // Map to response object
                response.Data = new BookingDetailForProviderResponse
                {
                    BookingCode = booking.BookingCode,
                    TotalPrice = booking.TotalPrice,
                    Status = (int)booking.Status,
                    CreatedAt = booking.CreateAt,
                    DepositAmount = booking.DepositAmount,
                    CancelType = booking.CancelType?.Type,
                    CancelReason = booking.CancelReason,
                    RejectReason = booking.RejectReason,

                    BookingDetails = booking.BookingDetails.Select(bd => new BookingDetailResponse
                    {
                        Id = bd.Id,
                        ServiceItem = bd.ServiceItem,
                        Cost = bd.Cost,
                        //EstimatedCompletion = bd.EstimatedCompletion,
                    }).ToList(),
                    SurveyDate = booking.TimeSlots.FirstOrDefault()?.SurveyDate,
                    Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",

                    DecorService = new DecorServiceDTO
                    {
                        Id = booking.DecorService.Id,
                        Style = booking.DecorService.Style,
                        BasePrice = booking.DecorService.BasePrice,
                        Description = booking.DecorService.Description,
                        StartDate = booking.DecorService.StartDate,
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
                // 1. Service Availability Check
                var decorService = await _unitOfWork.DecorServiceRepository.Queryable()
                    .Where(ds => ds.Id == request.DecorServiceId)
                    .FirstOrDefaultAsync();

                if (decorService == null)
                {
                    response.Message = "Decorate service not exists";
                    return response;
                }

                // 🔹 Kiểm tra nếu service đã không còn available
                if (decorService?.Status != DecorService.DecorServiceStatus.Available)
                {
                    response.Message = "This service is currently unavailable for booking";
                    return response;
                }

                /////------------------------------------------------------------------------------------------
                // 🔹 Kiểm tra provider
                var provider = await _unitOfWork.AccountRepository.Queryable()
                    .Where(acc => acc.Id == decorService.AccountId)
                    .FirstOrDefaultAsync();

                if (provider == null)
                {
                    response.Message = "Service provider not found.";
                    return response;
                }

                if (provider.ProviderStatus == Account.AccountStatus.Busy)
                {
                    response.Message = "The provider is currently busy. Yours has been added to the queue!";
                    //return response;
                }

                //🔹 Kiểm tra nếu người tạo booking cũng là chủ của dịch vụ
                if (decorService.AccountId == accountId)
                {
                    response.Message = "You cannot create a booking for your own service.";
                    return response;
                }

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
                /////------------------------------------------------------------------------------------------

                //Address Validation
                var address = await _unitOfWork.AddressRepository.GetByIdAsync(request.AddressId);
                if (address?.AccountId != accountId || address.IsDelete)
                {
                    response.Message = "The selected address is invalid or not registered to your account";
                    return response;
                }

                //Count Valid Addresses
                var validAddresses = await _unitOfWork.AddressRepository.Queryable()
                    .Where(a => a.AccountId == accountId && !a.IsDelete)
                    .CountAsync();

                //Active Booking Check
                var activeBookings = await _unitOfWork.BookingRepository.Queryable()
                    .Where(b => b.AccountId == accountId &&
                              (b.Status == BookingStatus.Pending ||
                               b.Status == BookingStatus.Planning ||
                               b.Status == BookingStatus.Quoting ||
                               b.Status == BookingStatus.Contracting ||
                               b.Status == BookingStatus.Confirm ||
                               b.Status == BookingStatus.DepositPaid ||
                               b.Status == BookingStatus.Preparing ||
                               b.Status == BookingStatus.InTransit ||
                               b.Status == BookingStatus.Progressing ||
                               b.Status == BookingStatus.FinalPaid ||
                               b.Status == BookingStatus.PendingCancel))
                    .ToListAsync();

                // Address Availability Check
                bool isAddressInUse = activeBookings.Any(b => b.AddressId == request.AddressId);
                if (isAddressInUse)
                {
                    response.Message = "This address is currently in use for another booking. Please choose a different one.";
                    return response;
                }

                // 7. Create New Booking
                var booking = new Booking
                {
                    BookingCode = GenerateBookingCode(),
                    AccountId = accountId,
                    AddressId = request.AddressId,
                    DecorServiceId = request.DecorServiceId,
                    Status = BookingStatus.Pending,
                    Note = request.Note,
                    //RequestChangeCount = 0, //số lần đổi yêu cầu
                    //IsAdditionalFeeCharged = false,
                    CommitDepositAmount = 500000,
                    CreateAt = DateTime.Now,
                    IsCommitDepositPaid = false,
                };

                await _unitOfWork.BookingRepository.InsertAsync(booking);
                booking.IsBooked = true;
                booking.CancelDisable = false;
                await _unitOfWork.CommitAsync();

                var timeSlot = new TimeSlot
                {
                    BookingId = booking.Id,
                    SurveyDate = request.SurveyDate,
                };

                await _unitOfWork.TimeSlotRepository.InsertAsync(timeSlot);
                await _unitOfWork.CommitAsync();

                //var zoomMeetingRequest = new ZoomMeetingRequest
                //{
                //    Topic = $"Booking #{booking.BookingCode} - Meeting",
                //    TimeZone = "Asia/Ho_Chi_Minh"
                //};

                //var zoomMeeting = await _zoomService.CreateMeetingAsync(zoomMeetingRequest);


                //booking.ZoomUrl = zoomMeeting.JoinUrl;
                //_unitOfWork.BookingRepository.Update(booking);
                //await _unitOfWork.CommitAsync();

                //// Thông báo cho Provider
                //string boldStyle = $"<span style='font-weight:bold;'>#{decorService.Style}</span>";
                //await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                //{
                //    AccountId = provider.Id,  // Gửi thông báo cho provider
                //    Title = "New Booking Request",
                //    Content = $"You have a new booking request for service {boldStyle}",
                //    Url = $"{_clientBaseUrl}/seller/request"
                //});

                response.Success = true;
                response.Message = "Booking created successfully";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while processing your booking";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse> UpdateBookingRequestAsync(string bookingCode, UpdateBookingRequest request, int accountId)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Include(b => b.TimeSlots)
                    .Where(b => b.BookingCode == bookingCode && b.AccountId == accountId)
                    .FirstOrDefaultAsync();

                //// Kiểm tra nếu là thành viên và còn lượt đổi miễn phí
                //var customer = await _unitOfWork.AccountRepository
                //    .Queryable()
                //    .Include(a => a.Subscription)
                //    .FirstOrDefaultAsync(a => a.Id == accountId);

                //int freeChangesAllowed = customer?.Subscription?.MaxFreeRequestChanges ?? 0;
                //bool isMember = customer?.Subscription != null && customer.Subscription.Status == Subscription.SubscriptionStatus.Subcribed;

                //// Nếu đã vượt quá số lần đổi miễn phí
                //if (!isMember || booking.RequestChangeCount >= freeChangesAllowed)
                //{
                //    booking.AdditionalCost = (booking.AdditionalCost ?? 0) + 50000; // ví dụ: 50k phí phát sinh
                //    booking.IsAdditionalFeeCharged = true;
                //}
                //else
                //{
                //    booking.RequestChangeCount++;
                //}

                if (booking == null)
                {
                    response.Message = "Booking not found or access denied.";
                    return response;
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    response.Message = "Only bookings in 'Pending' status can be updated.";
                    return response;
                }

                // Ghi chú yêu cầu
                if (!string.IsNullOrWhiteSpace(request.Note))
                    booking.Note = request.Note;

                // Cập nhật địa ch
                if (request.AddressId.HasValue)
                {
                    var address = await _unitOfWork.AddressRepository.GetByIdAsync(request.AddressId.Value);
                    if (address == null || address.AccountId != accountId || address.IsDelete)
                    {
                        response.Message = "Invalid address.";
                        return response;
                    }
                    booking.AddressId = request.AddressId.Value;
                }

                // Cập nhật ngày khảo sát
                if (request.SurveyDate.HasValue)
                {
                    var slot = booking.TimeSlots.FirstOrDefault();
                    if (slot != null)
                    {
                        slot.SurveyDate = request.SurveyDate.Value;
                        _unitOfWork.TimeSlotRepository.Update(slot);
                    }
                }

                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                var provider = await _unitOfWork.AccountRepository.Queryable()
                    .Where(acc => acc.Id == booking.DecorService.AccountId)
                    .FirstOrDefaultAsync();

                if (provider != null)
                {
                    string colorBookingCode = $"<span style='font-weight:bold;'>#{bookingCode}</span>";
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = provider.Id,  // Gửi thông báo cho provider
                        Title = "Booking Updated",
                        Content = $"The booking for service {colorBookingCode} has been updated. Please review the changes.",
                        Url = $"{_clientBaseUrl}/seller/booking/{bookingCode}"  // URL trang chi tiết của booking
                    });
                }

                response.Success = true;
                response.Message = "Booking updated successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update booking.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<bool>> ChangeBookingStatusAsync(string bookingCode)
        {
            var response = new BaseResponse<bool>();
            
            var booking = await _unitOfWork.BookingRepository.Queryable()
                .Include(b => b.DecorService)// thêm Decorservice để set trạng thái dịch vụ
                .Where(b => b.BookingCode == bookingCode)
                .FirstOrDefaultAsync();

            if (booking == null)
            {
                response.Message = "Booking not found.";
                return response;
            }

            // 🔹 Xác định trạng thái tiếp theo
            Booking.BookingStatus? newStatus = booking.Status switch
            {
                Booking.BookingStatus.Pending => Booking.BookingStatus.Planning,
                Booking.BookingStatus.Planning => Booking.BookingStatus.Quoting,
                Booking.BookingStatus.Quoting => Booking.BookingStatus.Contracting,
                Booking.BookingStatus.Contracting => Booking.BookingStatus.Confirm,
                Booking.BookingStatus.Confirm when booking.DepositAmount > 0 => Booking.BookingStatus.DepositPaid,
                Booking.BookingStatus.DepositPaid => Booking.BookingStatus.Preparing,
                Booking.BookingStatus.Preparing => Booking.BookingStatus.InTransit,
                Booking.BookingStatus.InTransit => Booking.BookingStatus.Progressing,
                Booking.BookingStatus.Progressing => Booking.BookingStatus.AllDone,
                Booking.BookingStatus.AllDone when booking.DepositAmount >= booking.TotalPrice => Booking.BookingStatus.FinalPaid,
                Booking.BookingStatus.FinalPaid => Booking.BookingStatus.Completed,
                _ => null // Giữ nguyên nếu không hợp lệ
            };

            if (newStatus == null)
            {
                response.Message = "Invalid status transition.";
                return response;
            }

            // Lấy thông tin Provider
            var provider = await _unitOfWork.AccountRepository.Queryable()
                        .FirstOrDefaultAsync(a => a.Id == _unitOfWork.DecorServiceRepository.Queryable()
                            .Where(ds => ds.Id == booking.DecorServiceId)
                            .Select(ds => ds.AccountId)
                            .FirstOrDefault());

            var quotation = await _unitOfWork.QuotationRepository.Queryable()
                        .Where(q => q.BookingId == booking.Id)
                        .FirstOrDefaultAsync();

            string colorbookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{bookingCode}</span>";

            switch (newStatus)
            {
                case Booking.BookingStatus.Planning:
                    // Chuyển Provider sang trạng thái Busy khi bắt đầu Planning
                    if (provider != null)
                    {
                        provider.ProviderStatus = Account.AccountStatus.Busy;
                        _unitOfWork.AccountRepository.Update(provider);
                    }

                    // Cập nhật trạng thái DecorService thành NotAvailable
                    if (booking.DecorService != null)
                    {
                        booking.DecorService.Status = DecorService.DecorServiceStatus.NotAvailable;
                        _unitOfWork.DecorServiceRepository.Update(booking.DecorService);
                    }

                    // Thông báo cho khách hàng
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.AccountId,
                        Title = "Booking Status Update",
                        Content = $"Provider has accepted your booking request #{colorbookingCode}. Please deposit commitmentfee.",
                        Url = $"{_clientBaseUrl}/booking/request"
                    });
                    break;

                case Booking.BookingStatus.Quoting:
                    // Thông báo cho khách hàng
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.AccountId,
                        Title = "Booking Status Update",
                        Content = $"Provider is preparing a quotation for your booking request #{colorbookingCode}.",
                        Url = $"{_clientBaseUrl}/booking/request"
                    });
                    break;

                case Booking.BookingStatus.Contracting:                    

                    if (quotation == null || quotation.Status != Quotation.QuotationStatus.Confirmed)
                    {
                        response.Message = "Quotation must be confirmed before moving to Contracting.";
                        return response;
                    }

                    // Thông báo cho khách hàng
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.AccountId,
                        Title = "Booking Status Update",
                        Content = $"Provider is preparing a contract for your confirmed booking #{colorbookingCode}.",
                        Url = $"{_clientBaseUrl}/quotation"
                    });
                    break;

                case Booking.BookingStatus.Confirm:
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
                    // Thông báo cho khách hàng
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.AccountId,
                        Title = "Booking Status Update",
                        Content = $"The decoration materials for booking #{colorbookingCode} are being prepared.",
                        Url = null
                    });
                    break;

                case Booking.BookingStatus.InTransit:
                    // Thông báo cho khách hàng
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.AccountId,
                        Title = "Booking Status Update",
                        Content = $"The decoration materials for booking #{colorbookingCode} are on their way to your location.",
                        Url = null
                    });
                    break;

                case Booking.BookingStatus.Progressing:
                    // Thông báo cho khách hàng
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.AccountId,
                        Title = "Booking Status Update",
                        Content = $"Your decoration for booking #{colorbookingCode} is in progress.",
                        Url = null
                    });
                    break;

                case Booking.BookingStatus.AllDone:
                    var finalpaymentUrl = $"{_clientBaseUrl}/payment/{bookingCode}?type=final";
                    // Thông báo cho khách hàng
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.AccountId,
                        Title = "Booking Status Update",
                        Content = $"Your decoration for booking #{colorbookingCode} is complete. Please proceed with the payment.",
                        Url = finalpaymentUrl
                    });
                    break;

                case Booking.BookingStatus.FinalPaid:
                    // ✅ Kiểm tra đã thanh toán thi công chưa trước khi chuyển sang `FinalPaid`
                    if (booking.TotalPrice > 0 && booking.DepositAmount < booking.TotalPrice)
                    {
                        response.Message = "Full payment is required before proceeding.";
                        return response;
                    }
                    break;

                case Booking.BookingStatus.Completed:

                    if (provider != null)
                    {
                        // ✅ Cập nhật trạng thái Provider thành `Available`
                        provider.ProviderStatus = Account.AccountStatus.Idle;
                        _unitOfWork.AccountRepository.Update(provider);

                        // ✅ Cộng điểm uy tín (+5), tối đa 100
                        provider.Reputation = Math.Min(100, provider.Reputation + 5);

                        _unitOfWork.AccountRepository.Update(provider);
                    }

                    booking.IsBooked = false;
                    booking.Status = newStatus.Value; // Đảm bảo cập nhật status
                    _unitOfWork.BookingRepository.Update(booking); // Cập nhật lại booking sau khi thay đổi IsBooked
                    break;
            }

            // Cập nhật trạng thái booking
            booking.Status = newStatus.Value;
            _unitOfWork.BookingRepository.Update(booking);
            await _unitOfWork.CommitAsync();
            response.Success = true;
            response.Message = $"Booking status changed successfully.";
            response.Data = true;
            return response;
        }

        public async Task<BaseResponse> RequestCancellationAsync(string bookingCode, int accountId, int cancelTypeId, string? cancelReason)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                        .Include(b => b.DecorService) // Thêm include để cập nhật trạng thái DecorService
                        .Where(b => b.BookingCode == bookingCode)
                        .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Success = false;
                    response.Message = "Booking not found.";
                    return response;
                }

                if (booking.CancelDisable == true)
                {
                    response.Success = false;
                    response.Message = "This booking can no longer be canceled.";
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
                    response.Message = "You can only request cancellation in Pending or Planning status.";
                    return response;
                }

                if (cancelTypeId == 7 && string.IsNullOrEmpty(cancelReason))
                {
                    response.Success = false;
                    response.Message = "Please provide a reason for cancellation when selecting 'Other'.";
                    return response;
                }

                //Thông báo
                string colorBookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{booking.BookingCode}</span>";
                string customerUrl = $"";
                string providerUrl = $"";

                // Xử lý khác nhau theo trạng thái
                if (booking.Status == BookingStatus.Pending)
                {
                    booking.Status = BookingStatus.Canceled;
                    booking.IsBooked = false;

                    if (booking.DecorService != null)
                    {
                        booking.DecorService.Status = DecorService.DecorServiceStatus.Available;
                        _unitOfWork.DecorServiceRepository.Update(booking.DecorService);
                    }

                    // ✅ THÔNG BÁO CHO CẢ CUSTOMER VÀ PROVIDER
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.AccountId,
                        Title = "Booking Canceled",
                        Content = $"You have successfully canceled booking {colorBookingCode}.",
                        Url = customerUrl
                    });

                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.DecorService.AccountId,
                        Title = "Booking Canceled",
                        Content = $"Customer has canceled booking {colorBookingCode}.",
                        Url = providerUrl
                    });

                    response.Message = "Booking has been canceled successfully.";
                }
                else if (booking.Status == BookingStatus.Planning)
                {
                    booking.Status = BookingStatus.PendingCancel;

                    // ✅ THÔNG BÁO CHO PROVIDER CHỜ DUYỆT
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.DecorService.AccountId,
                        Title = "Booking Cancellation Request",
                        Content = $"Customer has requested to cancel booking {colorBookingCode}. Please review and approve.",
                        Url = providerUrl
                    });

                    response.Message = "Cancellation request submitted successfully. Waiting for provider approval.";
                }

                // Cập nhật thông tin hủy
                booking.CancelTypeId = cancelTypeId;
                booking.CancelReason = cancelReason;

                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
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
                    .Where(b => b.BookingCode == bookingCode)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                var service = await _unitOfWork.DecorServiceRepository.Queryable()
                    .Where(ds => ds.Id == booking.DecorServiceId)
                    .FirstOrDefaultAsync();

                if (service == null || service.AccountId != providerId)
                {
                    response.Message = "You do not have permission to approve this cancellation.";
                    return response;
                }

                if (booking.Status != Booking.BookingStatus.PendingCancel)
                {
                    response.Message = "No pending cancellation request to approve.";
                    return response;
                }

                booking.Status = BookingStatus.Canceled;
                _unitOfWork.BookingRepository.Update(booking);
                service.Status = DecorService.DecorServiceStatus.Available;
                booking.IsBooked = false;

                await _unitOfWork.CommitAsync();

                string colorBookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{booking.BookingCode}</span>";
                string customerUrl = "";

                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = booking.AccountId,
                    Title = "Booking Canceled",
                    Content = $"Your booking #{colorBookingCode} has been canceled successfully.",
                    Url = customerUrl
                });

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
                    .Where(b => b.BookingCode == bookingCode)
                    .FirstOrDefaultAsync();

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

                if (booking.Status != Booking.BookingStatus.PendingCancel)
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
                    .Where(b => b.BookingCode == bookingCode)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                var service = await _unitOfWork.DecorServiceRepository.Queryable()
                    .Where(ds => ds.Id == booking.DecorServiceId)
                    .FirstOrDefaultAsync();

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
                booking.IsBooked = false;

                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                // ✅ THÔNG BÁO CHO CUSTOMER
                string colorBookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{booking.BookingCode}</span>";
                string customerUrl = "";

                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = booking.AccountId,
                    Title = "Booking Rejected",
                    Content = $"Your booking #{colorBookingCode} has been rejected by the provider",
                    Url = customerUrl
                });

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
                    .Where(b => b.BookingCode == bookingCode)
                    .FirstOrDefaultAsync();

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
                    .Where(q => q.BookingId == booking.Id)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                // 🔹 Lấy contract liên quan đến booking thông qua quotation
                var contract = await _unitOfWork.ContractRepository.Queryable()
                    .Where(c => c.QuotationId == quotation.Id)
                    .FirstOrDefaultAsync();

                if (contract == null)
                {
                    response.Message = "Contract not found for this booking.";
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

                // 🔹 Cập nhật contract đã đặt cọc
                contract.isDeposited = true;
                _unitOfWork.ContractRepository.Update(contract);

                //// 🔹 Chuyển trạng thái Provider sang "Busy"
                //provider.ProviderStatus = Account.AccountStatus.Busy;
                _unitOfWork.AccountRepository.Update(provider);

                await _unitOfWork.CommitAsync();

                // ========================
                // ✅ Thêm thông báo sau khi thanh toán thành công
                // ========================

                string customerUrl = ""; // FE route cho customer
                string providerUrl = "";       // FE route cho provider

                string colorbookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{bookingCode}</span>";
                // 1. Thông báo cho Customer
                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = booking.AccountId,
                    Title = "Deposited Successful",
                    Content = $"You have deposited for booking #{colorbookingCode} successful",
                    Url = customerUrl
                });

                // 2. Thông báo cho Provider
                var providerId = await _unitOfWork.DecorServiceRepository.Queryable()
                    .Where(ds => ds.Id == booking.DecorServiceId)
                    .Select(ds => ds.AccountId)
                    .FirstOrDefaultAsync();

                if (providerId > 0)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = providerId,
                        Title = "Deposited Booking",
                        Content = $"The customer has completed payment for booking #{colorbookingCode}.",
                        Url = providerUrl
                    });
                }

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
                    .Where(b => b.BookingCode == bookingCode)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }
                if (booking.Status != Booking.BookingStatus.AllDone)
                {
                    response.Message = "Only AllDone phase can be paid for.";
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

                // 🔹 Lấy Quotation trước (vì Contract liên kết với Quotation)
                var quotation = await _unitOfWork.QuotationRepository.Queryable()
                    .Where(q => q.BookingId == booking.Id)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found for this booking.";
                    return response;
                }

                // 🔹 Lấy Contract theo QuotationId
                var contract = await _unitOfWork.ContractRepository.Queryable()
                    .Where(c => c.QuotationId == quotation.Id)
                    .FirstOrDefaultAsync();

                if (contract == null)
                {
                    response.Message = "Contract not found for this booking.";
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

                booking.Status = Booking.BookingStatus.FinalPaid;
                _unitOfWork.BookingRepository.Update(booking);

                // 🔹 Cập nhật Contract
                contract.isFinalPaid = true;
                _unitOfWork.ContractRepository.Update(contract);

                await _unitOfWork.CommitAsync();

                // ========================
                // ✅ Thêm thông báo sau khi thanh toán thành công
                // ========================

                string customerUrl = ""; // FE route cho customer
                string providerUrl = "";       // FE route cho provider
                string adminUrl = "";             // FE route cho admin

                string colorbookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{bookingCode}</span>";
                // 1. Thông báo cho Customer
                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = booking.AccountId,
                    Title = "Payment Successful",
                    Content = $"Your payment for booking #{colorbookingCode} has been successfully processed. Thank you for your business!",
                    Url = customerUrl
                });

                // 2. Thông báo cho Provider
                if (providerId > 0)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = providerId,
                        Title = "Final Paid Booking",
                        Content = $"The customer has completed payment for booking #{colorbookingCode}.",
                        Url = providerUrl
                    });
                }

                // 3. Thông báo cho tất cả Admin
                var adminIds = await _unitOfWork.AccountRepository.Queryable()
                    .Where(a => a.RoleId == 1) // Giả sử role 1 là admin
                    .Select(a => a.Id)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = adminId,
                        Title = "Revenue Notice",
                        Content = $"You have been credited with an additional amount in your income.",
                        Url = adminUrl
                    });
                }

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

        // In BookingService.cs
        public async Task<BaseResponse> ProcessCommitDepositAsync(string bookingCode)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Include(b => b.DecorService)
                    .ThenInclude(ds => ds.Account)
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                // Only allow trust deposit in Planning status
                if (booking.Status != Booking.BookingStatus.Planning)
                {
                    response.Message = "Commit deposit can only be paid during Planning phase.";
                    return response;
                }

                if (booking.IsCommitDepositPaid == true)
                {
                    response.Message = "Commit deposit already paid.";
                    return response;
                }

                var provider = booking.DecorService.Account;
                var customerId = booking.AccountId;

                // Process payment
                bool paymentSuccess = await _paymentService.TrustDeposit(
                    customerId,
                    provider.Id,
                    booking.CommitDepositAmount,
                    booking.Id);

                if (!paymentSuccess)
                {
                    response.Message = "Commit deposit payment failed.";
                    return response;
                }

                // Update booking status
                booking.IsCommitDepositPaid = true;
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();

                // ✅ THÊM THÔNG BÁO
                string colorBookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{booking.BookingCode}</span>";

                string customerUrl = $"";
                string providerUrl = $"";

                // Thông báo cho Customer
                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = customerId,
                    Title = "Payment successful",
                    Content = $"You have successfully deposited the commitment fee for booking #{colorBookingCode}.",
                    Url = customerUrl
                });

                // Thông báo cho Provider
                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = provider.Id,
                    Title = "CommitmentFee Paid Booking",
                    Content = $"Customer has deposited the commitment fee for booking #{colorBookingCode}.",
                    Url = providerUrl
                });

                response.Success = true;
                response.Message = "Commit deposit paid successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to process commit deposit.";
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
