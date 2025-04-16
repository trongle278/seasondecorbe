using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IDashboardService
    {
        Task<BaseResponse<ProviderDashboardResponse>> GetProviderDashboardAsync(int providerId);
        Task<BaseResponse<List<MonthlyRevenueResponse>>> GetMonthlyRevenueAsync(int providerId);
        Task<BaseResponse<List<CustomerSpendingRankingResponse>>> GetTopCustomerSpendingRankingAsync(int providerId);
    }
}
