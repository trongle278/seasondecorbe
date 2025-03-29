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
        /// <summary>
        /// Tạo Ticket mới
        /// </summary>
        [HttpPost("create-ticket")]
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
        [HttpPost("reply-ticket")]
        public async Task<ActionResult<BaseResponse<SupportReplyResponse>>> AddReply([FromForm] AddSupportReplyRequest request)
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            bool isAdmin = User.IsInRole("Admin");

            var response = await _supportService.AddReplyAsync(request, accountId, isAdmin);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
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
