using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("getProviderDashboard")]
        [Authorize]
        public async Task<IActionResult> GetProviderDashboard()
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _dashboardService.GetProviderDashboardAsync(providerId);
            return Ok(result);
        }

        [HttpGet("getMonthlyRevenue")]
        [Authorize]
        public async Task<IActionResult> GetMonthlyRevenue()
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _dashboardService.GetMonthlyRevenueAsync(providerId);
            return Ok(result);
        }

        [HttpGet("getTopCustomerSpendingRanking")]
        [Authorize]
        public async Task<IActionResult> GetTopCustomerSpendingRanking()
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _dashboardService.GetTopCustomerSpendingRankingAsync(providerId);
            return Ok(result);
        }

        [HttpGet("getAdminDashboard")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var result = await _dashboardService.GetAdminDashboardAsync();
            return Ok(result);
        }

        [HttpGet("getAdminMonthlyRevenue")]
        public async Task<IActionResult> GetAdminMonthlyRevenue()
        {
            var result = await _dashboardService.GetAdminMonthlyRevenueAsync();
            return Ok(result);
        }

        [HttpGet("getTopProviderRatingRanking")]
        public async Task<IActionResult> GetTopProviderRatingRanking()
        {
            var result = await _dashboardService.GetTopProviderRatingRankingAsync();
            return Ok(result);
        }

        [HttpGet("getProviderPaginatedPaymentTransaction")]
        [Authorize]
        public async Task<IActionResult> GetProviderPaginatedPaymentTransaction([FromQuery]ProviderPaymentFilterRequest request)
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _dashboardService.GetProviderPaginatedPaymentsAsync(request, providerId);
            return Ok(result);
        }

        [HttpGet("getAdminPaginatedPaymentTransaction")]
        public async Task<IActionResult> GetAdminPaginatedPaymentTransaction([FromQuery] AdminPaymentFilterRequest request)
        {
            var result = await _dashboardService.GetAdminPaginatedPaymentsAsync(request);
            return Ok(result);
        }
    }
}
