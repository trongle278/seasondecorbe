using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.UnitOfWork;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SupportController : ControllerBase
    {
        private readonly ISupportService _supportService;
        private readonly IUnitOfWork _unitOfWork;

        public SupportController(ISupportService supportService, IUnitOfWork unitOfWork)
        {
            _supportService = supportService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tạo Ticket mới
        /// </summary>
        [HttpPost("createTicket")]
        public async Task<ActionResult<BaseResponse<SupportResponse>>> CreateTicket([FromForm] CreateSupportRequest request)
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var response = await _supportService.CreateTicketAsync(request, accountId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Thêm Reply vào Ticket
        /// </summary>
        [HttpPost("replyTicket")]
        public async Task<ActionResult<BaseResponse<SupportReplyResponse>>> AddReply([FromForm] AddSupportReplyRequest request, int supportId)
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            bool isAdmin = User.IsInRole("Admin");

            var response = await _supportService.AddReplyAsync(request, supportId, accountId, isAdmin);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("getSupportById/{supportId}")]
        public async Task<IActionResult> GetSupportById(int supportId)
        {
            try
            {
                var response = await _supportService.GetSupportByIdAsync(supportId);
                if (response == null)
                    return NotFound(new { message = "Ticket not found" });
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("getPaginatedSupportTicketsForAdmin")]
        public async Task<IActionResult> GetPaginatedSupportTicketsForAdmin([FromQuery] SupportFilterRequest request)
        {
            // Lấy providerId từ token (vì đối với provider, accountId chính là providerId)
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _supportService.GetPaginatedSupportForAdminAsync(request);
            return Ok(result);
        }

        [HttpGet("getPaginatedSupportTicketsForCustomer")]
        public async Task<IActionResult> GetPaginatedSupportTicketsForCustomer([FromQuery] SupportFilterRequest request)
        {
            // Lấy providerId từ token (vì đối với provider, accountId chính là providerId)
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _supportService.GetPaginatedTicketsForCustomerAsync(request, accountId);
            return Ok(result);
        }
    }
}
