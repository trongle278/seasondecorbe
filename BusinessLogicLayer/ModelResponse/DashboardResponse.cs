using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class ProviderDashboardResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalFollowers { get; set; }
        public int TotalServices { get; set; }
        public int TotalBookings { get; set; }
        public int ProcessingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CanceledBookings { get; set; }
        public decimal TotalBookingRevenue { get; set; }
        public int ThisWeekBookings { get; set; }
        public int LastWeekBookings { get; set; }
        public double BookingGrowthRate { get; set; }
        public List<TopServiceResponse> TopServices { get; set; }


        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalOrderRevenue { get; set; }
        public int ThisWeekOrders { get; set; }
        public int LastWeekOrders { get; set; }
        public double OrderGrowthRate { get; set; }
        public List<TopProductResponse> TopProducts { get; set; }
    }

    public class TopServiceResponse
    {
        public int ServiceId { get; set; }
        public string Style { get; set; }
        public int FavoriteCount { get; set; }
    }

    public class TopProductResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int SoldQuantity { get; set; }
    }

    public class AdminDashboardResponse
    {
        public decimal TotalRevenue { get; set; }
        public double RevenueGrowthPercentage { get; set; }
        public int TotalAccounts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProviders { get; set; }

        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CanceledBookings { get; set; }

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CanceledOrders { get; set; }

        public List<TopServiceResponse> TopServices { get; set; }
        public List<TopProductResponse> TopProducts { get; set; }
    }

    public class MonthlyRevenueResponse
    {
        public int Year { get; set; }  // Năm doanh thu
        public int Month { get; set; }  // Tháng trong năm (1 đến 12)
        public decimal TotalRevenue { get; set; }  // Doanh thu của tháng
    }
}
