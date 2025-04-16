using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class CustomerSpendingRankingResponse
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Avatar { get; set; }
        public decimal TotalSpending { get; set; }
    }

    public class ProviderRatingRankingResponse
    {
        public int ProviderId { get; set; }
        public string BusinessName { get; set; }
        public string ProviderName { get; set; }
        public string Email { get; set; }
        public string? Avatar { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}
