using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("getProviderDashboard")]
        public async Task<IActionResult> GetProviderDashboard()
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _dashboardService.GetProviderDashboardAsync(providerId);
            return Ok(result);
        }

        [HttpGet("getMonthlyRevenue")]
        public async Task<IActionResult> GetMonthlyRevenue()
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _dashboardService.GetMonthlyRevenueAsync(providerId);
            return Ok(result);
        }
    }
}
