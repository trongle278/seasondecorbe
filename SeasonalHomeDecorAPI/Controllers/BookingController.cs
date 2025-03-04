using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IPayosService _payosService; // Service tích hợp PayOS

        public BookingController(IBookingService bookingService, IPayosService payosService)
        {
            _bookingService = bookingService;
            _payosService = payosService;
        }

        [Authorize]
        // POST: api/Booking/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            // Lấy AccountId từ User Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new BaseResponse
                {
                    Success = false,
                    Message = "User is not authenticated."
                });
            }
            int accountId = int.Parse(userIdClaim.Value);

            var result = await _bookingService.CreateBookingAsync(request, accountId);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [Authorize]
        // POST: api/Booking/confirm
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmBooking([FromBody] ConfirmBookingRequest request)
        {
            // Ở đây có thể không cần AccountId vì chỉ cần BookingId và danh sách PaymentPhase
            var result = await _bookingService.ConfirmBookingAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [Authorize]
        // PUT: api/Booking/status
        [HttpPut("status")]
        public async Task<IActionResult> UpdateBookingStatus([FromBody] UpdateBookingStatusRequest request)
        {
            var result = await _bookingService.UpdateBookingStatusAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/Booking/{bookingId}
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetBooking(int bookingId)
        {
            var result = await _bookingService.GetBookingAsync(bookingId);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // POST: api/Booking/payment
        // Endpoint để thực hiện thanh toán qua PayOS
        [HttpPost("payment")]
        [Authorize]
        public async Task<IActionResult> MakePayment([FromBody] MakePaymentRequest request)
        {
            // Lấy AccountId từ User Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new BaseResponse
                {
                    Success = false,
                    Message = "User is not authenticated."
                });
            }
            int accountId = int.Parse(userIdClaim.Value);

            // Gọi BookingService để tạo Payment (trong đó bên trong đã gọi _payosService.CreatePaymentLinkAsync)
            var response = await _bookingService.MakePaymentAsync(request, accountId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
