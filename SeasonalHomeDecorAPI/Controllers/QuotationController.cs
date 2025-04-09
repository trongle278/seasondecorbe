using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.Services;
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
        [HttpPost("createQuotationByBookingCode/{bookingCode}")]
        public async Task<IActionResult> CreateQuotation(string bookingCode, CreateQuotationRequest request)
        {
            var response = await _quotationService.CreateQuotationAsync(bookingCode, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("uploadQuotationFileByBookingCode/{bookingCode}")]
        public async Task<IActionResult> UploadQuotationFile(string bookingCode, IFormFile quotationFile)
        {
            var response = await _quotationService.UploadQuotationFileAsync(bookingCode, quotationFile);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getPaginatedQuotationsForCustomer")]
        public async Task<IActionResult> GetPaginatedQuotationsForCustomer([FromQuery] QuotationFilterRequest request)
        {
            // Lấy accountId từ token
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _quotationService.GetPaginatedQuotationsForCustomerAsync(request, accountId);
            return Ok(result);
        }

        [HttpGet("getPaginatedQuotationsForProvider")]
        public async Task<IActionResult> GetPaginatedQuotationsForProvider([FromQuery] QuotationFilterRequest request)
        {
            // Lấy accountId từ token
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _quotationService.GetPaginatedQuotationsForProviderAsync(request, providerId);
            return Ok(result);
        }

        [HttpPut("confirmQuotation/{quotationCode}")]
        public async Task<IActionResult> ConfirmQuotation(string quotationCode, [FromBody] bool isConfirmed)
        {
            var response = await _quotationService.ConfirmQuotationAsync(quotationCode, isConfirmed);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getQuotationDetailByCustomer/{quotationCode}")]
        public async Task<IActionResult> GetQuotationDetailByCustomer(string quotationCode)
        {
            // Lấy customerId từ token
            var customerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var response = await _quotationService.GetQuotationDetailByCustomerAsync(quotationCode, customerId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
