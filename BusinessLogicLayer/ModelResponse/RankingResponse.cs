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
        public string? Avatar { get; set; }
        public decimal TotalSpending { get; set; }
    }
}
