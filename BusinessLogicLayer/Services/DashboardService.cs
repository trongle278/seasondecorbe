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
                var totalRevenue = bookings.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);

                var bookingsThisWeek = bookings.Where(b => b.CreateAt >= thisWeekStart).ToList();
                var bookingsLastWeek = bookings.Where(b => b.CreateAt >= lastWeekStart && b.CreateAt < lastWeekEnd).ToList();

                var thisWeekBookingCount = bookingsThisWeek.Count;
                var lastWeekBookingCount = bookingsLastWeek.Count;

                var thisWeekBookingRevenue = bookingsThisWeek.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);
                var lastWeekBookingRevenue = bookingsLastWeek.Where(b => b.Status == Booking.BookingStatus.Completed).Sum(b => b.TotalPrice);

                double bookingGrowthPercent = lastWeekBookingCount == 0
                    ? (thisWeekBookingCount > 0 ? 100 : 0)
                    : ((double)(thisWeekBookingCount - lastWeekBookingCount) / lastWeekBookingCount) * 100;

                double bookingRevenueGrowthPercent = lastWeekBookingRevenue == 0
                    ? (thisWeekBookingRevenue > 0 ? 100 : 0)
                    : ((double)(thisWeekBookingRevenue - lastWeekBookingRevenue) / (double)lastWeekBookingRevenue) * 100;

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
                var totalProductRevenue = relatedOrders
                    .Where(o => o.Status == Order.OrderStatus.Completed)
                    .Sum(o => o.TotalPrice);

                var ordersThisWeek = relatedOrders.Where(o => o.OrderDate >= thisWeekStart).ToList();
                var ordersLastWeek = relatedOrders.Where(o => o.OrderDate >= lastWeekStart && o.OrderDate < lastWeekEnd).ToList();

                var thisWeekOrderCount = ordersThisWeek.Count;
                var lastWeekOrderCount = ordersLastWeek.Count;

                var thisWeekOrderRevenue = ordersThisWeek.Where(o => o.Status == Order.OrderStatus.Completed).Sum(o => o.TotalPrice);
                var lastWeekOrderRevenue = ordersLastWeek.Where(o => o.Status == Order.OrderStatus.Completed).Sum(o => o.TotalPrice);

                double orderGrowthPercent = lastWeekOrderCount == 0
                    ? (thisWeekOrderCount > 0 ? 100 : 0)
                    : ((double)(thisWeekOrderCount - lastWeekOrderCount) / lastWeekOrderCount) * 100;

                double orderRevenueGrowthPercent = lastWeekOrderRevenue == 0
                    ? (thisWeekOrderRevenue > 0 ? 100 : 0)
                    : ((double)(thisWeekOrderRevenue - lastWeekOrderRevenue) / (double)lastWeekOrderRevenue) * 100;

                // --- RESPONSE ---
                response.Success = true;
                response.Data = new ProviderDashboardResponse
                {
                    TotalBookings = totalBookings,
                    ProcessingBookings = processingBookings,
                    CompletedBookings = completedBookings,
                    CanceledBookings = canceledBookings,
                    TotalBookingRevenue = totalRevenue,

                    TotalServices = totalServices,
                    TopServices = topServices,

                    TotalProducts = totalProducts,
                    TotalOrders = totalOrders,
                    TotalProductRevenue = totalProductRevenue,
                    TopProducts = topProducts,

                    ThisWeekBookings = thisWeekBookingCount,
                    LastWeekBookings = lastWeekBookingCount,
                    BookingGrowthRate = Math.Round(bookingGrowthPercent, 2),

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
                response.Message = "Lỗi khi lấy doanh thu theo tháng.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
