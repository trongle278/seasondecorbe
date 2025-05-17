using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;
using static DataAccessObject.Models.Booking;
using CloudinaryDotNet;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet("getPaginatedBookingsForCustomer")]
        [Authorize] // Bắt buộc đăng nhập để xem danh sách Booking
        public async Task<IActionResult> GetPaginatedBookingsForCustomer([FromQuery] BookingFilterRequest request)
        {
            // Lấy accountId từ token
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _bookingService.GetPaginatedBookingsForCustomerAsync(request, accountId);
            return Ok(result);
        }

        [HttpGet("getPaginatedBookingsForProvider")]
        public async Task<IActionResult> GetPaginatedBookingsForProvider([FromQuery] BookingFilterRequest request)
        {
            // Lấy providerId từ token (vì đối với provider, accountId chính là providerId)
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _bookingService.GetPaginatedBookingsForProviderAsync(request, providerId);
            return Ok(result);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromQuery] CreateBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.CreateBookingAsync(request, accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("updateBookingRequest/{bookingCode}")]
        public async Task<IActionResult> UpdateBookingRequest(string bookingCode, [FromQuery] UpdateBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _bookingService.UpdateBookingRequestAsync(bookingCode, request, accountId);
            return Ok(result);
        }

        [HttpGet("getPendingCancelBookingDetailByBookingCode/{bookingCode}")]
        public async Task<ActionResult<BaseResponse<BookingResponse>>> GetPendingCancelBookingDetailByBookingCode(string bookingCode)
        {
            int providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.GetPendingCancelBookingDetailByBookingCodeAsync(bookingCode, providerId);
            return Ok(response);
        }

        [HttpGet("getBookingRequestList")]
        public async Task<IActionResult> GetBookingsByUser()
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.GetBookingsByUserAsync(accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getBookingDetailsForProvider/{bookingCode}")]
        public async Task<IActionResult> GetBookingDetailsForProvider(string bookingCode)
        {
            try
            {
                var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var response = await _bookingService.GetBookingDetailForProviderAsync(bookingCode, accountId);

                if (!response.Success)
                {
                    return BadRequest(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse<List<BookingDetailForProviderResponse>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("status/{bookingCode}")]
        public async Task<IActionResult> ChangeBookingStatus(string bookingCode)
        {
            var response = await _bookingService.ChangeBookingStatusAsync(bookingCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        [HttpPut("requestCancel/{bookingCode}")]
        public async Task<IActionResult> RequestCancellation(string bookingCode, [FromBody] CancelBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.RequestCancellationAsync(bookingCode, accountId, request.CancelTypeId, request.CancelReason);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("approveCancellation/{bookingCode}")]
        public async Task<IActionResult> ApproveCancellation(string bookingCode)
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.ApproveCancellationAsync(bookingCode, providerId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("revokeCancellation/{bookingCode}")]
        public async Task<IActionResult> RevokeCancellation(string bookingCode)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.RevokeCancellationRequestAsync(bookingCode, accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("reject/{bookingCode}")]
        public async Task<IActionResult> RejectBooking(string bookingCode, [FromBody] RejectBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.RejectBookingAsync(bookingCode, accountId, request.Reason);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("deposit/{bookingCode}")]
        public async Task<IActionResult> ProcessDeposit(string bookingCode)
        {
            var response = await _bookingService.ProcessDepositAsync(bookingCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("payment/{bookingCode}")]
        public async Task<IActionResult> ProcessConstructionPayment(string bookingCode)
        {
            var response = await _bookingService.ProcessFinalPaymentAsync(bookingCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("processCommitDeposit/{bookingCode}")]
        public async Task<IActionResult> ProcessCommitDeposit(string bookingCode)
        {
            var response = await _bookingService.ProcessCommitDepositAsync(bookingCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
