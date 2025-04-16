using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;
using static DataAccessObject.Models.Booking;

namespace BusinessLogicLayer.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<ProviderDashboardResponse>> GetProviderDashboardAsync(int providerId)
        {
            var response = new BaseResponse<ProviderDashboardResponse>();
            try
            {
                var now = DateTime.Now;
                var thisWeekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                var lastWeekStart = thisWeekStart.AddDays(-7);
                var lastWeekEnd = thisWeekStart;

                // Lấy tỷ lệ hoa hồng từ Setting (với giả sử chỉ có 1 bản ghi hoa hồng)
                var commissionRate = await _unitOfWork.SettingRepository.Queryable()
                    .Select(s => s.Commission)
                    .FirstOrDefaultAsync();

                // --- BOOKINGS ---
                var bookingsQuery = _unitOfWork.BookingRepository.Queryable()
                    .Where(b => b.DecorService.AccountId == providerId);

                var bookings = await bookingsQuery.ToListAsync();

                var totalBookings = bookings.Count;
                var processingBookings = bookings.Count(b => b.Status is Booking.BookingStatus.Planning or
                                                             Booking.BookingStatus.Quoting or
                                                             Booking.BookingStatus.Contracting or
                                                             Booking.BookingStatus.Preparing or
                                                             Booking.BookingStatus.Progressing);

                var completedBookings = bookings.Count(b => b.Status == Booking.BookingStatus.Completed);
                var canceledBookings = bookings.Count(b => b.Status == Booking.BookingStatus.Canceled);
                var totalBookingRevenue = bookings.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);

                // Áp dụng hoa hồng vào doanh thu bookings
                totalBookingRevenue -= totalBookingRevenue * commissionRate;

                var bookingsThisWeek = bookings.Where(b => b.CreateAt >= thisWeekStart).ToList();
                var bookingsLastWeek = bookings.Where(b => b.CreateAt >= lastWeekStart && b.CreateAt < lastWeekEnd).ToList();

                var thisWeekBookingCount = bookingsThisWeek.Count;
                var lastWeekBookingCount = bookingsLastWeek.Count;

                var thisWeekBookingRevenue = bookingsThisWeek.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);
                var lastWeekBookingRevenue = bookingsLastWeek.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);

                // Áp dụng hoa hồng vào doanh thu bookings trong tuần này và tuần trước
                thisWeekBookingRevenue -= thisWeekBookingRevenue * commissionRate;
                lastWeekBookingRevenue -= lastWeekBookingRevenue * commissionRate;

                double bookingGrowthPercent = lastWeekBookingCount == 0
                    ? (thisWeekBookingCount > 0 ? 100 : 0)
                    : ((double)(thisWeekBookingCount - lastWeekBookingCount) / lastWeekBookingCount) * 100;

                double bookingRevenueGrowthPercent = lastWeekBookingRevenue == 0
                    ? (thisWeekBookingRevenue > 0 ? 100 : 0)
                    : ((double)(thisWeekBookingRevenue - lastWeekBookingRevenue) / (double)lastWeekBookingRevenue) * 100;

                // --- FOLLOWERS ---
                var totalFollowers = await _unitOfWork.FollowRepository.Queryable()
                    .CountAsync(f => f.FollowingId == providerId);

                // --- SERVICES ---
                var services = await _unitOfWork.DecorServiceRepository.Queryable()
                    .Where(s => s.AccountId == providerId)
                    .Include(s => s.FavoriteServices)
                    .ToListAsync();

                var totalServices = services.Count;
                var topServices = services
                    .OrderByDescending(s => s.FavoriteServices.Count)
                    .Take(5)
                    .Select(s => new TopServiceResponse
                    {
                        ServiceId = s.Id,
                        Style = s.Style,
                        FavoriteCount = s.FavoriteServices.Count
                    })
                    .ToList();

                // --- PRODUCTS ---
                var products = await _unitOfWork.ProductRepository.Queryable()
                    .Where(p => p.AccountId == providerId)
                    .Include(p => p.OrderDetails)
                    .ToListAsync();

                var totalProducts = products.Count;
                var topProducts = products
                    .OrderByDescending(p => p.OrderDetails.Sum(od => od.Quantity))
                    .Take(5)
                    .Select(p => new TopProductResponse
                    {
                        ProductId = p.Id,
                        ProductName = p.ProductName,
                        SoldQuantity = p.OrderDetails.Sum(od => od.Quantity)
                    })
                    .ToList();

                // --- ORDERS ---
                var orderDetails = await _unitOfWork.OrderDetailRepository.Queryable()
                    .Include(od => od.Order)
                    .Include(od => od.Product)
                    .Where(od => od.Product.AccountId == providerId)
                    .ToListAsync();

                var relatedOrders = orderDetails.Select(od => od.Order).Distinct().ToList();
                var totalOrders = relatedOrders.Count;
                var totalOrderRevenue = relatedOrders
                    .Where(o => o.Status == Order.OrderStatus.Completed)
                    .Sum(o => o.TotalPrice);

                // Áp dụng hoa hồng vào doanh thu sản phẩm
                totalOrderRevenue -= totalOrderRevenue * commissionRate;

                var ordersThisWeek = relatedOrders.Where(o => o.OrderDate >= thisWeekStart).ToList();
                var ordersLastWeek = relatedOrders.Where(o => o.OrderDate >= lastWeekStart && o.OrderDate < lastWeekEnd).ToList();

                var thisWeekOrderCount = ordersThisWeek.Count;
                var lastWeekOrderCount = ordersLastWeek.Count;

                var thisWeekOrderRevenue = ordersThisWeek.Where(o => o.Status == Order.OrderStatus.Completed).Sum(o => o.TotalPrice);
                var lastWeekOrderRevenue = ordersLastWeek.Where(o => o.Status == Order.OrderStatus.Completed).Sum(o => o.TotalPrice);

                // Áp dụng hoa hồng vào doanh thu orders trong tuần này và tuần trước
                thisWeekOrderRevenue -= thisWeekOrderRevenue * commissionRate;
                lastWeekOrderRevenue -= lastWeekOrderRevenue * commissionRate;

                double orderGrowthPercent = lastWeekOrderCount == 0
                    ? (thisWeekOrderCount > 0 ? 100 : 0)
                    : ((double)(thisWeekOrderCount - lastWeekOrderCount) / lastWeekOrderCount) * 100;

                double orderRevenueGrowthPercent = lastWeekOrderRevenue == 0
                    ? (thisWeekOrderRevenue > 0 ? 100 : 0)
                    : ((double)(thisWeekOrderRevenue - lastWeekOrderRevenue) / (double)lastWeekOrderRevenue) * 100;

                // --- RESPONSE ---
                var totalRevenue = totalBookingRevenue + totalOrderRevenue; // Tổng doanh thu sau khi trừ hoa hồng
                var thisWeekTotalRevenue = thisWeekBookingRevenue + thisWeekOrderRevenue;
                var lastWeekTotalRevenue = lastWeekBookingRevenue + lastWeekOrderRevenue;
                double totalRevenueGrowthPercent = lastWeekTotalRevenue == 0
                    ? (thisWeekTotalRevenue > 0 ? 100 : 0)
                    : ((double)(thisWeekTotalRevenue - lastWeekTotalRevenue) / (double)lastWeekTotalRevenue) * 100;

                response.Success = true;
                response.Data = new ProviderDashboardResponse
                {
                    TotalFollowers = totalFollowers,
                    TotalBookings = totalBookings,
                    TotalServices = totalServices,
                    TopServices = topServices,
                    ProcessingBookings = processingBookings,
                    CompletedBookings = completedBookings,
                    CanceledBookings = canceledBookings,
                    TotalBookingRevenue = totalBookingRevenue,
                    ThisWeekBookings = thisWeekBookingCount,
                    LastWeekBookings = lastWeekBookingCount,
                    BookingGrowthRate = Math.Round(bookingGrowthPercent, 2),             

                    TotalRevenue = totalRevenue, // Tổng doanh thu sau hoa hồng
                    ThisWeekTotalRevenue = thisWeekTotalRevenue,
                    LastWeekTotalRevenue = lastWeekTotalRevenue,
                    TotalRevenueGrowthRate = Math.Round(totalRevenueGrowthPercent, 2),

                    TotalProducts = totalProducts,
                    TotalOrders = totalOrders,
                    TopProducts = topProducts,
                    TotalOrderRevenue = totalOrderRevenue,
                    ThisWeekOrders = thisWeekOrderCount,
                    LastWeekOrders = lastWeekOrderCount,
                    OrderGrowthRate = Math.Round(orderGrowthPercent, 2)
                };
            }
            catch (Exception ex)
            {
                response.Message = "Failed to load dashboard.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<List<MonthlyRevenueResponse>>> GetMonthlyRevenueAsync(int providerId)
        {
            var response = new BaseResponse<List<MonthlyRevenueResponse>>();
            try
            {
                var now = DateTime.Now;
                var currentYear = now.Year;

                // Lấy tỷ lệ commission từ bảng Setting
                var commissionRate = await _unitOfWork.SettingRepository.Queryable()
                    .Select(s => s.Commission)
                    .FirstOrDefaultAsync();

                var transactions = await _unitOfWork.PaymentTransactionRepository.Queryable()
                    .Include(pt => pt.Booking)
                        .ThenInclude(b => b.DecorService)
                    .Include(pt => pt.Order)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                    .Where(pt =>
                        pt.TransactionStatus == PaymentTransaction.EnumTransactionStatus.Success &&
                        pt.TransactionDate.Year == currentYear &&
                        (
                            (pt.Booking != null && pt.Booking.DecorService.AccountId == providerId) ||
                            (pt.Order != null && pt.Order.OrderDetails.Any(od => od.Product.AccountId == providerId))
                        )
                    )
                    .ToListAsync();

                var monthlyRevenue = Enumerable.Range(1, 12).Select(month =>
                {
                    // Tính tổng doanh thu từ Booking
                    var bookingTotal = transactions
                        .Where(t => t.TransactionDate.Month == month && t.Booking != null && t.Booking.DecorService.AccountId == providerId)
                        .Select(t => t.Booking.TotalPrice)
                        .Distinct() // phòng trường hợp nhiều giao dịch liên quan đến 1 booking
                        .Sum();

                    // Tính tổng doanh thu từ Order
                    var orderTotal = transactions
                        .Where(t => t.TransactionDate.Month == month && t.Order != null)
                        .Select(t => new
                        {
                            t.Order.Id,
                            t.Order.TotalPrice,
                            ProviderProduct = t.Order.OrderDetails.Any(od => od.Product.AccountId == providerId)
                        })
                        .Where(o => o.ProviderProduct)
                        .DistinctBy(o => o.Id) // để không trùng nhiều transaction cùng 1 đơn hàng
                        .Sum(o => o.TotalPrice);

                    // Tính tổng doanh thu sau khi trừ commission
                    var totalRevenue = (bookingTotal + orderTotal) * (1 - commissionRate);

                    return new MonthlyRevenueResponse
                    {
                        Year = currentYear,
                        Month = month,
                        TotalRevenue = totalRevenue
                    };
                }).ToList();

                response.Success = true;
                response.Data = monthlyRevenue;
            }
            catch (Exception ex)
            {
                response.Message = "Failed to load monthly revenue.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<List<CustomerSpendingRankingResponse>>> GetTopCustomerSpendingRankingAsync(int providerId)
        {
            var response = new BaseResponse<List<CustomerSpendingRankingResponse>>();
            try
            {
                var setting = await _unitOfWork.SettingRepository.Queryable().FirstOrDefaultAsync();
                var commissionRate = setting?.Commission ?? 0;

                var transactions = await _unitOfWork.PaymentTransactionRepository.Queryable()
                    .Include(pt => pt.Booking)
                        .ThenInclude(b => b.DecorService)
                    .Include(pt => pt.Booking)
                        .ThenInclude(b => b.Account)
                    .Include(pt => pt.Order)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                    .Include(pt => pt.Order)
                        .ThenInclude(o => o.Account)
                    .Where(pt =>
                        pt.TransactionStatus == PaymentTransaction.EnumTransactionStatus.Success &&
                        (
                            (pt.Booking != null && pt.Booking.DecorService.AccountId == providerId &&
                             (pt.TransactionType == PaymentTransaction.EnumTransactionType.Deposit ||
                              pt.TransactionType == PaymentTransaction.EnumTransactionType.Pay)) ||
                            (pt.Order != null && pt.Order.OrderDetails.Any(od => od.Product.AccountId == providerId) &&
                             pt.TransactionType == PaymentTransaction.EnumTransactionType.Pay)
                        )
                    )
                    .ToListAsync();

                var customerRevenue = new Dictionary<int, (string FullName, string Email, string? Avatar, decimal Total)>();

                // Booking
                var bookingGroups = transactions
                    .Where(t => t.Booking != null && t.Booking.DecorService.AccountId == providerId)
                    .GroupBy(t => t.Booking.AccountId);

                foreach (var group in bookingGroups)
                {
                    var bookingTotal = group.Select(t => t.Booking.TotalPrice).Distinct().Sum();
                    var revenue = bookingTotal * (1 - commissionRate);
                    var customer = group.First().Booking.Account;
                    var fullName = $"{customer.LastName} {customer.FirstName}";
                    var email = customer.Email;
                    var avatar = customer.Avatar;

                    if (customerRevenue.ContainsKey(customer.Id))
                        customerRevenue[customer.Id] = (fullName, email, avatar, customerRevenue[customer.Id].Total + revenue);
                    else
                        customerRevenue[customer.Id] = (fullName, email, avatar, revenue);
                }

                // Order
                var orderGroups = transactions
                    .Where(t => t.Order != null)
                    .Select(t => new
                    {
                        t.Order.Id,
                        Account = t.Order.Account,
                        t.Order.TotalPrice,
                        ProviderProduct = t.Order.OrderDetails.Any(od => od.Product.AccountId == providerId)
                    })
                    .Where(x => x.ProviderProduct)
                    .DistinctBy(x => x.Id)
                    .GroupBy(x => x.Account.Id);

                foreach (var group in orderGroups)
                {
                    var orderTotal = group.Sum(x => x.TotalPrice);
                    var revenue = orderTotal * (1 - commissionRate);
                    var customer = group.First().Account;
                    var fullName = $"{customer.LastName} {customer.FirstName}";
                    var email = customer.Email;
                    var avatar = customer.Avatar;

                    if (customerRevenue.ContainsKey(customer.Id))
                        customerRevenue[customer.Id] = (fullName, email, avatar, customerRevenue[customer.Id].Total + revenue);
                    else
                        customerRevenue[customer.Id] = (fullName, email, avatar, revenue);
                }

                var top5 = customerRevenue
                    .OrderByDescending(x => x.Value.Total)
                    .Take(5)
                    .Select(x => new CustomerSpendingRankingResponse
                    {
                        CustomerId = x.Key,
                        FullName = x.Value.FullName,
                        Email = x.Value.Email,
                        Avatar = x.Value.Avatar,
                        TotalSpending = x.Value.Total
                    }).ToList();

                response.Success = true;
                response.Data = top5;
            }
            catch (Exception ex)
            {
                response.Message = "Failed to load top customer spending ranking.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<AdminDashboardResponse>> GetAdminDashboardAsync()
        {
            var response = new BaseResponse<AdminDashboardResponse>();
            try
            {
                var now = DateTime.Now;
                var thisWeekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                var lastWeekStart = thisWeekStart.AddDays(-7);
                var lastWeekEnd = thisWeekStart;

                // Lấy tỷ lệ hoa hồng từ Setting (với giả sử chỉ có 1 bản ghi hoa hồng)
                var commissionRate = await _unitOfWork.SettingRepository.Queryable()
                    .Select(s => s.Commission)
                    .FirstOrDefaultAsync();

                // --- BOOKINGS --- Tính tổng bookings cho tất cả providers
                var bookingsQuery = _unitOfWork.BookingRepository.Queryable();

                var bookings = await bookingsQuery.ToListAsync();

                var totalBookings = bookings.Count;
                var completedBookings = bookings.Count(b => b.Status == Booking.BookingStatus.Completed);
                var canceledBookings = bookings.Count(b => b.Status == Booking.BookingStatus.Canceled);
                var totalBookingRevenue = bookings.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);

                // Áp dụng hoa hồng vào doanh thu bookings
                totalBookingRevenue -= totalBookingRevenue * commissionRate;

                var bookingsThisWeek = bookings.Where(b => b.CreateAt >= thisWeekStart).ToList();
                var bookingsLastWeek = bookings.Where(b => b.CreateAt >= lastWeekStart && b.CreateAt < lastWeekEnd).ToList();

                var thisWeekBookingCount = bookingsThisWeek.Count;
                var lastWeekBookingCount = bookingsLastWeek.Count;

                var thisWeekBookingRevenue = bookingsThisWeek.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);
                var lastWeekBookingRevenue = bookingsLastWeek.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);

                // Áp dụng hoa hồng vào doanh thu bookings trong tuần này và tuần trước
                thisWeekBookingRevenue -= thisWeekBookingRevenue * commissionRate;
                lastWeekBookingRevenue -= lastWeekBookingRevenue * commissionRate;

                double bookingGrowthPercent = lastWeekBookingCount == 0
                    ? (thisWeekBookingCount > 0 ? 100 : 0)
                    : ((double)(thisWeekBookingCount - lastWeekBookingCount) / lastWeekBookingCount) * 100;

                double bookingRevenueGrowthPercent = lastWeekBookingRevenue == 0
                    ? (thisWeekBookingRevenue > 0 ? 100 : 0)
                    : ((double)(thisWeekBookingRevenue - lastWeekBookingRevenue) / (double)lastWeekBookingRevenue) * 100;

                // --- SERVICES --- Tính dịch vụ yêu thích của tất cả provider
                var services = await _unitOfWork.DecorServiceRepository.Queryable()
                    .Include(s => s.FavoriteServices)
                    .ToListAsync();

                var totalServices = services.Count;
                var topServices = services
                    .OrderByDescending(s => s.FavoriteServices.Count)
                    .Take(5)
                    .Select(s => new TopServiceResponse
                    {
                        ServiceId = s.Id,
                        Style = s.Style,
                        FavoriteCount = s.FavoriteServices.Count
                    })
                    .ToList();

                // --- PRODUCTS --- Tổng sản phẩm bán được
                var products = await _unitOfWork.ProductRepository.Queryable()
                    .Include(p => p.OrderDetails)
                    .ToListAsync();

                var totalProducts = products.Count;
                var topProducts = products
                    .OrderByDescending(p => p.OrderDetails.Sum(od => od.Quantity))
                    .Take(5)
                    .Select(p => new TopProductResponse
                    {
                        ProductId = p.Id,
                        ProductName = p.ProductName,
                        SoldQuantity = p.OrderDetails.Sum(od => od.Quantity)
                    })
                    .ToList();

                // --- ORDERS --- Tính tổng đơn hàng của tất cả provider
                var orderDetails = await _unitOfWork.OrderDetailRepository.Queryable()
                    .Include(od => od.Order)
                    .Where(od => od.Product.AccountId != null) // Tất cả sản phẩm từ provider
                    .ToListAsync();

                var relatedOrders = orderDetails.Select(od => od.Order).Distinct().ToList();
                var totalOrders = relatedOrders.Count;
                var totalOrderRevenue = relatedOrders
                    .Where(o => o.Status == Order.OrderStatus.Completed)
                    .Sum(o => o.TotalPrice);

                // Áp dụng hoa hồng vào doanh thu sản phẩm
                totalOrderRevenue -= totalOrderRevenue * commissionRate;

                var ordersThisWeek = relatedOrders.Where(o => o.OrderDate >= thisWeekStart).ToList();
                var ordersLastWeek = relatedOrders.Where(o => o.OrderDate >= lastWeekStart && o.OrderDate < lastWeekEnd).ToList();

                var thisWeekOrderCount = ordersThisWeek.Count;
                var lastWeekOrderCount = ordersLastWeek.Count;

                var thisWeekOrderRevenue = ordersThisWeek.Where(o => o.Status == Order.OrderStatus.Completed).Sum(o => o.TotalPrice);
                var lastWeekOrderRevenue = ordersLastWeek.Where(o => o.Status == Order.OrderStatus.Completed).Sum(o => o.TotalPrice);

                // Áp dụng hoa hồng vào doanh thu orders trong tuần này và tuần trước
                thisWeekOrderRevenue -= thisWeekOrderRevenue * commissionRate;
                lastWeekOrderRevenue -= lastWeekOrderRevenue * commissionRate;

                double orderGrowthPercent = lastWeekOrderCount == 0
                    ? (thisWeekOrderCount > 0 ? 100 : 0)
                    : ((double)(thisWeekOrderCount - lastWeekOrderCount) / lastWeekOrderCount) * 100;

                double orderRevenueGrowthPercent = lastWeekOrderRevenue == 0
                    ? (thisWeekOrderRevenue > 0 ? 100 : 0)
                    : ((double)(thisWeekOrderRevenue - lastWeekOrderRevenue) / (double)lastWeekOrderRevenue) * 100;

                // --- RESPONSE ---
                var totalRevenue = totalBookingRevenue + totalOrderRevenue; // Tổng doanh thu sau khi trừ hoa hồng

                response.Success = true;
                response.Data = new AdminDashboardResponse
                {
                    TotalRevenue = totalRevenue,
                    RevenueGrowthPercentage = bookingRevenueGrowthPercent + orderRevenueGrowthPercent, // Tỷ lệ tăng trưởng doanh thu
                    TotalAccounts = await _unitOfWork.AccountRepository.Queryable().CountAsync(),
                    TotalCustomers = await _unitOfWork.AccountRepository.Queryable().CountAsync(a => a.Role.Id == 3),
                    TotalProviders = await _unitOfWork.AccountRepository.Queryable().CountAsync(a => a.Role.Id == 2),

                    TotalBookings = totalBookings,
                    CompletedBookings = completedBookings,
                    CanceledBookings = canceledBookings,

                    TotalOrders = totalOrders,
                    CompletedOrders = relatedOrders.Count(o => o.Status == Order.OrderStatus.Completed),
                    CanceledOrders = relatedOrders.Count(o => o.Status == Order.OrderStatus.Cancelled),

                    TopServices = topServices,
                    TopProducts = topProducts
                };
            }
            catch (Exception ex)
            {
                response.Message = "Failed to load admin dashboard.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

    }
}
