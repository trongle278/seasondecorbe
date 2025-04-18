﻿using System;
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
                    ((!request.Status.HasValue || booking.Status == request.Status.Value)) &&
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
                        Status = (int)booking.DecorService.Status,
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
                    Address = $"{booking.Address.Detail}, {booking.Address.Street}, {booking.Address.Ward}, {booking.Address.District}, {booking.Address.Province}",
                    CreatedAt = booking.CreateAt,
                    IsQuoteExisted = booking.Quotations.Any(),

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
                        EstimatedCompletion = bd.EstimatedCompletion,
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
                    .FirstOrDefaultAsync(ds => ds.Id == request.DecorServiceId);

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
                    .FirstOrDefaultAsync(acc => acc.Id == decorService.AccountId);

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

                // 2. Address Validation
                var address = await _unitOfWork.AddressRepository.GetByIdAsync(request.AddressId);
                if (address?.AccountId != accountId || address.IsDelete)
                {
                    response.Message = "The selected address is invalid or not registered to your account";
                    return response;
                }

                // 3. Count Valid Addresses
                var validAddresses = await _unitOfWork.AddressRepository.Queryable()
                    .Where(a => a.AccountId == accountId && !a.IsDelete)
                    .CountAsync();

                // 4. Active Booking Check
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
                               b.Status == BookingStatus.ConstructionPayment ||
                               b.Status == BookingStatus.PendingCancellation))
                    .ToListAsync();

                //// 5. Booking Limit Enforcement
                //if (activeBookings.Count >= validAddresses)
                //{
                //    response.Message = "You've reached your maximum active bookings limit";
                //    return response;
                //}

                // 6. Address Availability Check
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
                    //RequestChangeCount = 0, //số lần đổi yêu cầu
                    //IsAdditionalFeeCharged = false,
                    CreateAt = DateTime.Now
                };

                await _unitOfWork.BookingRepository.InsertAsync(booking);

                // 🔹 Cập nhật trạng thái IsBooked cho account
                var customerAccount = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                if (customerAccount != null)
                {
                    customerAccount.IsBooked = true;
                    _unitOfWork.AccountRepository.Update(customerAccount);
                }

                await _unitOfWork.CommitAsync();

                var timeSlot = new TimeSlot
                {
                    BookingId = booking.Id,
                    SurveyDate = request.SurveyDate,
                };

                await _unitOfWork.TimeSlotRepository.InsertAsync(timeSlot);
                await _unitOfWork.CommitAsync();

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
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode && b.AccountId == accountId);

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
                Booking.BookingStatus.Planning => Booking.BookingStatus.Quoting,
                Booking.BookingStatus.Quoting => Booking.BookingStatus.Contracting,
                Booking.BookingStatus.Contracting => Booking.BookingStatus.Confirm,
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

            // Lấy thông tin Provider
            var provider = await _unitOfWork.AccountRepository.Queryable()
                        .FirstOrDefaultAsync(a => a.Id == _unitOfWork.DecorServiceRepository.Queryable()
                            .Where(ds => ds.Id == booking.DecorServiceId)
                            .Select(ds => ds.AccountId)
                            .FirstOrDefault());

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
                    break;

                case Booking.BookingStatus.Quoting:
                    break;

                case Booking.BookingStatus.Contracting:
                    var quotation = await _unitOfWork.QuotationRepository.Queryable()
                        .FirstOrDefaultAsync(q => q.BookingId == booking.Id);

                    if (quotation == null || quotation.Status != Quotation.QuotationStatus.Confirmed)
                    {
                        response.Message = "Quotation must be confirmed before moving to Contracting.";
                        return response;
                    }
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
                    break;

                case Booking.BookingStatus.InTransit:
                    // ✅ Khi chuyển sang `InTransit`, cập nhật EstimatedCompletion cho `Chi Phí Nguyên liệu`
                    var materialDetail = await _unitOfWork.BookingDetailRepository.Queryable()
                        .FirstOrDefaultAsync(bd => bd.BookingId == booking.Id && bd.ServiceItem == "Materials Cost");

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
                        .FirstOrDefaultAsync(bd => bd.BookingId == booking.Id && bd.ServiceItem == "Construction Cost");

                    if (laborDetail != null)
                    {
                        laborDetail.EstimatedCompletion = DateTime.Now;
                        _unitOfWork.BookingDetailRepository.Update(laborDetail);
                    }

                    if (provider != null)
                    {
                        // ✅ Cập nhật trạng thái Provider thành `Available`
                        provider.ProviderStatus = Account.AccountStatus.Idle;
                        _unitOfWork.AccountRepository.Update(provider);

                        // ✅ Cộng điểm uy tín (+5), tối đa 100
                        provider.Reputation = Math.Min(100, provider.Reputation + 5);

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
                        .Include(b => b.DecorService) // Thêm include để cập nhật trạng thái DecorService
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
                    response.Message = "You can only request cancellation in Pending or Planning status.";
                    return response;
                }

                if (cancelTypeId == 7 && string.IsNullOrEmpty(cancelReason))
                {
                    response.Success = false;
                    response.Message = "Please provide a reason for cancellation when selecting 'Other'.";
                    return response;
                }

                // Xử lý khác nhau theo trạng thái
                if (booking.Status == BookingStatus.Pending)
                {
                    // Nếu là Pending -> hủy luôn
                    booking.Status = BookingStatus.Canceled;

                    // Chuyển trạng thái DecorService về Available nếu có
                    if (booking.DecorService != null)
                    {
                        booking.DecorService.Status = DecorService.DecorServiceStatus.Available;
                        _unitOfWork.DecorServiceRepository.Update(booking.DecorService);
                    }

                    // 🔹 Cập nhật IsBooked của customer
                    var customer = await _unitOfWork.AccountRepository.GetByIdAsync(booking.AccountId);
                    if (customer != null)
                    {
                        customer.IsBooked = false;
                        _unitOfWork.AccountRepository.Update(customer);
                    }

                    response.Message = "Booking has been canceled successfully.";
                }
                else if (booking.Status == BookingStatus.Planning)
                {
                    // Nếu là Planning -> chuyển sang PendingCancellation
                    booking.Status = BookingStatus.PendingCancellation;
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

                // 🔹 Cập nhật IsBooked của customer
                var customer = await _unitOfWork.AccountRepository.GetByIdAsync(booking.AccountId);
                if (customer != null)
                {
                    customer.IsBooked = false;
                    _unitOfWork.AccountRepository.Update(customer);
                }

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

                //// 🔹 Chuyển trạng thái Provider sang "Busy"
                //provider.ProviderStatus = Account.AccountStatus.Busy;
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
        private static int _bookingCounter = 0;
        private string GenerateBookingCode()
        {
            _bookingCounter++;
            return $"BKG{_bookingCounter:D4}";
        }
        #endregion
    }
}
