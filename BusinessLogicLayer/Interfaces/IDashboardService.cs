using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;

namespace BusinessLogicLayer.Interfaces
{
    public interface IDashboardService
    {
        Task<BaseResponse<ProviderDashboardResponse>> GetProviderDashboardAsync(int providerId);
        Task<BaseResponse<List<MonthlyRevenueResponse>>> GetMonthlyRevenueAsync(int providerId);
        Task<BaseResponse<List<CustomerSpendingRankingResponse>>> GetTopCustomerSpendingRankingAsync(int providerId);
        Task<BaseResponse<AdminDashboardResponse>> GetAdminDashboardAsync();
        Task<BaseResponse<List<MonthlyRevenueResponse>>> GetAdminMonthlyRevenueAsync();
        Task<BaseResponse<List<ProviderRatingRankingResponse>>> GetTopProviderRatingRankingAsync();
        Task<BaseResponse<PageResult<ProviderPaymentResponse>>> GetProviderPaginatedPaymentsAsync(ProviderPaymentFilterRequest request, int providerId);
    }
}
