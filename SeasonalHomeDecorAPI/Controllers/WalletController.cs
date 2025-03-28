using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        public WalletController(IWalletService walletService) 
        {
            _walletService = walletService;
        }

        [HttpGet("getWalletBalance")]
        public async Task<IActionResult> GetWalletBalance()
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _walletService.GetWalletByAccountId(accountId);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Lấy danh sách giao dịch của ví theo WalletId.
        /// </summary>
        [HttpGet("getTransactionsDetails")]
        public async Task<IActionResult> GetTransactionsDetails()
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _walletService.GetAllTransactionsByAccountId(accountId);

            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
