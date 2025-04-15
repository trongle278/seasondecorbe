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
    public class DashboardService:IDashboardService
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
                // BOOKING
                var bookings = await _unitOfWork.BookingRepository.Queryable()
                    .Where(b => b.DecorService.AccountId == providerId)
                    .ToListAsync();

                var totalBookings = bookings.Count;
                var processingBookings = bookings.Count(b => b.Status is Booking.BookingStatus.Planning or
                                                                         Booking.BookingStatus.Quoting or
                                                                         Booking.BookingStatus.Contracting or
                                                                         Booking.BookingStatus.Preparing or
                                                                         Booking.BookingStatus.Progressing);

                var completedBookings = bookings.Count(b => b.Status == Booking.BookingStatus.Completed);
                var canceledBookings = bookings.Count(b => b.Status == Booking.BookingStatus.Canceled);

                var totalRevenue = bookings
                    .Where(b => b.Status == Booking.BookingStatus.Completed)
                    .Sum(b => b.TotalPrice);

                // DECOR SERVICES
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

                // PRODUCTS
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
                    }).ToList();

                // ORDERS
                var orderDetails = await _unitOfWork.OrderDetailRepository.Queryable()
                    .Include(od => od.Order)
                    .Include(od => od.Product)
                    .Where(od => od.Product.AccountId == providerId)
                    .ToListAsync();

                var relatedOrders = orderDetails
                    .Select(od => od.Order)
                    .Distinct()
                    .ToList();

                var totalOrders = relatedOrders.Count;

                var totalProductRevenue = relatedOrders
                    .Where(o => o.Status == Order.OrderStatus.Completed)
                    .Sum(o => o.TotalPrice);

                // RESPONSE
                response.Success = true;
                response.Data = new ProviderDashboardResponse
                {
                    TotalBookings = totalBookings,
                    ProcessingBookings = processingBookings,
                    CompletedBookings = completedBookings,
                    CanceledBookings = canceledBookings,
                    TotalRevenue = totalRevenue,

                    TotalServices = totalServices,
                    TopServices = topServices,

                    TotalProducts = totalProducts,
                    TotalOrders = totalOrders,
                    TotalProductRevenue = totalProductRevenue,
                    TopProducts = topProducts
                };
            }
            catch (Exception ex)
            {
                response.Message = "Failed to load dashboard.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
