using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class ProviderDashboardResponse
    {
        public int TotalBookings { get; set; }
        public int ProcessingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CanceledBookings { get; set; }
        public decimal TotalRevenue { get; set; }

        public int TotalServices { get; set; }
        public List<TopServiceResponse> TopServices { get; set; }

        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }

        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalProductRevenue { get; set; }
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
}
