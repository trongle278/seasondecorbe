using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IUnitOfWork _unitOfWork;

        public WalletController(IWalletService walletService, IUnitOfWork unitOfWork)
        {
            _walletService = walletService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all transactions for a wallet
        /// </summary>
        /// <param name="walletId">ID of the wallet</param>
        /// <returns>List of transactions</returns>
        [HttpGet("{walletId}/transactions")]
        public async Task<ActionResult<BaseResponse<List<WalletTransactionResponse>>>> GetWalletTransactions(int walletId)
        {
            var response = await _walletService.GetWalletTransactions(walletId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Get transactions by type for a wallet
        /// </summary>
        /// <param name="walletId">ID of the wallet</param>
        /// <param name="type">Transaction type (TopUp, Withdraw, Deposite, Refund, Pay, Revenue)</param>
        /// <returns>List of transactions of specified type</returns>
        [HttpGet("{walletId}/transactions/type/{type}")]
        public async Task<ActionResult<BaseResponse<List<WalletTransactionResponse>>>> GetTransactionsByType(
            int walletId,
            PaymentTransaction.EnumTransactionType type)
        {
            var response = await _walletService.GetTransactionsByType(walletId, type);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Get transactions by status for a wallet
        /// </summary>
        /// <param name="walletId">ID of the wallet</param>
        /// <param name="status">Transaction status (Pending, Success, Failed)</param>
        /// <returns>List of transactions with specified status</returns>
        [HttpGet("{walletId}/transactions/status/{status}")]
        public async Task<ActionResult<BaseResponse<List<WalletTransactionResponse>>>> GetTransactionsByStatus(
            int walletId,
            PaymentTransaction.EnumTransactionStatus status)
        {
            var response = await _walletService.GetTransactionsByStatus(walletId, status);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Get transactions within a date range for a wallet
        /// </summary>
        /// <param name="walletId">ID of the wallet</param>
        /// <param name="startDate">Start date (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date (format: yyyy-MM-dd)</param>
        /// <returns>List of transactions within the specified date range</returns>
        [HttpGet("{walletId}/transactions/date-range")]
        public async Task<ActionResult<BaseResponse<List<WalletTransactionResponse>>>> GetTransactionsByDateRange(
            int walletId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var response = await _walletService.GetTransactionsByDateRange(walletId, startDate, endDate);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Get details of a specific transaction
        /// </summary>
        /// <param name="transactionId">ID of the transaction</param>
        /// <returns>Transaction details</returns>
        [HttpGet("transactions/{transactionId}")]
        public async Task<ActionResult<BaseResponse<WalletTransactionResponse>>> GetTransactionDetail(int transactionId)
        {
            var response = await _walletService.GetTransactionDetail(transactionId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Get wallet by account ID
        /// </summary>
        /// <returns>Wallet information for the current user</returns>
        [HttpGet("user")]
        public async Task<IActionResult> GetUserWallet()
        {
            // Get current user ID from claims
            var accountIdClaim = User.FindFirst("AccountId");
            if (accountIdClaim == null)
                return Unauthorized(new BaseResponse { Success = false, Message = "User not authenticated" });

            if (!int.TryParse(accountIdClaim.Value, out int accountId))
                return BadRequest(new BaseResponse { Success = false, Message = "Invalid user ID" });

            // Get wallet by account ID (implement this method in your service)
            var wallet = await _unitOfWork.WalletRepository.Queryable()
                .FirstOrDefaultAsync(w => w.AccountId == accountId);

            if (wallet == null)
                return NotFound(new BaseResponse { Success = false, Message = "Wallet not found for this user" });

            return Ok(new BaseResponse
            {
                Success = true,
                Message = "Wallet retrieved successfully",
                Data = wallet
            });
        }
    }
}
