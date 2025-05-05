using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        [Authorize]
        public async Task<IActionResult> CreateQuotation(string bookingCode, CreateQuotationRequest request)
        {
            var response = await _quotationService.CreateQuotationAsync(bookingCode, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("uploadQuotationFileByBookingCode/{bookingCode}")]
        [Authorize]
        public async Task<IActionResult> UploadQuotationFile(string bookingCode, IFormFile quotationFile)
        {
            var response = await _quotationService.UploadQuotationFileAsync(bookingCode, quotationFile);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getPaginatedQuotationsForCustomer")]
        [Authorize]
        public async Task<IActionResult> GetPaginatedQuotationsForCustomer([FromQuery] QuotationFilterRequest request)
        {
            // Lấy accountId từ token
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _quotationService.GetPaginatedQuotationsForCustomerAsync(request, accountId);
            return Ok(result);
        }

        [HttpGet("getPaginatedQuotationsForProvider")]
        [Authorize]
        public async Task<IActionResult> GetPaginatedQuotationsForProvider([FromQuery] QuotationFilterRequest request)
        {
            // Lấy accountId từ token
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _quotationService.GetPaginatedQuotationsForProviderAsync(request, providerId);
            return Ok(result);
        }

        [HttpPut("confirmQuotation/{quotationCode}")]
        [Authorize]
        public async Task<IActionResult> ConfirmQuotation(string quotationCode, [FromBody] bool isConfirmed)
        {
            var response = await _quotationService.ConfirmQuotationAsync(quotationCode, isConfirmed);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("requestToChangeQuotation/{quotationCode}")]
        [Authorize]
        public async Task<IActionResult> RequestToChangeQuotation(string quotationCode, string? changeReason)
        {
            var response = await _quotationService.RequestChangeQuotationAsync(quotationCode, changeReason);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("approveToChangeQuotation/{quotationCode}")]
        [Authorize]
        public async Task<IActionResult> ApproveToChangeQuotation(string quotationCode)
        {
            var response = await _quotationService.ApproveChangeQuotationAsync(quotationCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("requestCancelQuotation/{quotationCode}")]
        [Authorize]
        public async Task<IActionResult> RequestCancelQuotation(string quotationCode, int quotationCancelId, string cancelReason)
        {
            var response = await _quotationService.RequestCancelQuotationAsync(quotationCode, quotationCancelId, cancelReason);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("approveCancelQuotation/{quotationCode}")]
        [Authorize]
        public async Task<IActionResult> ApproveCancelQuotation(string quotationCode)
        {
            var response = await _quotationService.ApproveCancelQuotationAsync(quotationCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getQuotationDetailByCustomer/{quotationCode}")]
        [Authorize]
        public async Task<IActionResult> GetQuotationDetailByCustomer(string quotationCode)
        {
            // Lấy customerId từ token
            var customerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var response = await _quotationService.GetQuotationDetailByCustomerAsync(quotationCode, customerId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getPaginatedRelatedProduct")]
        [Authorize]
        public async Task<IActionResult> GetPaginatedRelatedProduct([FromQuery] PagingRelatedProductRequest request)
        {
            var response = await _quotationService.GetPaginatedRelatedProductAsync(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("addProductToQuotation/{quotationCode}")]
        [Authorize]
        public async Task<IActionResult> AddProductToQuotation(string quotationCode, int productId, int quantity)
        {
            var response = await _quotationService.AddProductToQuotationAsync(quotationCode, productId, quantity);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("removeProductFromQuotation/{quotationCode}")]
        [Authorize]
        public async Task<IActionResult> RemoveProductFromQuotation(string quotationCode, int productId)
        {
            var response = await _quotationService.RemoveProductFromQuotationAsync(quotationCode, productId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
