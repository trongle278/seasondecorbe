using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuotationController : ControllerBase
    {
        private readonly IQuotationService _quotationService;

        public QuotationController(IQuotationService quotationService)
        {
            _quotationService = quotationService;
        }

        /// <summary>
        /// Tạo báo giá cho một booking
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateQuotation([FromBody] CreateQuotationRequest request)
        {
            var response = await _quotationService.CreateQuotationAsync(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Lấy báo giá theo BookingId
        /// </summary>
        [HttpGet("getQuotationByBookingId/{bookingId}")]
        public async Task<IActionResult> GetQuotationByBookingId(int bookingId)
        {
            var response = await _quotationService.GetQuotationDetailAsync(bookingId);
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}
