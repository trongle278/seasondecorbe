using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;
using static DataAccessObject.Models.Booking;

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
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.CreateBookingAsync(request, accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getPendingCancellations")]
        public async Task<IActionResult> GetPendingCancellationBookingsForProvider()
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.GetPendingCancellationBookingsForProviderAsync(providerId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getBookingRequestList")]
        public async Task<IActionResult> GetBookingsByUser()
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.GetBookingsByUserAsync(accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getBookingDetails/{bookingId}")]
        public async Task<IActionResult> GetBookingDetails(int bookingId)
        {
            var response = await _bookingService.GetBookingDetailsAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("status/{bookingId}")]
        public async Task<IActionResult> ChangeBookingStatus(int bookingId)
        {
            var response = await _bookingService.ChangeBookingStatusAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        [HttpPut("requestCancel/{bookingId}")]
        public async Task<IActionResult> RequestCancellation(int bookingId, [FromBody] CancelBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.RequestCancellationAsync(bookingId, accountId, request.CancelTypeId, request.CancelReason);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("approveCancellation/{bookingId}")]
        public async Task<IActionResult> ApproveCancellation(int bookingId)
        {
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.ApproveCancellationAsync(bookingId, providerId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("revokeCancellation/{bookingId}")]
        public async Task<IActionResult> RevokeCancellation(int bookingId)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.RevokeCancellationRequestAsync(bookingId, accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("reject/{bookingId}")]
        public async Task<IActionResult> RejectBooking(int bookingId, [FromBody] RejectBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.RejectBookingAsync(bookingId, accountId, request.Reason);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("deposit/{bookingId}")]
        public async Task<IActionResult> ProcessDeposit(int bookingId)
        {
            var response = await _bookingService.ProcessDepositAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("payment/{bookingId}")]
        public async Task<IActionResult> ProcessConstructionPayment(int bookingId)
        {
            var response = await _bookingService.ProcessFinalPaymentAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
