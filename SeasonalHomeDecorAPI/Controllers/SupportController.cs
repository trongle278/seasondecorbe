using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
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

        // POST: api/Support/create-ticket
        [HttpPost("create-ticket")]
        public async Task<IActionResult> CreateTicket([FromForm] CreateSupportRequest request)
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "User ID not found in token." });
                }

                if (!int.TryParse(userIdClaim.Value, out int accountId))
                {
                    return BadRequest(new { message = "Invalid user ID in token." });
                }

                // Nếu không tìm thấy tài khoản trong DB, coi đó là admin và gán accountId = -1
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                request.AccountId = account == null ? -1 : accountId;

                var response = await _supportService.CreateTicketAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/Support/reply-ticket
        [HttpPost("reply-ticket")]
        public async Task<IActionResult> ReplyTicket([FromForm] AddSupportReplyRequest request)
        {
            try
            {
                // Lấy userId từ token và gán vào request
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "User ID not found in token." });
                }

                if (!int.TryParse(userIdClaim.Value, out int accountId))
                {
                    return BadRequest(new { message = "Invalid user ID in token." });
                }
                request.AccountId = accountId;

                // Kiểm tra tài khoản từ DB; nếu không tìm thấy thì coi đó là admin
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId);
                bool isAdmin = account == null || account.RoleId == 1;

                var replyResponse = await _supportService.AddReplyAsync(request, isAdmin);
                return Ok(replyResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/Support/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            try
            {
                var response = await _supportService.GetTicketByIdAsync(id);
                if (response == null)
                    return NotFound(new { message = "Ticket not found" });
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
